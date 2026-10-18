using System;
using System.Collections.Generic;
using ApplianceRepairCenter.Enums;
using ApplianceRepairCenter.Events;
using ApplianceRepairCenter.Interfaces;
using ApplianceRepairCenter.Models;
using ApplianceRepairCenter.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ApplianceRepairCenter.Tests;

/// <summary>
/// The delegate and the events: who is told what, and when.
/// </summary>
[TestClass]
public class EventTests
{
    [TestMethod]
    public void StatusChanged_IsRaisedOnEveryStepWithTheOldAndTheNewStatus()
    {
        Refrigerator refrigerator = TestData.Refrigerator();
        List<StatusChangedEventArgs> received = new List<StatusChangedEventArgs>();
        object? lastSender = null;
        refrigerator.StatusChanged += (sender, e) =>
        {
            lastSender = sender;
            received.Add(e);
        };

        refrigerator.StartRepair();
        refrigerator.AddOperation(TestData.Operation());
        refrigerator.CompleteRepair();
        refrigerator.DeliverToOwner();

        Assert.AreEqual(3, received.Count);
        Assert.AreSame(refrigerator, lastSender);
        AssertChange(received[0], refrigerator, ApplianceStatus.Received, ApplianceStatus.UnderRepair);
        AssertChange(received[1], refrigerator, ApplianceStatus.UnderRepair, ApplianceStatus.Repaired);
        AssertChange(received[2], refrigerator, ApplianceStatus.Repaired, ApplianceStatus.Delivered);
    }

    [TestMethod]
    public void StatusChanged_IsNotRaisedWhenAStepIsRefused()
    {
        Refrigerator refrigerator = TestData.Refrigerator();
        int raised = 0;
        refrigerator.StatusChanged += (_, _) => raised++;

        Assert.ThrowsException<InvalidOperationException>(refrigerator.DeliverToOwner);
        Assert.ThrowsException<InvalidOperationException>(refrigerator.CompleteRepair);

        Assert.AreEqual(0, raised);
    }

    [TestMethod]
    public void StatusChanged_NeedsNoSubscriber()
    {
        Refrigerator refrigerator = TestData.Refrigerator();

        refrigerator.StartRepair();

        Assert.AreEqual(ApplianceStatus.UnderRepair, refrigerator.Status);
    }

    [TestMethod]
    public void StatusChanged_CallsEverySubscriberInOrderUntilItUnsubscribes()
    {
        Refrigerator refrigerator = TestData.Refrigerator();
        List<string> calls = new List<string>();
        StatusChangedEventHandler first = (_, _) => calls.Add("first");
        StatusChangedEventHandler second = (_, _) => calls.Add("second");
        refrigerator.StatusChanged += first;
        refrigerator.StatusChanged += second;

        refrigerator.StartRepair();
        refrigerator.StatusChanged -= first;
        refrigerator.AddOperation(TestData.Operation());
        refrigerator.CompleteRepair();

        CollectionAssert.AreEqual(new[] { "first", "second", "second" }, calls);
    }

    [TestMethod]
    public void RepairCompleted_IsRaisedByTheCenterOnlyWhenARepairIsFinished()
    {
        RepairCenter center = new RepairCenter("Test Center");
        WashingMachine washer = TestData.WashingMachine();
        center.Register(washer);
        List<StatusChangedEventArgs> completed = new List<StatusChangedEventArgs>();
        object? sender = null;
        center.RepairCompleted += (source, e) =>
        {
            sender = source;
            completed.Add(e);
        };

        washer.StartRepair();
        washer.AddOperation(TestData.Operation());
        Assert.AreEqual(0, completed.Count);

        washer.CompleteRepair();
        Assert.AreEqual(1, completed.Count);
        Assert.AreSame(center, sender);
        AssertChange(completed[0], washer, ApplianceStatus.UnderRepair, ApplianceStatus.Repaired);

        washer.DeliverToOwner();
        Assert.AreEqual(1, completed.Count);
    }

