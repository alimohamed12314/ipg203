using System;
using System.Collections.Generic;
using System.Linq;
using ApplianceRepairCenter.Enums;
using ApplianceRepairCenter.Events;
using ApplianceRepairCenter.Interfaces;

namespace ApplianceRepairCenter.Services;

/// <summary>
/// One repair center: it takes appliances in, follows each of them through its repair, and reports
/// on costs, finished repairs and late deliveries. It knows appliances only as
/// <see cref="IAppliance"/>, so every type is handled by the same code.
/// </summary>
public sealed class RepairCenter
{
    // Belongs to the class, not to one center: the number of appliances at each stage in all
    // centers together. A count that covers several centers cannot be read from one center's list.
    private static readonly Dictionary<ApplianceStatus, int> s_statusCounts =
        Enum.GetValues<ApplianceStatus>().ToDictionary(status => status, _ => 0);

    private readonly List<IAppliance> _appliances = new List<IAppliance>();

    public RepairCenter(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    /// <summary>Raised when the repair of one of this center's appliances is finished.</summary>
    public event StatusChangedEventHandler? RepairCompleted;

    /// <summary>Raised by <see cref="CheckOverdueDeliveries"/> for each appliance that is late.</summary>
    public event EventHandler<DeliveryOverdueEventArgs>? DeliveryOverdue;

    /// <summary>The number of appliances registered in all centers.</summary>
    public static int TotalRegistered
    {
        get { return s_statusCounts.Values.Sum(); }
    }

    /// <summary>The number of appliances a technician is working on right now, in all centers.</summary>
    public static int UnderRepairCount
    {
        get { return CountByStatus(ApplianceStatus.UnderRepair); }
    }

    public string Name { get; }

    /// <summary>A read-only view: appliances enter the center through <see cref="Register"/> only.</summary>
    public IReadOnlyList<IAppliance> Appliances
    {
        get { return _appliances.AsReadOnly(); }
    }

    /// <summary>The number of appliances that have the given status, in all centers.</summary>
    public static int CountByStatus(ApplianceStatus status)
    {
        return s_statusCounts[status];
    }

    /// <summary>Adds up the repair cost of any group of appliances; each one prices itself.</summary>
    public static decimal CalculateTotalCost(IEnumerable<IAppliance> appliances)
    {
        ArgumentNullException.ThrowIfNull(appliances);
        return appliances.Sum(appliance => appliance.CalculateRepairCost());
    }

    /// <summary>Takes an appliance in and starts listening for its status changes.</summary>
    public void Register(IAppliance appliance)
    {
        ArgumentNullException.ThrowIfNull(appliance);

        if (FindBySerialNumber(appliance.SerialNumber) is not null)
        {
            throw new InvalidOperationException(
                $"An appliance with serial number {appliance.SerialNumber} is already registered.");
        }

        _appliances.Add(appliance);
        s_statusCounts[appliance.Status]++;
        appliance.StatusChanged += HandleApplianceStatusChanged;
    }

    /// <summary>Looks an appliance up by its serial number; returns null when there is none.</summary>
    public IAppliance? FindBySerialNumber(string serialNumber)
    {
        return _appliances.FirstOrDefault(appliance =>
            string.Equals(appliance.SerialNumber, serialNumber, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The appliances whose repair is finished, collected by their owners or not.</summary>
    public IReadOnlyList<IAppliance> GetRepairedAppliances()
    {
        return _appliances.Where(appliance => appliance.IsRepaired).ToList();
    }

    /// <summary>The appliances still waiting for a technician or being worked on.</summary>
    public IReadOnlyList<IAppliance> GetAppliancesUnderMaintenance()
    {
        return _appliances.Where(appliance => !appliance.IsRepaired).ToList();
    }

    /// <summary>The cost of all repairs in this center.</summary>
    public decimal CalculateTotalRepairCost()
    {
        return CalculateTotalCost(_appliances);
    }

    /// <summary>
    /// Finds the appliances that are still not repaired after their promised date and raises
    /// <see cref="DeliveryOverdue"/> for each of them.
    /// </summary>
    public IReadOnlyList<IAppliance> CheckOverdueDeliveries(DateTime asOf)
    {
        List<IAppliance> overdue = _appliances.Where(appliance => appliance.IsOverdue(asOf)).ToList();

        foreach (IAppliance appliance in overdue)
        {
            int daysLate = (asOf.Date - appliance.ExpectedDeliveryDate).Days;
            DeliveryOverdue?.Invoke(this, new DeliveryOverdueEventArgs(appliance, daysLate));
        }

        return overdue;
    }

    /// <summary>
    /// Runs each time a registered appliance changes status: keeps the counts right, announces a
    /// finished repair, and stops listening once the appliance has left the center.
    /// </summary>
    private void HandleApplianceStatusChanged(object? sender, StatusChangedEventArgs e)
    {
        s_statusCounts[e.PreviousStatus]--;
        s_statusCounts[e.NewStatus]++;

        if (e.NewStatus == ApplianceStatus.Repaired)
        {
            RepairCompleted?.Invoke(this, e);
        }
        else if (e.NewStatus == ApplianceStatus.Delivered)
        {
            e.Appliance.StatusChanged -= HandleApplianceStatusChanged;
        }
    }
}
