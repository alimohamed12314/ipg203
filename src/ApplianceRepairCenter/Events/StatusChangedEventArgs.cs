using System;
using ApplianceRepairCenter.Enums;
using ApplianceRepairCenter.Interfaces;

namespace ApplianceRepairCenter.Events;

/// <summary>
/// What a listener learns when an appliance changes status.
/// </summary>
public sealed class StatusChangedEventArgs : EventArgs
{
    public StatusChangedEventArgs(IAppliance appliance, ApplianceStatus previousStatus, ApplianceStatus newStatus)
    {
        Appliance = appliance;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
    }

    /// <summary>The appliance whose status changed.</summary>
    public IAppliance Appliance { get; }

    /// <summary>The status it had before the change.</summary>
    public ApplianceStatus PreviousStatus { get; }

    /// <summary>The status it has now.</summary>
    public ApplianceStatus NewStatus { get; }
}
