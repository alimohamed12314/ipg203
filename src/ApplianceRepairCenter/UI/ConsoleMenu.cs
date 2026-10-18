using System;
using System.Collections.Generic;
using System.Linq;
using ApplianceRepairCenter.Enums;
using ApplianceRepairCenter.Interfaces;
using ApplianceRepairCenter.Models;
using ApplianceRepairCenter.Services;
using ApplianceRepairCenter.Validation;

namespace ApplianceRepairCenter.UI;

/// <summary>
/// The interactive menu: each entry pairs a label with the action that carries it out.
/// </summary>
internal sealed class ConsoleMenu
{
    private const int DefaultRepairDays = 5;

    private readonly RepairCenter _center;
    private readonly (string Label, Action Run)[] _items;

    public ConsoleMenu(RepairCenter center)
    {
        _center = center;
        _items = new (string Label, Action Run)[]
        {
            ("Register a new appliance", RegisterAppliance),
            ("Start a repair", StartRepair),
            ("Record a repair operation", RecordOperation),
            ("Complete a repair", CompleteRepair),
            ("Deliver an appliance to its owner", DeliverAppliance),
            ("Postpone a delivery date", PostponeDelivery),
            ("List all appliances", () => ConsoleView.PrintAppliances(_center.Appliances)),
            ("List appliances still under maintenance", () => ConsoleView.PrintAppliances(_center.GetAppliancesUnderMaintenance())),
            ("List repaired appliances", () => ConsoleView.PrintAppliances(_center.GetRepairedAppliances())),
            ("Show the details of an appliance", ShowDetails),
            ("Show the cost report", () => ConsoleView.PrintCostReport(_center)),
            ("Check for overdue deliveries", CheckOverdueDeliveries),
            ("Run the demo scenario", () => DemoScenario.Run(_center)),
        };
    }

    /// <summary>Shows the menu and carries out the chosen entries until the user exits.</summary>
    public void Run()
    {
        while (true)
        {
            ConsoleView.PrintDashboard(_center);
            for (int index = 0; index < _items.Length; index++)
            {
                Console.WriteLine($"  {index + 1,2}. {_items[index].Label}");
            }

            Console.WriteLine("   0. Exit");

            int choice = ConsoleInput.ReadInt("Choose", 0, _items.Length);
            if (choice == 0)
            {
                return;
            }

            (string label, Action run) = _items[choice - 1];
            ConsoleView.PrintTitle(label);
            try
            {
                run();
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                // The model refused the request: say why and go back to the menu.
                Console.WriteLine($"  Not done: {ex.Message}");
            }
        }
    }

    private void RegisterAppliance()
    {
        Console.WriteLine("  1. Refrigerator   2. Washing Machine   3. Air Conditioner");
        int type = ConsoleInput.ReadInt("Appliance type", 1, 3);

        string serialNumber = ConsoleInput.ReadText(
            "Serial number",
            serial => InputValidator.IsValidSerialNumber(serial) && _center.FindBySerialNumber(serial) is null,
            "Use 6 to 20 letters, digits and hyphens (for example RF-2024-0001) not registered before.",
            upperCase: true);
        string brand = ConsoleInput.ReadText("Brand", IsNotBlank, "The brand is required.");
        string model = ConsoleInput.ReadText("Model", IsNotBlank, "The model is required.");

        string ownerName = ConsoleInput.ReadText(
            "Owner name", InputValidator.IsValidPersonName, "Use 3 to 50 letters.");
        string ownerPhone = ConsoleInput.ReadText(
            "Owner mobile number", InputValidator.IsValidPhoneNumber, "Use 09 followed by eight digits.");
        Customer owner = new Customer(ownerName, ownerPhone);

        string fault = ConsoleInput.ReadText("Reported fault", IsNotBlank, "Describe the fault.");
        DateTime received = ConsoleInput.ReadDate(
            "Received on", DateTime.Today, InputValidator.IsNotInFuture, "Enter a real date that is not in the future.");
        DateTime expected = ConsoleInput.ReadDate(
            "Promised for",
            received.AddDays(DefaultRepairDays),
            date => InputValidator.IsValidDeliveryDate(received, date),
            "Enter a real date, not before the day the appliance was received.");

        IAppliance appliance = type switch
        {
            1 => new Refrigerator(
                serialNumber, brand, model, owner, fault, received, expected,
                ConsoleInput.ReadInt("Capacity in liters", 40, 1000),
                ConsoleInput.ReadYesNo("Is it a no-frost model?")),
            2 => new WashingMachine(
                serialNumber, brand, model, owner, fault, received, expected,
                ConsoleInput.ReadInt("Drum capacity in kg", 3, 20),
                ConsoleInput.ReadYesNo("Is it front loading?")),
            _ => new AirConditioner(
                serialNumber, brand, model, owner, fault, received, expected,
                ConsoleInput.ReadInt("Cooling capacity in BTU", 5000, 60000),
                ConsoleInput.ReadYesNo("Is it a split unit?")),
        };

        _center.Register(appliance);
        Console.WriteLine($"  Registered: {appliance}, status {appliance.Status}.");
    }

