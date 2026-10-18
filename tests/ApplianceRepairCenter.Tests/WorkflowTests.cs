using System;
using System.Collections.Generic;
using ApplianceRepairCenter.Enums;
using ApplianceRepairCenter.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ApplianceRepairCenter.Tests;

/// <summary>
/// The repair workflow: Received, UnderRepair, Repaired, Delivered - in that order only.
/// </summary>
[TestClass]
public class WorkflowTests
{
    [TestMethod]
    public void Appliance_GoesThroughTheFourStagesInOrder()
    {
        WashingMachine washer = TestData.WashingMachine();
        Assert.AreEqual(ApplianceStatus.Received, washer.Status);

        washer.StartRepair();
        Assert.AreEqual(ApplianceStatus.UnderRepair, washer.Status);
        Assert.IsFalse(washer.IsRepaired);

        washer.AddOperation(TestData.Operation());
        washer.CompleteRepair();
        Assert.AreEqual(ApplianceStatus.Repaired, washer.Status);
        Assert.IsTrue(washer.IsRepaired);

        washer.DeliverToOwner();
        Assert.AreEqual(ApplianceStatus.Delivered, washer.Status);
        Assert.IsTrue(washer.IsRepaired);
    }

    [TestMethod]
    public void StartRepair_IsRefusedOnceTheRepairHasStarted()
    {
        WashingMachine washer = TestData.WashingMachine();
        washer.StartRepair();

        Assert.ThrowsException<InvalidOperationException>(washer.StartRepair);
        Assert.AreEqual(ApplianceStatus.UnderRepair, washer.Status);
    }

    [TestMethod]
    public void CompleteRepair_IsRefusedBeforeTheRepairStarts()
    {
        WashingMachine washer = TestData.WashingMachine();

        Assert.ThrowsException<InvalidOperationException>(washer.CompleteRepair);
        Assert.AreEqual(ApplianceStatus.Received, washer.Status);
    }

    [TestMethod]
    public void CompleteRepair_IsRefusedWhenNoWorkWasRecorded()
    {
        WashingMachine washer = TestData.WashingMachine();
        washer.StartRepair();

        InvalidOperationException error = Assert.ThrowsException<InvalidOperationException>(washer.CompleteRepair);

        StringAssert.Contains(error.Message, "no recorded work");
        Assert.AreEqual(ApplianceStatus.UnderRepair, washer.Status);
    }

    [TestMethod]
    public void DeliverToOwner_IsRefusedBeforeTheRepairIsComplete()
    {
        WashingMachine received = TestData.WashingMachine("WM-TEST-0001");
        WashingMachine underRepair = TestData.WashingMachine("WM-TEST-0002");
        underRepair.StartRepair();

        Assert.ThrowsException<InvalidOperationException>(received.DeliverToOwner);
        Assert.ThrowsException<InvalidOperationException>(underRepair.DeliverToOwner);
    }

    [TestMethod]
    public void DeliveredAppliance_AcceptsNoFurtherStep()
    {
        WashingMachine washer = TestData.Repaired(TestData.WashingMachine());
        washer.DeliverToOwner();

        Assert.ThrowsException<InvalidOperationException>(washer.StartRepair);
        Assert.ThrowsException<InvalidOperationException>(washer.CompleteRepair);
        Assert.ThrowsException<InvalidOperationException>(washer.DeliverToOwner);
        Assert.ThrowsException<InvalidOperationException>(() => washer.AddOperation(TestData.Operation()));
        Assert.AreEqual(ApplianceStatus.Delivered, washer.Status);
    }

    [TestMethod]
    public void AddOperation_RecordsWorkInOrderWhileUnderRepair()
    {
        WashingMachine washer = TestData.WashingMachine();
        washer.StartRepair();
        RepairOperation first = TestData.Operation(1m, 10_000m);
        RepairOperation second = TestData.Operation(2m, 20_000m);

        washer.AddOperation(first);
        washer.AddOperation(second);

        Assert.AreEqual(2, washer.Operations.Count);
        Assert.AreSame(first, washer.Operations[0]);
        Assert.AreSame(second, washer.Operations[1]);
    }

