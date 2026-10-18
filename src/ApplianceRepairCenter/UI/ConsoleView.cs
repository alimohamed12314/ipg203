using System;
using System.Collections.Generic;
using System.Globalization;
using ApplianceRepairCenter.Enums;
using ApplianceRepairCenter.Interfaces;
using ApplianceRepairCenter.Models;
using ApplianceRepairCenter.Services;
using ApplianceRepairCenter.Validation;

namespace ApplianceRepairCenter.UI;

/// <summary>
/// Prints the center's data on the console. Numbers and dates are always written the same way,
/// whatever the language settings of the computer.
/// </summary>
internal static class ConsoleView
{
    private const string Currency = "SYP";
    private const int RuleWidth = 108;

    public static string FormatMoney(decimal amount)
    {
        return $"{amount.ToString("N0", CultureInfo.InvariantCulture)} {Currency}";
    }

    public static string FormatDate(DateTime date)
    {
        return date.ToString(InputValidator.DateFormat, CultureInfo.InvariantCulture);
    }

    public static void PrintTitle(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"--- {title} ---");
    }

    /// <summary>The center's name and how many appliances are at each stage, in every center.</summary>
    public static void PrintDashboard(RepairCenter center)
    {
        List<string> counts = new List<string> { $"Registered: {RepairCenter.TotalRegistered}" };
        foreach (ApplianceStatus status in Enum.GetValues<ApplianceStatus>())
        {
            counts.Add($"{status}: {RepairCenter.CountByStatus(status)}");
        }

        Console.WriteLine();
        Console.WriteLine(new string('=', RuleWidth));
        Console.WriteLine($" {center.Name}");
        Console.WriteLine($" {string.Join("  |  ", counts)}");
        Console.WriteLine(new string('=', RuleWidth));
    }

    /// <summary>One numbered line per appliance. Each appliance describes and prices itself.</summary>
    public static void PrintAppliances(IReadOnlyList<IAppliance> appliances)
    {
        if (appliances.Count == 0)
        {
            Console.WriteLine("  (none)");
            return;
        }

        Console.WriteLine($"  {"#",2}  {"Serial number",-14} {"Appliance",-52} {"Status",-11} {"Due",-10} {"Cost",13}");
        for (int index = 0; index < appliances.Count; index++)
        {
            IAppliance appliance = appliances[index];
            Console.WriteLine(
                $"  {index + 1,2}  {appliance.SerialNumber,-14} {appliance,-52} {appliance.Status,-11} " +
                $"{FormatDate(appliance.ExpectedDeliveryDate),-10} {FormatMoney(appliance.CalculateRepairCost()),13}");
        }
    }

    /// <summary>The full repair ticket of one appliance, with the work recorded on it.</summary>
    public static void PrintDetails(IAppliance appliance)
    {
        Console.WriteLine($"  {appliance}");
        Console.WriteLine($"    Serial number     : {appliance.SerialNumber}");
        Console.WriteLine($"    Owner             : {appliance.Owner}");
        Console.WriteLine($"    Reported fault    : {appliance.FaultDescription}");
        Console.WriteLine($"    Received          : {FormatDate(appliance.ReceivedDate)}");
        Console.WriteLine($"    Expected delivery : {FormatDate(appliance.ExpectedDeliveryDate)}");
        Console.WriteLine($"    Status            : {appliance.Status}");
        Console.WriteLine($"    Operations        : {appliance.Operations.Count}");

        foreach (RepairOperation operation in appliance.Operations)
        {
            string hours = operation.LaborHours.ToString("0.##", CultureInfo.InvariantCulture);
            Console.WriteLine(
                $"      {FormatDate(operation.PerformedOn)}  {operation.Description,-40} {hours,5} h   parts {FormatMoney(operation.PartsCost),13}");
        }

        Console.WriteLine($"    Repair cost       : {FormatMoney(appliance.CalculateRepairCost())}");
    }

    /// <summary>What the repairs cost, split into finished ones and those still in the workshop.</summary>
    public static void PrintCostReport(RepairCenter center)
    {
        IReadOnlyList<IAppliance> underMaintenance = center.GetAppliancesUnderMaintenance();
        IReadOnlyList<IAppliance> repaired = center.GetRepairedAppliances();

        PrintCostLine("Still under maintenance (cost so far)", underMaintenance.Count, RepairCenter.CalculateTotalCost(underMaintenance));
        PrintCostLine("Repaired", repaired.Count, RepairCenter.CalculateTotalCost(repaired));
        PrintCostLine("All repairs", center.Appliances.Count, center.CalculateTotalRepairCost());
    }

    private static void PrintCostLine(string label, int count, decimal total)
    {
        Console.WriteLine($"  {label,-38} {count,3} appliance(s) {FormatMoney(total),15}");
    }
}
