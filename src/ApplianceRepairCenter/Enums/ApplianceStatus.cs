namespace ApplianceRepairCenter.Enums;

/// <summary>
/// The stages an appliance goes through at the repair center, in the order they happen.
/// </summary>
public enum ApplianceStatus
{
    /// <summary>Checked in and waiting for a technician.</summary>
    Received,

    /// <summary>A technician is working on it.</summary>
    UnderRepair,

    /// <summary>Fixed and waiting for its owner to collect it.</summary>
    Repaired,

    /// <summary>Handed back to its owner.</summary>
    Delivered,
}
