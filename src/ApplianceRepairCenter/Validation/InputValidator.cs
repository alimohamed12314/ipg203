using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ApplianceRepairCenter.Validation;

/// <summary>
/// The rules that data must pass before the system accepts it.
/// The class is static because a rule needs no object: the user interface calls it to ask again
/// after a wrong entry, and the model calls it to refuse an invalid value.
/// </summary>
public static class InputValidator
{
    private const string IsoDateFormat = "yyyy-MM-dd";
    private const int MinSerialNumberLength = 6;
    private const int MaxSerialNumberLength = 20;
    private const int MinNameLength = 3;
    private const int MaxNameLength = 50;

    // Groups of capital letters and digits joined by single hyphens, such as RF-2024-0001.
    private static readonly Regex s_serialNumberPattern =
        new Regex(@"\A[A-Z0-9]+(-[A-Z0-9]+)*\z", RegexOptions.CultureInvariant);

    // A Syrian mobile number: 09 followed by eight digits.
    private static readonly Regex s_phoneNumberPattern =
        new Regex(@"\A09[0-9]{8}\z", RegexOptions.CultureInvariant);

    // Words made of letters, separated by one space, hyphen or apostrophe.
    private static readonly Regex s_personNamePattern =
        new Regex(@"\A\p{L}+([ '-]\p{L}+)*\z", RegexOptions.CultureInvariant);

    /// <summary>The single date format the system reads and prints.</summary>
    public static string DateFormat
    {
        get { return IsoDateFormat; }
    }

    /// <summary>Checks that a serial number has the length and the characters the center accepts.</summary>
    public static bool IsValidSerialNumber(string? serialNumber)
    {
        return serialNumber is not null
            && serialNumber.Length >= MinSerialNumberLength
            && serialNumber.Length <= MaxSerialNumberLength
            && s_serialNumberPattern.IsMatch(serialNumber);
    }

    /// <summary>Checks that a phone number is a mobile number the center can send a message to.</summary>
    public static bool IsValidPhoneNumber(string? phoneNumber)
    {
        return phoneNumber is not null && s_phoneNumberPattern.IsMatch(phoneNumber);
    }

    /// <summary>Checks that a customer name is made of letters only and has a sensible length.</summary>
    public static bool IsValidPersonName(string? name)
    {
        return name is not null
            && name.Length >= MinNameLength
            && name.Length <= MaxNameLength
            && s_personNamePattern.IsMatch(name);
    }

    /// <summary>Checks that a date is today or earlier; nothing can be received or repaired tomorrow.</summary>
    public static bool IsNotInFuture(DateTime date)
    {
        return date.Date <= DateTime.Today;
    }

    /// <summary>Checks that an appliance is not promised back before the day it was received.</summary>
    public static bool IsValidDeliveryDate(DateTime receivedDate, DateTime expectedDeliveryDate)
    {
        return expectedDeliveryDate.Date >= receivedDate.Date;
    }

    /// <summary>Reads a date typed as <see cref="DateFormat"/>; refuses text that is not a real calendar date.</summary>
    public static bool TryParseDate(string? text, out DateTime date)
    {
        return DateTime.TryParseExact(
            text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}
