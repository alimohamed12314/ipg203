using System;

namespace ApplianceRepairCenter.Models;

/// <summary>
/// A refrigerator. Its cooling circuit is sealed, so every repair ends with a test of that circuit.
/// </summary>
public sealed class Refrigerator : Appliance
{
    // A no-frost model hides its fan, heater and sensors behind panels: more work to reach them.
    private const decimal NoFrostLaborFactor = 1.20m;

    // The cooling circuit is tested for leaks and cooling power; a larger cabinet takes longer.
    private const decimal CoolingTestFeePerLiter = 100m;

    public Refrigerator(
        string serialNumber,
        string brand,
        string model,
        Customer owner,
        string faultDescription,
        DateTime receivedDate,
        DateTime expectedDeliveryDate,
        int capacityLiters,
        bool isNoFrost)
        : base(serialNumber, brand, model, owner, faultDescription, receivedDate, expectedDeliveryDate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacityLiters);

        CapacityLiters = capacityLiters;
        IsNoFrost = isNoFrost;
    }

    public int CapacityLiters { get; }

    public bool IsNoFrost { get; }

    public override string TypeName
    {
        get { return "Refrigerator"; }
    }

    public override decimal CalculateRepairCost()
    {
        decimal laborFactor = IsNoFrost ? NoFrostLaborFactor : 1m;
        decimal coolingTestFee = CapacityLiters * CoolingTestFeePerLiter;

        return CalculateBaseCost(laborFactor) + coolingTestFee;
    }

    public override string ToString()
    {
        string cooling = IsNoFrost ? "no-frost" : "direct cool";
        return $"{base.ToString()} ({CapacityLiters} L, {cooling})";
    }
}
