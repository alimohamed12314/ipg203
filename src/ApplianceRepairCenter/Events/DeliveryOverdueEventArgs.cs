using System;
using ApplianceRepairCenter.Interfaces;

namespace ApplianceRepairCenter.Events;

/// <summary>
/// What a listener learns when an appliance is still not repaired after its promised delivery date.
/// </summary>
public sealed class DeliveryOverdueEventArgs : EventArgs
{
    public DeliveryOverdueEventArgs(IAppliance appliance, int daysLate)
    {
        Appliance = appliance;
        DaysLate = daysLate;
    }

    /// <summary>The appliance that is late.</summary>
    public IAppliance Appliance { get; }

    /// <summary>How many days have passed since the promised delivery date.</summary>
    public int DaysLate { get; }
}
