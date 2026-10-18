using System;
using System.Globalization;
using System.IO;
using ApplianceRepairCenter.Validation;

namespace ApplianceRepairCenter.UI;

/// <summary>
/// Reads values typed by the user and keeps asking until the entry is acceptable.
/// </summary>
internal static class ConsoleInput
{
    /// <summary>Turns typed text into a value of type T; returns false when the text is not one.</summary>
    private delegate bool Parser<T>(string text, out T value);

    /// <summary>Reads a line of text that satisfies the given rule.</summary>
    public static string ReadText(string prompt, Func<string, bool> isValid, string hint, bool upperCase = false)
    {
        return Read<string>(prompt, ParseText, isValid, hint);

        bool ParseText(string text, out string value)
        {
            value = upperCase ? text.ToUpperInvariant() : text;
            return true;
        }
    }

    /// <summary>Reads a whole number between two limits.</summary>
    public static int ReadInt(string prompt, int min, int max)
    {
        return Read<int>(prompt, ParseInt, number => number >= min && number <= max, $"Enter a whole number from {min} to {max}.");

        static bool ParseInt(string text, out int value)
        {
            return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
        }
    }

    /// <summary>Reads a number that may have decimals, such as 1.5, between two limits.</summary>
    public static decimal ReadDecimal(string prompt, decimal min, decimal max)
    {
        string hint = FormattableString.Invariant($"Enter a number from {min} to {max}, such as 1.5.");
        return Read<decimal>(prompt, ParseDecimal, number => number >= min && number <= max, hint);

        static bool ParseDecimal(string text, out decimal value)
        {
            return decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
        }
    }

    /// <summary>Reads a date; pressing Enter accepts the suggested one.</summary>
    public static DateTime ReadDate(string prompt, DateTime suggested, Func<DateTime, bool> isValid, string hint)
    {
        string suggestedText = ConsoleView.FormatDate(suggested);
        return Read<DateTime>($"{prompt} ({InputValidator.DateFormat}) [{suggestedText}]", InputValidator.TryParseDate, isValid, hint, suggestedText);
    }

    /// <summary>Reads a yes or no answer.</summary>
    public static bool ReadYesNo(string prompt)
    {
        return Read<bool>($"{prompt} (y/n)", ParseYesNo, _ => true, "Answer y or n.");

        static bool ParseYesNo(string text, out bool value)
        {
            value = text.Equals("y", StringComparison.OrdinalIgnoreCase) || text.Equals("yes", StringComparison.OrdinalIgnoreCase);
            return value || text.Equals("n", StringComparison.OrdinalIgnoreCase) || text.Equals("no", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// The loop all the readers share: ask, parse, check, and explain what is expected when the
    /// entry is refused. An empty entry stands for <paramref name="defaultText"/> when one is given.
    /// </summary>
    private static T Read<T>(string prompt, Parser<T> parse, Func<T, bool> isValid, string hint, string? defaultText = null)
    {
        while (true)
        {
            Console.Write($"  {prompt}: ");
            string? line = Console.ReadLine();

            if (line is null)
            {
                throw new EndOfStreamException("The input ended before the entry was complete.");
            }

            string text = line.Trim();
            if (text.Length == 0 && defaultText is not null)
            {
                text = defaultText;
            }

            if (parse(text, out T value) && isValid(value))
            {
                return value;
            }

            Console.WriteLine($"    Not accepted. {hint}");
        }
    }
}
