using System;
using ApplianceRepairCenter.Validation;

namespace ApplianceRepairCenter.Models;

/// <summary>
/// The owner of an appliance: the person the center notifies and hands the appliance back to.
/// A customer cannot be changed once created.
/// </summary>
public sealed class Customer
{
    public Customer(string name, string phoneNumber)
    {
        if (!InputValidator.IsValidPersonName(name))
        {
            throw new ArgumentException("A name is 3 to 50 letters; spaces and hyphens may separate its words.", nameof(name));
        }

        if (!InputValidator.IsValidPhoneNumber(phoneNumber))
        {
            throw new ArgumentException("A phone number is 09 followed by eight digits.", nameof(phoneNumber));
        }

        Name = name;
        PhoneNumber = phoneNumber;
    }

    public string Name { get; }

    public string PhoneNumber { get; }

    public override string ToString()
    {
        return $"{Name} ({PhoneNumber})";
    }
}
