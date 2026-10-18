using System;
using System.IO;
using System.Linq;
using ApplianceRepairCenter.Services;
using ApplianceRepairCenter.UI;

namespace ApplianceRepairCenter;

/// <summary>
/// Entry point: builds the repair center, connects the notifications to its events and starts
/// the user interface.
/// </summary>
internal static class Program
{
    /// <summary>Run with this argument to play the demo scenario and exit, without the menu.</summary>
    private const string DemoArgument = "--demo";

    private static void Main(string[] args)
    {
        RepairCenter center = new RepairCenter("Damascus Home Appliance Repair Center");

        // The notifier's methods match the delegates of these events, so they can subscribe.
        center.RepairCompleted += CustomerNotifier.NotifyRepairCompleted;
        center.DeliveryOverdue += CustomerNotifier.NotifyDeliveryOverdue;

        if (args.Contains(DemoArgument))
        {
            DemoScenario.Run(center);
            return;
        }

        try
        {
            new ConsoleMenu(center).Run();
        }
        catch (EndOfStreamException)
        {
            // The input was closed, for example when it is piped from a file: leave quietly.
        }
    }
}