    [TestMethod]
    public void AddOperation_IsRefusedUnlessUnderRepair()
    {
        WashingMachine received = TestData.WashingMachine("WM-TEST-0001");
        WashingMachine repaired = TestData.Repaired(TestData.WashingMachine("WM-TEST-0002"));

        Assert.ThrowsException<InvalidOperationException>(() => received.AddOperation(TestData.Operation()));
        Assert.ThrowsException<InvalidOperationException>(() => repaired.AddOperation(TestData.Operation()));
        Assert.AreEqual(0, received.Operations.Count);
        Assert.AreEqual(1, repaired.Operations.Count);
    }

    [TestMethod]
    public void AddOperation_RejectsNullAndWorkDatedBeforeIntake()
    {
        Refrigerator refrigerator = TestData.Refrigerator(receivedDaysAgo: 2);
        refrigerator.StartRepair();
        RepairOperation tooEarly = new RepairOperation("Work", 1m, 0m, TestData.Today.AddDays(-5));

        Assert.ThrowsException<ArgumentNullException>(() => refrigerator.AddOperation(null!));
        Assert.ThrowsException<ArgumentException>(() => refrigerator.AddOperation(tooEarly));
        Assert.AreEqual(0, refrigerator.Operations.Count);
    }

    [TestMethod]
    public void Operations_CannotBeChangedFromOutside()
    {
        WashingMachine washer = TestData.WashingMachine();
        washer.StartRepair();
        washer.AddOperation(TestData.Operation());

        IList<RepairOperation> view = (IList<RepairOperation>)washer.Operations;

        Assert.IsTrue(view.IsReadOnly);
        Assert.ThrowsException<NotSupportedException>(() => view.Add(TestData.Operation()));
        Assert.ThrowsException<NotSupportedException>(view.Clear);
        Assert.AreEqual(1, washer.Operations.Count);
    }

    [TestMethod]
    public void ExpectedDeliveryDate_CanBePostponedButNotSetBeforeIntake()
    {
        Refrigerator refrigerator = TestData.Refrigerator(receivedDaysAgo: 3, dueInDays: 2);
        DateTime original = refrigerator.ExpectedDeliveryDate;

        refrigerator.ExpectedDeliveryDate = TestData.Today.AddDays(10).AddHours(8);
        Assert.AreEqual(TestData.Today.AddDays(10), refrigerator.ExpectedDeliveryDate);

        refrigerator.ExpectedDeliveryDate = refrigerator.ReceivedDate;
        Assert.AreEqual(refrigerator.ReceivedDate, refrigerator.ExpectedDeliveryDate);

        Assert.ThrowsException<ArgumentException>(
            () => refrigerator.ExpectedDeliveryDate = refrigerator.ReceivedDate.AddDays(-1));
        Assert.AreEqual(refrigerator.ReceivedDate, refrigerator.ExpectedDeliveryDate);
        Assert.AreNotEqual(original, refrigerator.ExpectedDeliveryDate);
    }

    [TestMethod]
    public void IsOverdue_IsTrueOnlyAfterThePromisedDayWhileNotRepaired()
    {
        Refrigerator refrigerator = TestData.Refrigerator(receivedDaysAgo: 5, dueInDays: -2);
        DateTime due = refrigerator.ExpectedDeliveryDate;

        Assert.IsFalse(refrigerator.IsOverdue(due.AddDays(-1)));
        Assert.IsFalse(refrigerator.IsOverdue(due));
        Assert.IsFalse(refrigerator.IsOverdue(due.AddHours(20)));
        Assert.IsTrue(refrigerator.IsOverdue(due.AddDays(1)));
        Assert.IsTrue(refrigerator.IsOverdue(TestData.Today));

        refrigerator.StartRepair();
        Assert.IsTrue(refrigerator.IsOverdue(TestData.Today));

        refrigerator.AddOperation(TestData.Operation());
        refrigerator.CompleteRepair();
        Assert.IsFalse(refrigerator.IsOverdue(TestData.Today));
    }
}
