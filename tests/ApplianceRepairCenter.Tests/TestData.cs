using System;
using ApplianceRepairCenter.Models;

namespace ApplianceRepairCenter.Tests;

/// <summary>
/// Builds valid objects for the tests, so that each test states only the values it is about.
/// </summary>
internal static class TestData
{
    public static readonly DateTime Today = DateTime.Today;

    public static Customer Owner()
    {
        return new Customer("Ahmad Khalil", "0944123456");
    }

    public static Refrigerator Refrigerator(
        string serialNumber = "RF-TEST-0001",
        int capacityLiters = 300,
        bool isNoFrost = false,
        int receivedDaysAgo = 3,
        int dueInDays = 2)
    {
        return new Refrigerator(
            serialNumber, "Samsung", "RT38", Owner(), "Not cooling",
            Today.AddDays(-receivedDaysAgo), Today.AddDays(dueInDays), capacityLiters, isNoFrost);
    }

    public static WashingMachine WashingMachine(
        string serialNumber = "WM-TEST-0001",
        int drumCapacityKg = 7,
        bool isFrontLoading = false)
    {
        return new WashingMachine(
            serialNumber, "LG", "F4V5", Owner(), "The drum does not spin",
            Today.AddDays(-3), Today.AddDays(2), drumCapacityKg, isFrontLoading);
    }

    public static AirConditioner AirConditioner(
        string serialNumber = "AC-TEST-0001",
        int coolingCapacityBtu = 12000,
        bool isSplitUnit = false)
    {
        return new AirConditioner(
            serialNumber, "Gree", "Pular", Owner(), "Weak cooling",
            Today.AddDays(-3), Today.AddDays(2), coolingCapacityBtu, isSplitUnit);
    }

    /// <summary>One piece of work: 2 hours of labor and 50,000 of spare parts unless stated otherwise.</summary>
    public static RepairOperation Operation(decimal laborHours = 2m, decimal partsCost = 50_000m)
    {
        return new RepairOperation("Test work", laborHours, partsCost, Today);
    }

    /// <summary>Takes an appliance to the Repaired status with one standard operation.</summary>
    public static T Repaired<T>(T appliance)
        where T : Appliance
    {
        appliance.StartRepair();
        appliance.AddOperation(Operation());
        appliance.CompleteRepair();
        return appliance;
    }
}
