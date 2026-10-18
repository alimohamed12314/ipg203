using ApplianceRepairCenter.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ApplianceRepairCenter.Tests;

/// <summary>
/// Each appliance type prices its repair with its own rules. The expected amounts are worked out
/// here from the tariff: inspection 25,000 and labor 60,000 per hour, plus the type's own fees.
/// The standard operation of these tests is 2 hours of labor and 50,000 of spare parts.
/// </summary>
[TestClass]
public class RepairCostTests
{
    private const decimal Inspection = 25_000m;
    private const decimal Parts = 50_000m;
    private const decimal TwoHoursOfLabor = 2 * 60_000m;

    [TestMethod]
    public void Refrigerator_AddsACoolingTestPricedByCapacity()
    {
        Refrigerator refrigerator = WithStandardOperation(TestData.Refrigerator(capacityLiters: 300, isNoFrost: false));

        // cooling test: 300 liters x 100
        Assert.AreEqual(Inspection + Parts + TwoHoursOfLabor + 30_000m, refrigerator.CalculateRepairCost());
        Assert.AreEqual(225_000m, refrigerator.CalculateRepairCost());
    }

    [TestMethod]
    public void Refrigerator_ChargesTwentyPercentMoreLaborForNoFrost()
    {
        Refrigerator refrigerator = WithStandardOperation(TestData.Refrigerator(capacityLiters: 300, isNoFrost: true));

        Assert.AreEqual(Inspection + Parts + (TwoHoursOfLabor * 1.20m) + 30_000m, refrigerator.CalculateRepairCost());
        Assert.AreEqual(249_000m, refrigerator.CalculateRepairCost());
    }

    [TestMethod]
    public void WashingMachine_TopLoadingPaysOnlyTheSharedPart()
    {
        WashingMachine washer = WithStandardOperation(TestData.WashingMachine(drumCapacityKg: 7, isFrontLoading: false));

        Assert.AreEqual(Inspection + Parts + TwoHoursOfLabor, washer.CalculateRepairCost());
        Assert.AreEqual(195_000m, washer.CalculateRepairCost());
    }

    [TestMethod]
    public void WashingMachine_ChargesTwentyFivePercentMoreLaborForFrontLoading()
    {
        WashingMachine washer = WithStandardOperation(TestData.WashingMachine(drumCapacityKg: 7, isFrontLoading: true));

        Assert.AreEqual(Inspection + Parts + (TwoHoursOfLabor * 1.25m), washer.CalculateRepairCost());
        Assert.AreEqual(225_000m, washer.CalculateRepairCost());
    }

    [DataTestMethod]
    [DataRow(8, 195_000)]
    [DataRow(9, 225_000)]
    [DataRow(12, 225_000)]
    public void WashingMachine_AddsAHandlingFeeFromNineKilograms(int drumCapacityKg, int expectedCost)
    {
        WashingMachine washer = WithStandardOperation(TestData.WashingMachine(drumCapacityKg: drumCapacityKg));

        Assert.AreEqual((decimal)expectedCost, washer.CalculateRepairCost());
    }

    [DataTestMethod]
    [DataRow(9000, 1)]
    [DataRow(12000, 1)]
    [DataRow(12001, 2)]
    [DataRow(18000, 2)]
    [DataRow(24000, 2)]
    [DataRow(30000, 3)]
    public void AirConditioner_ChargesRefrigerantForEachStartedTon(int coolingCapacityBtu, int startedTons)
    {
        AirConditioner airConditioner = WithStandardOperation(
            TestData.AirConditioner(coolingCapacityBtu: coolingCapacityBtu, isSplitUnit: false));

        Assert.AreEqual(Inspection + Parts + TwoHoursOfLabor + (startedTons * 35_000m), airConditioner.CalculateRepairCost());
    }

    [TestMethod]
    public void AirConditioner_AddsAVisitFeeForASplitUnit()
    {
        AirConditioner window = WithStandardOperation(TestData.AirConditioner("AC-TEST-0001", 12000, isSplitUnit: false));
        AirConditioner split = WithStandardOperation(TestData.AirConditioner("AC-TEST-0002", 12000, isSplitUnit: true));

        Assert.AreEqual(230_000m, window.CalculateRepairCost());
        Assert.AreEqual(270_000m, split.CalculateRepairCost());
    }

    [TestMethod]
    public void BeforeAnyWork_TheCostIsTheInspectionAndTheFeesOfTheType()
    {
        Assert.AreEqual(Inspection + 30_000m, TestData.Refrigerator(capacityLiters: 300).CalculateRepairCost());
        Assert.AreEqual(Inspection, TestData.WashingMachine(drumCapacityKg: 7).CalculateRepairCost());
        Assert.AreEqual(Inspection + 30_000m, TestData.WashingMachine(drumCapacityKg: 10).CalculateRepairCost());
        Assert.AreEqual(Inspection + 35_000m, TestData.AirConditioner(isSplitUnit: false).CalculateRepairCost());
        Assert.AreEqual(Inspection + 35_000m + 40_000m, TestData.AirConditioner(isSplitUnit: true).CalculateRepairCost());
    }

    [TestMethod]
    public void TheCost_GrowsWithEveryRecordedOperation()
    {
        WashingMachine washer = TestData.WashingMachine(drumCapacityKg: 7, isFrontLoading: false);
        washer.StartRepair();

        washer.AddOperation(TestData.Operation(laborHours: 1m, partsCost: 45_000m));
        Assert.AreEqual(Inspection + 45_000m + 60_000m, washer.CalculateRepairCost());

        washer.AddOperation(TestData.Operation(laborHours: 2.5m, partsCost: 120_000m));
        Assert.AreEqual(Inspection + 165_000m + (3.5m * 60_000m), washer.CalculateRepairCost());
    }

    [TestMethod]
    public void TheSameWork_CostsDifferentlyOnEachApplianceType()
    {
        // One list of the abstract type; each element answers with its own rules.
        Appliance[] appliances =
        {
            WithStandardOperation(TestData.Refrigerator(capacityLiters: 380, isNoFrost: true)),
            WithStandardOperation(TestData.WashingMachine(drumCapacityKg: 9, isFrontLoading: true)),
            WithStandardOperation(TestData.AirConditioner(coolingCapacityBtu: 18000, isSplitUnit: true)),
        };

        Assert.AreEqual(257_000m, appliances[0].CalculateRepairCost());
        Assert.AreEqual(255_000m, appliances[1].CalculateRepairCost());
        Assert.AreEqual(305_000m, appliances[2].CalculateRepairCost());
    }

    private static T WithStandardOperation<T>(T appliance)
        where T : Appliance
    {
        appliance.StartRepair();
        appliance.AddOperation(TestData.Operation());
        return appliance;
    }
}
