using System;
using ApplianceRepairCenter.Enums;
using ApplianceRepairCenter.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ApplianceRepairCenter.Tests;

/// <summary>
/// An object is either created with valid data or not created at all.
/// </summary>
[TestClass]
public class ConstructionTests
{
    private static readonly DateTime s_today = TestData.Today;

    [TestMethod]
    public void NewAppliance_KeepsItsIntakeDataAndStartsAsReceived()
    {
        Customer owner = TestData.Owner();
        Refrigerator refrigerator = new Refrigerator(
            "RF-2024-0001", " Samsung ", " RT38 ", owner, " Not cooling ",
            s_today.AddDays(-3).AddHours(14), s_today.AddDays(2).AddHours(9), 380, true);

        Assert.AreEqual("RF-2024-0001", refrigerator.SerialNumber);
        Assert.AreEqual("Samsung", refrigerator.Brand);
        Assert.AreEqual("RT38", refrigerator.Model);
        Assert.AreSame(owner, refrigerator.Owner);
        Assert.AreEqual("Not cooling", refrigerator.FaultDescription);
        Assert.AreEqual(s_today.AddDays(-3), refrigerator.ReceivedDate);
        Assert.AreEqual(s_today.AddDays(2), refrigerator.ExpectedDeliveryDate);
        Assert.AreEqual(380, refrigerator.CapacityLiters);
        Assert.IsTrue(refrigerator.IsNoFrost);
        Assert.AreEqual(ApplianceStatus.Received, refrigerator.Status);
        Assert.IsFalse(refrigerator.IsRepaired);
        Assert.AreEqual(0, refrigerator.Operations.Count);
    }

    [DataTestMethod]
    [DataRow("rf-2024-0001")]
    [DataRow("RF 1")]
    [DataRow("")]
    public void Appliance_RejectsAnInvalidSerialNumber(string serialNumber)
    {
        ArgumentException error = Assert.ThrowsException<ArgumentException>(
            () => TestData.Refrigerator(serialNumber: serialNumber));

        Assert.AreEqual("serialNumber", error.ParamName);
    }

    [DataTestMethod]
    [DataRow("", "RT38", "Not cooling", "brand")]
    [DataRow("Samsung", "  ", "Not cooling", "model")]
    [DataRow("Samsung", "RT38", "", "faultDescription")]
    public void Appliance_RequiresBrandModelAndFault(string brand, string model, string fault, string expectedParameter)
    {
        ArgumentException error = Assert.ThrowsException<ArgumentException>(
            () => new Refrigerator("RF-2024-0001", brand, model, TestData.Owner(), fault, s_today, s_today, 300, false));

        Assert.AreEqual(expectedParameter, error.ParamName);
    }

    [TestMethod]
    public void Appliance_RequiresAnOwner()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new Refrigerator("RF-2024-0001", "Samsung", "RT38", null!, "Not cooling", s_today, s_today, 300, false));
    }

    [TestMethod]
    public void Appliance_CannotBeReceivedInTheFuture()
    {
        ArgumentException error = Assert.ThrowsException<ArgumentException>(
            () => TestData.Refrigerator(receivedDaysAgo: -1, dueInDays: 5));

        Assert.AreEqual("receivedDate", error.ParamName);
    }

    [TestMethod]
    public void Appliance_CannotBePromisedBeforeItWasReceived()
    {
        Assert.ThrowsException<ArgumentException>(
            () => TestData.Refrigerator(receivedDaysAgo: 3, dueInDays: -4));
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-5)]
    public void EachApplianceType_RejectsACapacityThatIsNotPositive(int capacity)
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => TestData.Refrigerator(capacityLiters: capacity));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => TestData.WashingMachine(drumCapacityKg: capacity));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => TestData.AirConditioner(coolingCapacityBtu: capacity));
    }

    [TestMethod]
    public void EachApplianceType_DescribesItself()
    {
        Assert.AreEqual("Refrigerator", TestData.Refrigerator().TypeName);
        Assert.AreEqual("Washing Machine", TestData.WashingMachine().TypeName);
        Assert.AreEqual("Air Conditioner", TestData.AirConditioner().TypeName);

        Assert.AreEqual(
            "Refrigerator Samsung RT38 (300 L, no-frost)", TestData.Refrigerator(isNoFrost: true).ToString());
        Assert.AreEqual(
            "Refrigerator Samsung RT38 (300 L, direct cool)", TestData.Refrigerator(isNoFrost: false).ToString());
        Assert.AreEqual(
            "Washing Machine LG F4V5 (7 kg, front loading)", TestData.WashingMachine(isFrontLoading: true).ToString());
        Assert.AreEqual(
            "Washing Machine LG F4V5 (7 kg, top loading)", TestData.WashingMachine(isFrontLoading: false).ToString());
        Assert.AreEqual(
            "Air Conditioner Gree Pular (12000 BTU, split)", TestData.AirConditioner(isSplitUnit: true).ToString());
        Assert.AreEqual(
            "Air Conditioner Gree Pular (12000 BTU, window)", TestData.AirConditioner(isSplitUnit: false).ToString());
    }

    [TestMethod]
    public void Customer_KeepsNameAndPhone()
    {
        Customer customer = new Customer("Rana Haddad", "0933555120");

        Assert.AreEqual("Rana Haddad", customer.Name);
        Assert.AreEqual("0933555120", customer.PhoneNumber);
        Assert.AreEqual("Rana Haddad (0933555120)", customer.ToString());
    }

    [DataTestMethod]
    [DataRow("R2", "0933555120", "name")]
    [DataRow("Rana Haddad", "12345", "phoneNumber")]
    public void Customer_RejectsAnInvalidNameOrPhone(string name, string phoneNumber, string expectedParameter)
    {
        ArgumentException error = Assert.ThrowsException<ArgumentException>(() => new Customer(name, phoneNumber));

        Assert.AreEqual(expectedParameter, error.ParamName);
    }

    [TestMethod]
    public void RepairOperation_KeepsWhatWasDone()
    {
        RepairOperation operation = new RepairOperation(" Replace drive belt ", 1.5m, 45_000m, s_today.AddHours(10));

        Assert.AreEqual("Replace drive belt", operation.Description);
        Assert.AreEqual(1.5m, operation.LaborHours);
        Assert.AreEqual(45_000m, operation.PartsCost);
        Assert.AreEqual(s_today, operation.PerformedOn);
    }

    [TestMethod]
    public void RepairOperation_RejectsInvalidValues()
    {
        Assert.ThrowsException<ArgumentException>(() => new RepairOperation(" ", 1m, 0m, s_today));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new RepairOperation("Work", 0m, 0m, s_today));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new RepairOperation("Work", -1m, 0m, s_today));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new RepairOperation("Work", 1m, -1m, s_today));
        Assert.ThrowsException<ArgumentException>(() => new RepairOperation("Work", 1m, 0m, s_today.AddDays(1)));
    }

    [TestMethod]
    public void RepairOperation_AllowsWorkWithoutSpareParts()
    {
        RepairOperation operation = new RepairOperation("Clean filter", 0.5m, 0m, s_today);

        Assert.AreEqual(0m, operation.PartsCost);
    }
}
