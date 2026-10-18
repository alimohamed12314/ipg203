using System;
using System.Collections.Generic;
using System.Linq;
using ApplianceRepairCenter.Enums;
using ApplianceRepairCenter.Interfaces;
using ApplianceRepairCenter.Models;
using ApplianceRepairCenter.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ApplianceRepairCenter.Tests;

/// <summary>
/// The main class: one list of appliances of different types, handled through the same contract.
/// </summary>
[TestClass]
public class RepairCenterTests
{
    [TestMethod]
    public void Register_AddsTheApplianceToTheCenter()
    {
        RepairCenter center = new RepairCenter("  Test Center ");
        Refrigerator refrigerator = TestData.Refrigerator();

        center.Register(refrigerator);

        Assert.AreEqual("Test Center", center.Name);
        Assert.AreEqual(1, center.Appliances.Count);
        Assert.AreSame(refrigerator, center.Appliances[0]);
    }

    [TestMethod]
    public void Register_RefusesNullAndASerialNumberAlreadyRegistered()
    {
        RepairCenter center = new RepairCenter("Test Center");
        center.Register(TestData.Refrigerator("RF-TEST-0001"));

        Assert.ThrowsException<ArgumentNullException>(() => center.Register(null!));
        Assert.ThrowsException<InvalidOperationException>(() => center.Register(TestData.WashingMachine("RF-TEST-0001")));
        Assert.AreEqual(1, center.Appliances.Count);
    }

    [TestMethod]
    public void ACenter_NeedsAName()
    {
        Assert.ThrowsException<ArgumentException>(() => new RepairCenter("  "));
    }

    [TestMethod]
    public void FindBySerialNumber_IgnoresLetterCaseAndReturnsNullWhenUnknown()
    {
        RepairCenter center = new RepairCenter("Test Center");
        Refrigerator refrigerator = TestData.Refrigerator("RF-TEST-0001");
        center.Register(refrigerator);

        Assert.AreSame(refrigerator, center.FindBySerialNumber("RF-TEST-0001"));
        Assert.AreSame(refrigerator, center.FindBySerialNumber("rf-test-0001"));
        Assert.IsNull(center.FindBySerialNumber("RF-TEST-9999"));
    }

    [TestMethod]
    public void Appliances_CannotBeChangedFromOutside()
    {
        RepairCenter center = new RepairCenter("Test Center");
        center.Register(TestData.Refrigerator());

        IList<IAppliance> view = (IList<IAppliance>)center.Appliances;

        Assert.ThrowsException<NotSupportedException>(() => view.Add(TestData.WashingMachine()));
        Assert.ThrowsException<NotSupportedException>(view.Clear);
        Assert.AreEqual(1, center.Appliances.Count);
    }

    [TestMethod]
    public void FollowUp_SeparatesRepairedAppliancesFromThoseStillUnderMaintenance()
    {
        RepairCenter center = new RepairCenter("Test Center");
        Refrigerator received = TestData.Refrigerator("RF-TEST-0001");
        WashingMachine underRepair = TestData.WashingMachine("WM-TEST-0001");
        AirConditioner repaired = TestData.AirConditioner("AC-TEST-0001");
        WashingMachine delivered = TestData.WashingMachine("WM-TEST-0002");
        foreach (Appliance appliance in new Appliance[] { received, underRepair, repaired, delivered })
        {
            center.Register(appliance);
        }

        underRepair.StartRepair();
        TestData.Repaired(repaired);
        TestData.Repaired(delivered).DeliverToOwner();

        CollectionAssert.AreEqual(
            new IAppliance[] { received, underRepair }, center.GetAppliancesUnderMaintenance().ToList());
        CollectionAssert.AreEqual(
            new IAppliance[] { repaired, delivered }, center.GetRepairedAppliances().ToList());
    }

    [TestMethod]
    public void CalculateTotalRepairCost_AddsWhatEachTypeChargesByItsOwnRules()
    {
        RepairCenter center = new RepairCenter("Test Center");
        Refrigerator refrigerator = TestData.Refrigerator(capacityLiters: 380, isNoFrost: true);
        WashingMachine washer = TestData.WashingMachine(drumCapacityKg: 9, isFrontLoading: true);
        AirConditioner airConditioner = TestData.AirConditioner(coolingCapacityBtu: 18000, isSplitUnit: true);
        foreach (Appliance appliance in new Appliance[] { refrigerator, washer, airConditioner })
        {
            center.Register(appliance);
            appliance.StartRepair();
            appliance.AddOperation(TestData.Operation());
        }

        // 257,000 + 255,000 + 305,000: see RepairCostTests for each amount.
        Assert.AreEqual(817_000m, center.CalculateTotalRepairCost());
        Assert.AreEqual(
            refrigerator.CalculateRepairCost() + washer.CalculateRepairCost() + airConditioner.CalculateRepairCost(),
            center.CalculateTotalRepairCost());
    }

