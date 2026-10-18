using System;

namespace ApplianceRepairCenter.Models;

/// <summary>
/// An air conditioner. A split unit is fixed to the wall and repaired where it is installed, and
/// the refrigerant it needs grows with its cooling capacity.
/// </summary>
public sealed class AirConditioner : Appliance
{
    // Cooling capacity is sold in tons: one ton is 12,000 BTU per hour.
    private const int BtuPerTon = 12_000;

    // The refrigerant pressure is checked and topped up; charged for each started ton.
    private const decimal RefrigerantFeePerTon = 35_000m;

    // A split unit cannot be brought to the workshop: the technician travels to the customer.
    private const decimal OnSiteVisitFee = 40_000m;

    public AirConditioner(
        string serialNumber,
        string brand,
        string model,
        Customer owner,
        string faultDescription,
        DateTime receivedDate,
        DateTime expectedDeliveryDate,
        int coolingCapacityBtu,
        bool isSplitUnit)
        : base(serialNumber, brand, model, owner, faultDescription, receivedDate, expectedDeliveryDate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(coolingCapacityBtu);

        CoolingCapacityBtu = coolingCapacityBtu;
        IsSplitUnit = isSplitUnit;
    }

    public int CoolingCapacityBtu { get; }

    public bool IsSplitUnit { get; }

    public override string TypeName
    {
        get { return "Air Conditioner"; }
    }

    public override decimal CalculateRepairCost()
    {
        decimal startedTons = Math.Ceiling((decimal)CoolingCapacityBtu / BtuPerTon);
        decimal refrigerantFee = startedTons * RefrigerantFeePerTon;
        decimal visitFee = IsSplitUnit ? OnSiteVisitFee : 0m;

        return CalculateBaseCost() + refrigerantFee + visitFee;
    }

    public override string ToString()
    {
        string unit = IsSplitUnit ? "split" : "window";
        return $"{base.ToString()} ({CoolingCapacityBtu} BTU, {unit})";
    }
}
