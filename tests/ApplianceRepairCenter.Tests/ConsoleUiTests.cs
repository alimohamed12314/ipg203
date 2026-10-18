using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using ApplianceRepairCenter.Enums;
using ApplianceRepairCenter.Interfaces;
using ApplianceRepairCenter.Services;
using ApplianceRepairCenter.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ApplianceRepairCenter.Tests;

/// <summary>
/// Drives the console user interface as a user would: typed lines go in, printed text comes out.
/// </summary>
[TestClass]
public class ConsoleUiTests
{
    private const string Exit = "0";

    private TextReader _originalInput = TextReader.Null;
    private TextWriter _originalOutput = TextWriter.Null;
    private CultureInfo _originalCulture = CultureInfo.InvariantCulture;

    [TestInitialize]
    public void RememberTheConsole()
    {
        _originalInput = Console.In;
        _originalOutput = Console.Out;
        _originalCulture = CultureInfo.CurrentCulture;
    }

    [TestCleanup]
    public void RestoreTheConsole()
    {
        Console.SetIn(_originalInput);
        Console.SetOut(_originalOutput);
        CultureInfo.CurrentCulture = _originalCulture;
    }

    [TestMethod]
    public void AFullSession_TakesOneApplianceFromIntakeToDelivery()
    {
        RepairCenter center = CreateCenter();

        string output = RunMenu(
            center,
            "1", "1", "rf-9000-01", "Samsung", "RT38", "Ahmad Khalil", "12345", "0944123456", "Not cooling", string.Empty, string.Empty, "380", "y",
            "2", "1",
            "3", "1", "Replace thermostat sensor", "1.5", "85000",
            "4", "1",
            "5", "1",
            "7",
            "10", "1",
            "11",
            "12",
            Exit);

        StringAssert.Contains(output, "Not accepted. Use 09 followed by eight digits.");
        StringAssert.Contains(output, "Registered: Refrigerator Samsung RT38 (380 L, no-frost), status Received.");
        StringAssert.Contains(output, "RF-9000-01 is now UnderRepair. Repair cost: 63,000 SYP.");
        StringAssert.Contains(output, "Recorded. Repair cost so far: 256,000 SYP.");
        StringAssert.Contains(
            output,
            "[SMS to Ahmad Khalil (0944123456)] Test Center: your Refrigerator Samsung RT38 (380 L, no-frost) " +
            "is repaired and ready for collection. Amount due: 256,000 SYP.");
        StringAssert.Contains(output, "RF-9000-01 is now Repaired. Repair cost: 256,000 SYP.");
        StringAssert.Contains(output, "RF-9000-01 is now Delivered. Repair cost: 256,000 SYP.");
        StringAssert.Contains(output, "Reported fault    : Not cooling");
        StringAssert.Contains(output, $"Received          : {ConsoleView.FormatDate(DateTime.Today)}");
        StringAssert.Contains(output, $"Expected delivery : {ConsoleView.FormatDate(DateTime.Today.AddDays(5))}");
        StringAssert.Contains(output, "Replace thermostat sensor");
        StringAssert.Matches(output, new Regex(@"1\.5 h +parts +85,000 SYP"));
        StringAssert.Matches(output, new Regex(@"All repairs +1 appliance\(s\) +256,000 SYP"));
        StringAssert.Contains(output, "0 appliance(s) overdue.");

        IAppliance appliance = center.FindBySerialNumber("RF-9000-01")!;
        Assert.AreEqual(ApplianceStatus.Delivered, appliance.Status);
        Assert.AreEqual(256_000m, appliance.CalculateRepairCost());
    }

    [TestMethod]
    public void Registration_CoversTheThreeApplianceTypes()
    {
        RepairCenter center = CreateCenter();

        string output = RunMenu(
            center,
            "1", "2", "WM-9000-01", "LG", "F4V5", "Rana Haddad", "0933555120", "No spin", string.Empty, string.Empty, "9", "y",
            "1", "3", "AC-9000-01", "Gree", "Pular", "Samer Nassar", "0991002003", "Weak cooling", string.Empty, string.Empty, "18000", "n",
            Exit);

        StringAssert.Contains(output, "Registered: Washing Machine LG F4V5 (9 kg, front loading), status Received.");
        StringAssert.Contains(output, "Registered: Air Conditioner Gree Pular (18000 BTU, window), status Received.");
        Assert.AreEqual(2, center.Appliances.Count);
    }