    [TestMethod]
    public void RepairCompleted_ConcernsOnlyTheAppliancesOfThatCenter()
    {
        RepairCenter firstCenter = new RepairCenter("First Center");
        RepairCenter secondCenter = new RepairCenter("Second Center");
        WashingMachine washer = TestData.WashingMachine();
        firstCenter.Register(washer);
        int raisedByFirst = 0;
        int raisedBySecond = 0;
        firstCenter.RepairCompleted += (_, _) => raisedByFirst++;
        secondCenter.RepairCompleted += (_, _) => raisedBySecond++;

        TestData.Repaired(washer);
        TestData.Repaired(TestData.Refrigerator());

        Assert.AreEqual(1, raisedByFirst);
        Assert.AreEqual(0, raisedBySecond);
    }

    [TestMethod]
    public void DeliveryOverdue_IsRaisedForEachLateApplianceWithTheDaysLate()
    {
        RepairCenter center = new RepairCenter("Test Center");
        Refrigerator threeDaysLate = TestData.Refrigerator("RF-TEST-0001", receivedDaysAgo: 9, dueInDays: -3);
        Refrigerator oneDayLate = TestData.Refrigerator("RF-TEST-0002", receivedDaysAgo: 9, dueInDays: -1);
        Refrigerator onTime = TestData.Refrigerator("RF-TEST-0003", receivedDaysAgo: 9, dueInDays: 0);
        Refrigerator lateButRepaired = TestData.Repaired(TestData.Refrigerator("RF-TEST-0004", receivedDaysAgo: 9, dueInDays: -5));
        foreach (Refrigerator refrigerator in new[] { threeDaysLate, oneDayLate, onTime, lateButRepaired })
        {
            center.Register(refrigerator);
        }

        Dictionary<string, int> daysLateBySerialNumber = new Dictionary<string, int>();
        object? sender = null;
        center.DeliveryOverdue += (source, e) =>
        {
            sender = source;
            daysLateBySerialNumber.Add(e.Appliance.SerialNumber, e.DaysLate);
        };

        IReadOnlyList<IAppliance> overdue = center.CheckOverdueDeliveries(TestData.Today);

        Assert.AreSame(center, sender);
        Assert.AreEqual(2, daysLateBySerialNumber.Count);
        Assert.AreEqual(3, daysLateBySerialNumber["RF-TEST-0001"]);
        Assert.AreEqual(1, daysLateBySerialNumber["RF-TEST-0002"]);
        CollectionAssert.AreEqual(new IAppliance[] { threeDaysLate, oneDayLate }, new List<IAppliance>(overdue));
    }

    [TestMethod]
    public void DeliveryOverdue_IsNotRaisedWhenNothingIsLate()
    {
        RepairCenter center = new RepairCenter("Test Center");
        center.Register(TestData.Refrigerator(dueInDays: 2));
        int raised = 0;
        center.DeliveryOverdue += (_, _) => raised++;

        IReadOnlyList<IAppliance> overdue = center.CheckOverdueDeliveries(TestData.Today);

        Assert.AreEqual(0, raised);
        Assert.AreEqual(0, overdue.Count);
    }

    [TestMethod]
    public void CheckOverdueDeliveries_WorksWithoutSubscribers()
    {
        RepairCenter center = new RepairCenter("Test Center");
        center.Register(TestData.Refrigerator(receivedDaysAgo: 9, dueInDays: -3));

        Assert.AreEqual(1, center.CheckOverdueDeliveries(TestData.Today).Count);
    }

    [TestMethod]
    public void TheCenter_ListensFromRegistrationUntilDelivery()
    {
        RepairCenter center = new RepairCenter("Test Center");
        FakeAppliance appliance = new FakeAppliance("FAKE-0001", 10_000m);
        Assert.AreEqual(0, appliance.SubscriberCount);

        center.Register(appliance);
        Assert.AreEqual(1, appliance.SubscriberCount);

        appliance.StartRepair();
        appliance.CompleteRepair();
        Assert.AreEqual(1, appliance.SubscriberCount);

        appliance.DeliverToOwner();
        Assert.AreEqual(0, appliance.SubscriberCount);
    }

    private static void AssertChange(
        StatusChangedEventArgs e, IAppliance appliance, ApplianceStatus previousStatus, ApplianceStatus newStatus)
    {
        Assert.AreSame(appliance, e.Appliance);
        Assert.AreEqual(previousStatus, e.PreviousStatus);
        Assert.AreEqual(newStatus, e.NewStatus);
    }
}