    [TestMethod]
    public void CalculateTotalRepairCost_IsZeroForAnEmptyCenter()
    {
        Assert.AreEqual(0m, new RepairCenter("Test Center").CalculateTotalRepairCost());
    }

    [TestMethod]
    public void CalculateTotalCost_PricesAnyGroupOfAppliances()
    {
        IAppliance[] group =
        {
            new FakeAppliance("FAKE-0001", 10_000m),
            new FakeAppliance("FAKE-0002", 5_500m),
        };

        Assert.AreEqual(15_500m, RepairCenter.CalculateTotalCost(group));
        Assert.AreEqual(0m, RepairCenter.CalculateTotalCost(Array.Empty<IAppliance>()));
        Assert.ThrowsException<ArgumentNullException>(() => RepairCenter.CalculateTotalCost(null!));
    }

    [TestMethod]
    public void TheCenter_HandlesAnyImplementationOfTheContract()
    {
        RepairCenter center = new RepairCenter("Test Center");
        FakeAppliance fake = new FakeAppliance("FAKE-0001", 10_000m);
        int completed = 0;
        center.RepairCompleted += (_, _) => completed++;

        center.Register(fake);
        center.Register(TestData.WashingMachine());
        fake.StartRepair();
        fake.CompleteRepair();

        Assert.AreEqual(1, completed);
        Assert.AreSame(fake, center.GetRepairedAppliances().Single());
        Assert.AreEqual(10_000m + 25_000m, center.CalculateTotalRepairCost());
    }

    [TestMethod]
    public void StaticCounts_FollowEveryApplianceOfEveryCenter()
    {
        // The counts are shared by all centers and by all tests, so only their changes are checked.
        Dictionary<ApplianceStatus, int> before = CurrentCounts();
        int totalBefore = RepairCenter.TotalRegistered;
        RepairCenter firstCenter = new RepairCenter("First Center");
        RepairCenter secondCenter = new RepairCenter("Second Center");
        Refrigerator refrigerator = TestData.Refrigerator();
        WashingMachine washer = TestData.WashingMachine();

        firstCenter.Register(refrigerator);
        secondCenter.Register(washer);
        AssertChanges(before, received: 2);
        Assert.AreEqual(totalBefore + 2, RepairCenter.TotalRegistered);

        refrigerator.StartRepair();
        washer.StartRepair();
        AssertChanges(before, underRepair: 2);
        Assert.AreEqual(before[ApplianceStatus.UnderRepair] + 2, RepairCenter.UnderRepairCount);

        refrigerator.AddOperation(TestData.Operation());
        refrigerator.CompleteRepair();
        AssertChanges(before, underRepair: 1, repaired: 1);

        refrigerator.DeliverToOwner();
        AssertChanges(before, underRepair: 1, delivered: 1);
        Assert.AreEqual(totalBefore + 2, RepairCenter.TotalRegistered);
    }

    [TestMethod]
    public void StaticCounts_IgnoreAppliancesThatNoCenterRegistered()
    {
        Dictionary<ApplianceStatus, int> before = CurrentCounts();

        TestData.Repaired(TestData.Refrigerator());
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => TestData.Refrigerator(capacityLiters: 0));

        AssertChanges(before);
    }

    [TestMethod]
    public void StaticCounts_DoNotMoveWhenAStepIsRefused()
    {
        RepairCenter center = new RepairCenter("Test Center");
        Refrigerator refrigerator = TestData.Refrigerator();
        center.Register(refrigerator);
        Dictionary<ApplianceStatus, int> before = CurrentCounts();

        Assert.ThrowsException<InvalidOperationException>(refrigerator.DeliverToOwner);
        Assert.ThrowsException<InvalidOperationException>(() => center.Register(refrigerator));

        AssertChanges(before);
    }

    [TestMethod]
    public void TotalRegistered_IsTheSumOfTheCountsOfAllStatuses()
    {
        new RepairCenter("Test Center").Register(TestData.Refrigerator());

        Assert.AreEqual(CurrentCounts().Values.Sum(), RepairCenter.TotalRegistered);
    }

    private static Dictionary<ApplianceStatus, int> CurrentCounts()
    {
        return Enum.GetValues<ApplianceStatus>().ToDictionary(status => status, RepairCenter.CountByStatus);
    }

    private static void AssertChanges(
        Dictionary<ApplianceStatus, int> before, int received = 0, int underRepair = 0, int repaired = 0, int delivered = 0)
    {
        Assert.AreEqual(before[ApplianceStatus.Received] + received, RepairCenter.CountByStatus(ApplianceStatus.Received));
        Assert.AreEqual(before[ApplianceStatus.UnderRepair] + underRepair, RepairCenter.CountByStatus(ApplianceStatus.UnderRepair));
        Assert.AreEqual(before[ApplianceStatus.Repaired] + repaired, RepairCenter.CountByStatus(ApplianceStatus.Repaired));
        Assert.AreEqual(before[ApplianceStatus.Delivered] + delivered, RepairCenter.CountByStatus(ApplianceStatus.Delivered));
    }
}