    private void StartRepair()
    {
        AdvanceWorkflow(ApplianceStatus.Received, appliance => appliance.StartRepair());
    }

    private void RecordOperation()
    {
        IAppliance? appliance = SelectAppliance(candidate => candidate.Status == ApplianceStatus.UnderRepair);
        if (appliance is null)
        {
            return;
        }

        string description = ConsoleInput.ReadText("Work done", IsNotBlank, "Describe the work.");
        decimal laborHours = ConsoleInput.ReadDecimal("Labor hours", 0.25m, 100m);
        decimal partsCost = ConsoleInput.ReadDecimal("Spare parts cost", 0m, 100_000_000m);

        appliance.AddOperation(new RepairOperation(description, laborHours, partsCost, DateTime.Today));
        Console.WriteLine($"  Recorded. Repair cost so far: {ConsoleView.FormatMoney(appliance.CalculateRepairCost())}.");
    }

    private void CompleteRepair()
    {
        // Completing the repair makes the center raise RepairCompleted: the owner is notified.
        AdvanceWorkflow(ApplianceStatus.UnderRepair, appliance => appliance.CompleteRepair());
    }

    private void DeliverAppliance()
    {
        AdvanceWorkflow(ApplianceStatus.Repaired, appliance => appliance.DeliverToOwner());
    }

    private void PostponeDelivery()
    {
        IAppliance? appliance = SelectAppliance(candidate => !candidate.IsRepaired);
        if (appliance is null)
        {
            return;
        }

        appliance.ExpectedDeliveryDate = ConsoleInput.ReadDate(
            "New delivery date",
            appliance.ExpectedDeliveryDate,
            date => InputValidator.IsValidDeliveryDate(appliance.ReceivedDate, date),
            "Enter a real date, not before the day the appliance was received.");
        Console.WriteLine($"  {appliance.SerialNumber} is now promised for {ConsoleView.FormatDate(appliance.ExpectedDeliveryDate)}.");
    }

    private void ShowDetails()
    {
        IAppliance? appliance = SelectAppliance(_ => true);
        if (appliance is not null)
        {
            ConsoleView.PrintDetails(appliance);
        }
    }

    private void CheckOverdueDeliveries()
    {
        // The center raises DeliveryOverdue for each late appliance: its owner is notified.
        int overdueCount = _center.CheckOverdueDeliveries(DateTime.Today).Count;
        Console.WriteLine($"  {overdueCount} appliance(s) overdue.");
    }

    /// <summary>
    /// The three workflow steps differ only in the status they start from and the method they
    /// call, so they share this code and pass the step as a delegate.
    /// </summary>
    private void AdvanceWorkflow(ApplianceStatus currentStatus, Action<IAppliance> step)
    {
        IAppliance? appliance = SelectAppliance(candidate => candidate.Status == currentStatus);
        if (appliance is null)
        {
            return;
        }

        step(appliance);
        Console.WriteLine(
            $"  {appliance.SerialNumber} is now {appliance.Status}. Repair cost: {ConsoleView.FormatMoney(appliance.CalculateRepairCost())}.");
    }

    /// <summary>
    /// Lists the appliances an action applies to and lets the user pick one;
    /// returns null when there is none or the user cancels.
    /// </summary>
    private IAppliance? SelectAppliance(Func<IAppliance, bool> isCandidate)
    {
        List<IAppliance> candidates = _center.Appliances.Where(isCandidate).ToList();
        ConsoleView.PrintAppliances(candidates);
        if (candidates.Count == 0)
        {
            return null;
        }

        int choice = ConsoleInput.ReadInt("Appliance number (0 to cancel)", 0, candidates.Count);
        return choice == 0 ? null : candidates[choice - 1];
    }

    private static bool IsNotBlank(string text)
    {
        return !string.IsNullOrWhiteSpace(text);
    }
}
