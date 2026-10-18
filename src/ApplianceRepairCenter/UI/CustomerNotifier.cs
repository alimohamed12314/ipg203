using System;
using ApplianceRepairCenter.Events;
using ApplianceRepairCenter.Interfaces;
using ApplianceRepairCenter.Services;

namespace ApplianceRepairCenter.UI;

/// <summary>
/// Tells customers what happens to their appliances. Its methods have the signatures of the
/// center's events, so they are subscribed to them; a real system would send text messages
/// where this one prints them.
/// </summary>
internal static class CustomerNotifier
{
    /// <summary>Handles <see cref="RepairCenter.RepairCompleted"/>: the appliance can be collected.</summary>
    public static void NotifyRepairCompleted(object? sender, StatusChangedEventArgs e)
    {
        string amountDue = ConsoleView.FormatMoney(e.Appliance.CalculateRepairCost());
        Send(sender, e.Appliance, $"your {e.Appliance} is repaired and ready for collection. Amount due: {amountDue}.");
    }

    /// <summary>Handles <see cref="RepairCenter.DeliveryOverdue"/>: the repair is taking longer than promised.</summary>
    public static void NotifyDeliveryOverdue(object? sender, DeliveryOverdueEventArgs e)
    {
        Send(sender, e.Appliance, $"we are sorry: your {e.Appliance} is {e.DaysLate} day(s) late. We are still working on it.");
    }

    private static void Send(object? sender, IAppliance appliance, string message)
    {
        string centerName = sender is RepairCenter center ? center.Name : "The repair center";
        Console.WriteLine($"  [SMS to {appliance.Owner}] {centerName}: {message}");
    }
}
