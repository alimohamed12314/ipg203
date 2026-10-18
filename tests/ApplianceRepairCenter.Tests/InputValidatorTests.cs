using System;
using ApplianceRepairCenter.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ApplianceRepairCenter.Tests;

[TestClass]
public class InputValidatorTests
{
    [DataTestMethod]
    [DataRow("RF-2024-0001")]
    [DataRow("WM58213A")]
    [DataRow("AC-771")]
    [DataRow("123456")]
    [DataRow("A1-B2-C3-D4-E5-F6-G7")]
    public void IsValidSerialNumber_AcceptsCapitalLettersDigitsAndSingleHyphens(string serialNumber)
    {
        Assert.IsTrue(InputValidator.IsValidSerialNumber(serialNumber));
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("RF-12")]
    [DataRow("A1-B2-C3-D4-E5-F6-G7H")]
    [DataRow("rf-2024-0001")]
    [DataRow("RF 2024 0001")]
    [DataRow("RF--2024")]
    [DataRow("-RF2024")]
    [DataRow("RF2024-")]
    [DataRow("RF_2024_01")]
    [DataRow("RF-2024-0001\n")]
    [DataRow("RF-\u0662\u0660\u0662\u0664-0001")]
    public void IsValidSerialNumber_RejectsEverythingElse(string? serialNumber)
    {
        Assert.IsFalse(InputValidator.IsValidSerialNumber(serialNumber));
    }

    [DataTestMethod]
    [DataRow("0944123456", true)]
    [DataRow("0933555120", true)]
    [DataRow("094412345", false)]
    [DataRow("09441234567", false)]
    [DataRow("0844123456", false)]
    [DataRow("+963944123456", false)]
    [DataRow("09441 3456", false)]
    [DataRow("09\u0664\u0664123456", false)]
    [DataRow("", false)]
    [DataRow(null, false)]
    public void IsValidPhoneNumber_AcceptsOnly09FollowedByEightDigits(string? phoneNumber, bool expected)
    {
        Assert.AreEqual(expected, InputValidator.IsValidPhoneNumber(phoneNumber));
    }

    [DataTestMethod]
    [DataRow("Ahmad Khalil", true)]
    [DataRow("Abdul-Rahman Al-Khatib", true)]
    [DataRow("\u0623\u062D\u0645\u062F \u062E\u0644\u064A\u0644", true)]
    [DataRow("Ali", true)]
    [DataRow("Al", false)]
    [DataRow("Ahmad  Khalil", false)]
    [DataRow(" Ahmad", false)]
    [DataRow("Ahmad2", false)]
    [DataRow("Ahmad!", false)]
    [DataRow("", false)]
    [DataRow(null, false)]
    public void IsValidPersonName_AcceptsLettersInWords(string? name, bool expected)
    {
        Assert.AreEqual(expected, InputValidator.IsValidPersonName(name));
    }

    [TestMethod]
    public void IsValidPersonName_RejectsNamesLongerThanFiftyLetters()
    {
        Assert.IsTrue(InputValidator.IsValidPersonName(new string('a', 50)));
        Assert.IsFalse(InputValidator.IsValidPersonName(new string('a', 51)));
    }

    [TestMethod]
    public void IsNotInFuture_AcceptsTodayAndEarlier()
    {
        Assert.IsTrue(InputValidator.IsNotInFuture(DateTime.Today));
        Assert.IsTrue(InputValidator.IsNotInFuture(DateTime.Today.AddDays(-400)));
        Assert.IsTrue(InputValidator.IsNotInFuture(DateTime.Today.AddHours(23)));
        Assert.IsFalse(InputValidator.IsNotInFuture(DateTime.Today.AddDays(1)));
    }

    [TestMethod]
    public void IsValidDeliveryDate_AcceptsTheReceivedDayAndLater()
    {
        DateTime received = new DateTime(2026, 10, 5);

        Assert.IsTrue(InputValidator.IsValidDeliveryDate(received, received));
        Assert.IsTrue(InputValidator.IsValidDeliveryDate(received, received.AddDays(7)));
        Assert.IsFalse(InputValidator.IsValidDeliveryDate(received, received.AddDays(-1)));
    }

    [TestMethod]
    public void TryParseDate_ReadsTheSystemDateFormat()
    {
        Assert.IsTrue(InputValidator.TryParseDate("2026-10-05", out DateTime date));
        Assert.AreEqual(new DateTime(2026, 10, 5), date);
    }

    [DataTestMethod]
    [DataRow("2026-02-30")]
    [DataRow("2026-13-01")]
    [DataRow("05-10-2026")]
    [DataRow("2026/10/05")]
    [DataRow("2026-10-5")]
    [DataRow("today")]
    [DataRow("")]
    [DataRow(null)]
    public void TryParseDate_RejectsTextThatIsNotARealDateInThatFormat(string? text)
    {
        Assert.IsFalse(InputValidator.TryParseDate(text, out _));
    }
}
