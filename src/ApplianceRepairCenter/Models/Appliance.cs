using System;
using System.Collections.Generic;
using System.Linq;
using ApplianceRepairCenter.Enums;
using ApplianceRepairCenter.Events;
using ApplianceRepairCenter.Interfaces;
using ApplianceRepairCenter.Validation;

namespace ApplianceRepairCenter.Models;

/// <summary>
/// What all appliances in the workshop share: the intake data, the repair workflow and the record
/// of the work done. How the repair is priced differs from one type to another, so that part is
/// left abstract for each type to define.
/// </summary>
public abstract class Appliance : IAppliance
{
    // The center's tariff, the same for every appliance type (Syrian pounds).
    private const decimal InspectionFee = 25_000m;
    private const decimal HourlyLaborRate = 60_000m;

    private readonly List<RepairOperation> _operations = new List<RepairOperation>();
    private DateTime _expectedDeliveryDate;

    protected Appliance(
        string serialNumber,
        string brand,
        string model,
        Customer owner,
        string faultDescription,
        DateTime receivedDate,
        DateTime expectedDeliveryDate)
    {
        if (!InputValidator.IsValidSerialNumber(serialNumber))
        {
            throw new ArgumentException(
                "A serial number is 6 to 20 capital letters, digits and hyphens, for example RF-2024-0001.",
                nameof(serialNumber));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(brand);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(faultDescription);

        if (!InputValidator.IsNotInFuture(receivedDate))
        {
            throw new ArgumentException("An appliance cannot be received on a future date.", nameof(receivedDate));
        }

        SerialNumber = serialNumber;
        Brand = brand.Trim();
        Model = model.Trim();
        Owner = owner;
        FaultDescription = faultDescription.Trim();
        ReceivedDate = receivedDate.Date;

        // Assigned through the property, so its setter checks the date against ReceivedDate.
        ExpectedDeliveryDate = expectedDeliveryDate;
    }

    public event StatusChangedEventHandler? StatusChanged;

    public string SerialNumber { get; }

    public string Brand { get; }

    public string Model { get; }

    public Customer Owner { get; }

    public string FaultDescription { get; }

    public DateTime ReceivedDate { get; }

    public DateTime ExpectedDeliveryDate
    {
        get
        {
            return _expectedDeliveryDate;
        }

        set
        {
            if (!InputValidator.IsValidDeliveryDate(ReceivedDate, value))
            {
                throw new ArgumentException(
                    "The delivery date cannot be earlier than the day the appliance was received.",
                    nameof(value));
            }

            _expectedDeliveryDate = value.Date;
        }
    }

    /// <summary>Changes only through the workflow methods, never by direct assignment from outside.</summary>
    public ApplianceStatus Status { get; private set; } = ApplianceStatus.Received;

    public bool IsRepaired
    {
        get { return Status is ApplianceStatus.Repaired or ApplianceStatus.Delivered; }
    }

    /// <summary>A read-only view: operations are added through <see cref="AddOperation"/> only.</summary>
    public IReadOnlyList<RepairOperation> Operations
    {
        get { return _operations.AsReadOnly(); }
    }

    /// <summary>The name of the appliance type as shown to people, for example "Washing Machine".</summary>
    public abstract string TypeName { get; }

    public void StartRepair()
    {
        ChangeStatus(ApplianceStatus.Received, ApplianceStatus.UnderRepair);
    }

    public void AddOperation(RepairOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (Status != ApplianceStatus.UnderRepair)
        {
            throw new InvalidOperationException(
                $"Work can be recorded only while appliance {SerialNumber} is under repair; it is {Status}.");
        }

        if (operation.PerformedOn < ReceivedDate)
        {
            throw new ArgumentException(
                "An operation cannot be dated before the appliance was received.", nameof(operation));
        }

        _operations.Add(operation);
    }

    public void CompleteRepair()
    {
        if (Status == ApplianceStatus.UnderRepair && _operations.Count == 0)
        {
            throw new InvalidOperationException(
                $"Appliance {SerialNumber} has no recorded work, so its repair cannot be declared finished.");
        }

        ChangeStatus(ApplianceStatus.UnderRepair, ApplianceStatus.Repaired);
    }

    public void DeliverToOwner()
    {
        ChangeStatus(ApplianceStatus.Repaired, ApplianceStatus.Delivered);
    }

    public bool IsOverdue(DateTime asOf)
    {
        return !IsRepaired && asOf.Date > ExpectedDeliveryDate;
    }

    /// <summary>Prices the repair with the rules of the concrete appliance type.</summary>
    public abstract decimal CalculateRepairCost();

    public override string ToString()
    {
        return $"{TypeName} {Brand} {Model}";
    }

    /// <summary>
    /// The part of the price all types share: the inspection fee, the spare parts and the
    /// technician's hours. A type that takes more work to open up passes a labor factor above 1.
    /// </summary>
    protected decimal CalculateBaseCost(decimal laborFactor = 1m)
    {
        decimal laborHours = _operations.Sum(operation => operation.LaborHours);
        decimal partsCost = _operations.Sum(operation => operation.PartsCost);

        return InspectionFee + partsCost + (laborHours * HourlyLaborRate * laborFactor);
    }

    /// <summary>Raises <see cref="StatusChanged"/>; does nothing when nobody is listening.</summary>
    protected virtual void OnStatusChanged(StatusChangedEventArgs e)
    {
        StatusChanged?.Invoke(this, e);
    }

    /// <summary>
    /// The only place where the status changes: it refuses a step taken out of order,
    /// then announces the change.
    /// </summary>
    private void ChangeStatus(ApplianceStatus requiredStatus, ApplianceStatus newStatus)
    {
        if (Status != requiredStatus)
        {
            throw new InvalidOperationException(
                $"Appliance {SerialNumber} is {Status}; it must be {requiredStatus} to become {newStatus}.");
        }

        Status = newStatus;
        OnStatusChanged(new StatusChangedEventArgs(this, requiredStatus, newStatus));
    }
}
