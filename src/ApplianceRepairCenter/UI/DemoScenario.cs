using System;
using ApplianceRepairCenter.Events;
using ApplianceRepairCenter.Interfaces;
using ApplianceRepairCenter.Models;
using ApplianceRepairCenter.Services;
using ApplianceRepairCenter.Validation;

namespace ApplianceRepairCenter.UI;

/// <summary>
/// A ready-made working week at the center: it registers five appliances and takes them through
/// the workflow, so that every feature of the system can be seen without typing anything.
/// </summary>
internal static class DemoScenario
{
    private const string FirstSerialNumber = "RF-2024-0001";

    public static void Run(RepairCenter center)
    {
        if (center.FindBySerialNumber(FirstSerialNumber) is not null)
        {
            Console.WriteLine("  The demo data is already loaded.");
            return;
        }

        // Dates are counted from today, so the scenario is valid whenever it runs.
        DateTime today = DateTime.Today;

        ConsoleView.PrintTitle("1. Intake: five appliances are registered");
        IAppliance lateFridge = new Refrigerator(
            FirstSerialNumber, "Samsung", "RT38", new Customer("Ahmad Khalil", "0944123456"),
            "Not cooling, the compressor never stops", today.AddDays(-9), today.AddDays(-2),
            capacityLiters: 380, isNoFrost: true);
        IAppliance washer = new WashingMachine(
            "WM-2023-0417", "LG", "F4V5", new Customer("Rana Haddad", "0933555120"),
            "The drum does not spin", today.AddDays(-5), today.AddDays(1),
            drumCapacityKg: 9, isFrontLoading: true);
        IAppliance airConditioner = new AirConditioner(
            "AC-2025-0088", "Gree", "Pular", new Customer("Samer Nassar", "0991002003"),
            "Weak cooling and water leaking indoors", today.AddDays(-3), today.AddDays(2),
            coolingCapacityBtu: 18000, isSplitUnit: true);
        IAppliance waitingFridge = new Refrigerator(
            "RF-2022-0730", "Beko", "RDSA240", new Customer("Lina Youssef", "0955667788"),
            "Torn door seal, frost builds up", today.AddDays(-1), today.AddDays(4),
            capacityLiters: 240, isNoFrost: false);
        IAppliance topLoader = new WashingMachine(
            "WM-2021-0099", "Bosch", "WOT24", new Customer("Khaled Mansour", "0988112233"),
            "Leaks water from the bottom", today.AddDays(-2), today.AddDays(3),
            drumCapacityKg: 7, isFrontLoading: false);

        foreach (IAppliance appliance in new[] { lateFridge, washer, airConditioner, waitingFridge, topLoader })
        {
            center.Register(appliance);
        }

        ConsoleView.PrintAppliances(center.Appliances);

        ConsoleView.PrintTitle("2. Four repairs start and the technicians record their work");

        // A second subscriber on one appliance: the center already listens to the same event.
        washer.StatusChanged += TraceStatusChange;

        lateFridge.StartRepair();
        lateFridge.AddOperation(new RepairOperation("Replace thermostat sensor", 1.5m, 85_000m, today.AddDays(-8)));

        washer.StartRepair();
        washer.AddOperation(new RepairOperation("Replace drive belt", 1m, 45_000m, today.AddDays(-4)));
        washer.AddOperation(new RepairOperation("Replace drum bearings", 2.5m, 120_000m, today.AddDays(-3)));

        airConditioner.StartRepair();
        airConditioner.AddOperation(new RepairOperation("Clean coils and unblock drain line", 2m, 15_000m, today.AddDays(-2)));

        topLoader.StartRepair();
        topLoader.AddOperation(new RepairOperation("Replace drain pump", 1m, 60_000m, today.AddDays(-1)));
        Console.WriteLine($"  Appliances under repair now: {RepairCenter.UnderRepairCount}");

        ConsoleView.PrintTitle("3. Two repairs are completed: the center raises RepairCompleted");
        washer.CompleteRepair();
        airConditioner.CompleteRepair();

        ConsoleView.PrintTitle("4. One owner collects her appliance");
        washer.DeliverToOwner();
        Console.WriteLine($"  Amount collected: {ConsoleView.FormatMoney(washer.CalculateRepairCost())}");

        ConsoleView.PrintTitle("5. Daily check for late repairs: the center raises DeliveryOverdue");
        int overdueCount = center.CheckOverdueDeliveries(today).Count;
        Console.WriteLine($"  {overdueCount} appliance(s) overdue.");

        ConsoleView.PrintTitle("6. Follow-up: appliances still under maintenance");
        ConsoleView.PrintAppliances(center.GetAppliancesUnderMaintenance());

        ConsoleView.PrintTitle("7. Follow-up: repaired appliances");
        ConsoleView.PrintAppliances(center.GetRepairedAppliances());

        ConsoleView.PrintTitle("8. Repair ticket of one appliance");
        ConsoleView.PrintDetails(washer);

        ConsoleView.PrintTitle("9. Cost report");
        ConsoleView.PrintCostReport(center);

        ConsoleView.PrintTitle("10. Rules the system enforces");
        ShowValidation();
        ShowRefusal(
            "Register a refrigerator whose serial number is \"rf 12\"",
            () => center.Register(new Refrigerator(
                "rf 12", "Samsung", "RT38", new Customer("Maya Saleh", "0944000111"),
                "Not cooling", today, today, capacityLiters: 300, isNoFrost: false)));
        ShowRefusal($"Register {FirstSerialNumber} a second time", () => center.Register(lateFridge));
        ShowRefusal($"Deliver {waitingFridge.SerialNumber} before it is repaired", waitingFridge.DeliverToOwner);
        ShowRefusal(
            $"Promise {waitingFridge.SerialNumber} for a day before it was received",
            () => waitingFridge.ExpectedDeliveryDate = waitingFridge.ReceivedDate.AddDays(-1));

        ConsoleView.PrintDashboard(center);
    }

    private static void TraceStatusChange(object? sender, StatusChangedEventArgs e)
    {
        Console.WriteLine($"  [trace] {e.Appliance.SerialNumber}: {e.PreviousStatus} -> {e.NewStatus}");
    }

    private static void ShowValidation()
    {
        Console.WriteLine($"  IsValidSerialNumber(\"RF-2024-0001\") = {InputValidator.IsValidSerialNumber("RF-2024-0001")}");
        Console.WriteLine($"  IsValidSerialNumber(\"rf 12\")        = {InputValidator.IsValidSerialNumber("rf 12")}");
        Console.WriteLine($"  IsValidPhoneNumber(\"0944123456\")    = {InputValidator.IsValidPhoneNumber("0944123456")}");
        Console.WriteLine($"  TryParseDate(\"2026-02-30\")          = {InputValidator.TryParseDate("2026-02-30", out _)}");
    }

    /// <summary>Tries something the model must not allow and prints the reason it gives.</summary>
    private static void ShowRefusal(string attempt, Action action)
    {
        try
        {
            action();
            Console.WriteLine($"  {attempt}: accepted");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            Console.WriteLine($"  {attempt}");
            Console.WriteLine($"      refused: {ex.Message}");
        }
    }
}
