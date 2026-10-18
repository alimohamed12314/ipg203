using System;
using System.Collections.Generic;
using ApplianceRepairCenter.Enums;
using ApplianceRepairCenter.Events;
using ApplianceRepairCenter.Interfaces;
using ApplianceRepairCenter.Models;

namespace ApplianceRepairCenter.Tests;

/// <summary>
/// An appliance that implements <see cref="IAppliance"/> directly, without the abstract class.
/// The repair center must handle it like any other, which shows that the center depends on the
/// contract only. It also counts who is listening to its event.
/// </summary>
internal sealed class FakeAppliance : IAppliance
{
    private StatusChangedEventHandler? _statusChanged;

    public FakeAppliance(string serialNumber, decimal repairCost)
    {
        SerialNumber = serialNumber;
        RepairCost = repairCost;
    }

    public event StatusChangedEventHandler? StatusChanged
    {
        add
        {
            _statusChanged += value;
            SubscriberCount++;
        }

        remove
        {
            _statusChanged -= value;
            SubscriberCount--;
        }
    }

    public int SubscriberCount { get; private set; }

    public decimal RepairCost { get; }

    public string SerialNumber { get; }

    public Customer Owner { get; } = TestData.Owner();

    public string FaultDescription { get; } = "Unknown device";

    public DateTime ReceivedDate { get; } = TestData.Today;

    public DateTime ExpectedDeliveryDate { get; set; } = TestData.Today;

    public ApplianceStatus Status { get; private set; } = ApplianceStatus.Received;

    public bool IsRepaired
    {
        get { return Status >= ApplianceStatus.Repaired; }
    }

    public IReadOnlyList<RepairOperation> Operations { get; } = Array.Empty<RepairOperation>();

    public void StartRepair()
    {
        MoveTo(ApplianceStatus.UnderRepair);
    }

    public void AddOperation(RepairOperation operation)
    {
    }

    public void CompleteRepair()
    {
        MoveTo(ApplianceStatus.Repaired);
    }

    public void DeliverToOwner()
    {
        MoveTo(ApplianceStatus.Delivered);
    }

    public bool IsOverdue(DateTime asOf)
    {
        return !IsRepaired && asOf.Date > ExpectedDeliveryDate;
    }

    public decimal CalculateRepairCost()
    {
        return RepairCost;
    }

    private void MoveTo(ApplianceStatus newStatus)
    {
        ApplianceStatus previousStatus = Status;
        Status = newStatus;
        _statusChanged?.Invoke(this, new StatusChangedEventArgs(this, previousStatus, newStatus));
    }
}
