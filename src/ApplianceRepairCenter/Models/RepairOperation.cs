using System;
using ApplianceRepairCenter.Validation;

namespace ApplianceRepairCenter.Models;

/// <summary>
/// One piece of maintenance work done on an appliance: what was done, how long it took and what
/// the spare parts cost. It is a record of something that happened, so it cannot be changed.
/// </summary>
public sealed class RepairOperation
{
    public RepairOperation(string description, decimal laborHours, decimal partsCost, DateTime performedOn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(laborHours);
        ArgumentOutOfRangeException.ThrowIfNegative(partsCost);

        if (!InputValidator.IsNotInFuture(performedOn))
        {
            throw new ArgumentException("An operation cannot be dated in the future.", nameof(performedOn));
        }

        Description = description.Trim();
        LaborHours = laborHours;
        PartsCost = partsCost;
        PerformedOn = performedOn.Date;
    }

    public string Description { get; }

    /// <summary>The technician's working time, in hours.</summary>
    public decimal LaborHours { get; }

    /// <summary>The price of the spare parts used; zero when none were needed.</summary>
    public decimal PartsCost { get; }

    public DateTime PerformedOn { get; }
}