    [TestMethod]
    public void Registration_AsksAgainUntilEveryEntryIsValid()
    {
        RepairCenter center = CreateCenter();
        center.Register(TestData.Refrigerator("RF-TEST-0001"));
        string tomorrow = ConsoleView.FormatDate(DateTime.Today.AddDays(1));
        string yesterday = ConsoleView.FormatDate(DateTime.Today.AddDays(-1));

        string output = RunMenu(
            center,
            "x", "99", "1",
            "7", "1",
            "rf 1", "RF-TEST-0001", "RF-TEST-0002",
            string.Empty, "Samsung",
            "RT38",
            "A1", "Ahmad Khalil",
            "0944", "0944123456",
            "  ", "Not cooling",
            "2026-02-30", tomorrow, string.Empty,
            yesterday, string.Empty,
            "abc", "0", "380",
            "maybe", "n",
            Exit);

        StringAssert.Contains(output, "Not accepted. Enter a whole number from 0 to 13.");
        StringAssert.Contains(output, "Not accepted. Enter a whole number from 1 to 3.");
        StringAssert.Contains(output, "not registered before.");
        StringAssert.Contains(output, "Not accepted. The brand is required.");
        StringAssert.Contains(output, "Not accepted. Use 3 to 50 letters.");
        StringAssert.Contains(output, "Not accepted. Describe the fault.");
        StringAssert.Contains(output, "Not accepted. Enter a real date that is not in the future.");
        StringAssert.Contains(output, "Not accepted. Enter a real date, not before the day the appliance was received.");
        StringAssert.Contains(output, "Not accepted. Enter a whole number from 40 to 1000.");
        StringAssert.Contains(output, "Not accepted. Answer y or n.");
        StringAssert.Contains(output, "Registered: Refrigerator Samsung RT38 (380 L, direct cool), status Received.");
        Assert.AreEqual(2, center.Appliances.Count);
    }

    [TestMethod]
    public void TheMenu_ReportsWhatTheModelRefusesAndGoesOn()
    {
        RepairCenter center = CreateCenter();
        IAppliance refrigerator = TestData.Refrigerator("RF-TEST-0001");
        center.Register(refrigerator);
        refrigerator.StartRepair();

        string output = RunMenu(center, "4", "1", "7", Exit);

        StringAssert.Contains(output, "Not done: Appliance RF-TEST-0001 has no recorded work");
        Assert.AreEqual(ApplianceStatus.UnderRepair, refrigerator.Status);
    }

    [TestMethod]
    public void AnAction_DoesNothingWhenThereIsNoApplianceForItOrTheUserCancels()
    {
        RepairCenter center = CreateCenter();
        IAppliance refrigerator = TestData.Refrigerator("RF-TEST-0001");
        center.Register(refrigerator);

        string output = RunMenu(center, "5", "9", "2", "0", "3", "0.5", Exit);

        StringAssert.Contains(output, "(none)");
        Assert.AreEqual(ApplianceStatus.Received, refrigerator.Status);
    }

    [TestMethod]
    public void PostponingADelivery_AcceptsOnlyADateFromIntakeOn()
    {
        RepairCenter center = CreateCenter();
        IAppliance refrigerator = TestData.Refrigerator("RF-TEST-0001", receivedDaysAgo: 3, dueInDays: 2);
        center.Register(refrigerator);
        string beforeIntake = ConsoleView.FormatDate(DateTime.Today.AddDays(-10));
        string newDate = ConsoleView.FormatDate(DateTime.Today.AddDays(9));

        string output = RunMenu(center, "6", "1", beforeIntake, newDate, Exit);

        StringAssert.Contains(output, "Not accepted. Enter a real date, not before the day the appliance was received.");
        StringAssert.Contains(output, $"RF-TEST-0001 is now promised for {newDate}.");
        Assert.AreEqual(DateTime.Today.AddDays(9), refrigerator.ExpectedDeliveryDate);
    }

