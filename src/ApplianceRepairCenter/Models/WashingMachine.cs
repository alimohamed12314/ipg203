using System;

namespace ApplianceRepairCenter.Models;

/// <summary>
/// A washing machine. Its faults are mostly mechanical, so the price follows how hard the drum
/// is to reach and how heavy it is to handle.
/// </summary>
public sealed class WashingMachine : Appliance
{
    // The drum of a front loader comes out only after the tub is taken apart.
    private const decimal FrontLoadingLaborFactor = 1.25m;

    // From this drum size up, two technicians are needed to move the machine.
    private const int HeavyDrumThresholdKg = 9;
    private const decimal HeavyDrumHandlingFee = 30_000m;

    public WashingMachine(
        string serialNumber,
        string brand,
        string model,
        Customer owner,
        string faultDescription,
        DateTime receivedDate,
        DateTime expectedDeliveryDate,
        int drumCapacityKg,
        bool isFrontLoading)
        : base(serialNumber, brand, model, owner, faultDescription, receivedDate, expectedDeliveryDate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(drumCapacityKg);

        DrumCapacityKg = drumCapacityKg;
        IsFrontLoading = isFrontLoading;
    }

    public int DrumCapacityKg { get; }

    public bool IsFrontLoading { get; }

    public override string TypeName
    {
        get { return "Washing Machine"; }
    }

    public override decimal CalculateRepairCost()
    {
        decimal laborFactor = IsFrontLoading ? FrontLoadingLaborFactor : 1m;
        decimal handlingFee = DrumCapacityKg >= HeavyDrumThresholdKg ? HeavyDrumHandlingFee : 0m;

        return CalculateBaseCost(laborFactor) + handlingFee;
    }

    public override string ToString()
    {
        string loading = IsFrontLoading ? "front loading" : "top loading";
        return $"{base.ToString()} ({DrumCapacityKg} kg, {loading})";
    }
}