    [TestMethod]
    public void TheDemoScenario_ShowsEveryFeatureAndLoadsOnlyOnce()
    {
        RepairCenter center = CreateCenter();

        string output = RunMenu(center, "13", "13", "12", "8", "9", Exit);

        StringAssert.Contains(output, "[trace] WM-2023-0417: Received -> UnderRepair");
        StringAssert.Contains(output, "is repaired and ready for collection. Amount due: 482,500 SYP.");
        StringAssert.Contains(output, "is repaired and ready for collection. Amount due: 270,000 SYP.");
        StringAssert.Contains(output, "is 2 day(s) late.");
        StringAssert.Matches(output, new Regex(@"All repairs +5 appliance\(s\) +1,202,500 SYP"));
        StringAssert.Contains(output, "refused: Appliance RF-2022-0730 is Received; it must be Repaired to become Delivered.");
        StringAssert.Contains(output, "The demo data is already loaded.");
        StringAssert.Contains(output, "1 appliance(s) overdue.");

        Assert.AreEqual(5, center.Appliances.Count);
        Assert.AreEqual(3, center.GetAppliancesUnderMaintenance().Count);
        Assert.AreEqual(2, center.GetRepairedAppliances().Count);
        Assert.AreEqual(1_202_500m, center.CalculateTotalRepairCost());
    }

    [DataTestMethod]
    [DataRow("ar-SA")]
    [DataRow("de-DE")]
    [DataRow("fr-FR")]
    public void Output_IsTheSameWhateverTheLanguageOfTheComputer(string cultureName)
    {
        string expected = RunDemoScenario(CultureInfo.InvariantCulture);

        string actual = RunDemoScenario(new CultureInfo(cultureName));

        Assert.AreEqual(expected, actual);
        StringAssert.Contains(actual, "1,202,500 SYP");
        StringAssert.Contains(actual, "2.5 h");
        // The washing machine of the scenario is promised for tomorrow.
        StringAssert.Contains(actual, DateTime.Today.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void Numbers_AreReadWithADecimalPointWhateverTheLanguageOfTheComputer()
    {
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        RepairCenter center = CreateCenter();
        IAppliance washer = TestData.WashingMachine("WM-TEST-0001");
        center.Register(washer);
        washer.StartRepair();

        string output = RunMenu(center, "3", "1", "Replace belt", "1,5", "1.5", "45000", Exit);

        StringAssert.Contains(output, "Not accepted. Enter a number from 0.25 to 100, such as 1.5.");
        Assert.AreEqual(1.5m, washer.Operations[0].LaborHours);
        Assert.AreEqual(45_000m, washer.Operations[0].PartsCost);
    }

    [TestMethod]
    public void TheMenu_StopsWhenTheInputEnds()
    {
        RepairCenter center = CreateCenter();

        Assert.ThrowsException<EndOfStreamException>(() => RunMenu(center, "1", "1"));
        Assert.AreEqual(0, center.Appliances.Count);
    }

    /// <summary>A center wired to the notifications exactly as Program.Main does it.</summary>
    private static RepairCenter CreateCenter()
    {
        RepairCenter center = new RepairCenter("Test Center");
        center.RepairCompleted += CustomerNotifier.NotifyRepairCompleted;
        center.DeliveryOverdue += CustomerNotifier.NotifyDeliveryOverdue;
        return center;
    }

    private static string RunMenu(RepairCenter center, params string[] typedLines)
    {
        Console.SetIn(new StringReader(string.Join(Environment.NewLine, typedLines)));
        StringWriter output = new StringWriter();
        Console.SetOut(output);

        new ConsoleMenu(center).Run();

        return output.ToString();
    }

    /// <summary>
    /// Runs the scenario under a culture and returns what it printed, without the two places that
    /// show the static counts: those are shared with the other tests.
    /// </summary>
    private static string RunDemoScenario(CultureInfo culture)
    {
        CultureInfo.CurrentCulture = culture;
        StringWriter output = new StringWriter();
        Console.SetOut(output);

        DemoScenario.Run(CreateCenter());

        string text = output.ToString();
        text = text.Substring(0, text.IndexOf("=====", StringComparison.Ordinal));
        return Regex.Replace(text, "Appliances under repair now: [0-9]+", string.Empty);
    }
}
