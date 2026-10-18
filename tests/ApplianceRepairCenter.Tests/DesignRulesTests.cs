using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ApplianceRepairCenter.Events;
using ApplianceRepairCenter.Interfaces;
using ApplianceRepairCenter.Models;
using ApplianceRepairCenter.Services;
using ApplianceRepairCenter.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ApplianceRepairCenter.Tests;

/// <summary>
/// The object-oriented structure the assignment asks for, checked on the compiled program itself:
/// if someone later makes a field public or drops an override, a test here fails.
/// </summary>
[TestClass]
public class DesignRulesTests
{
    private const BindingFlags DeclaredMembers =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly Type[] s_ownTypes = typeof(Appliance).Assembly.GetTypes()
        .Where(type => !type.Name.StartsWith('<') && !type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
        .ToArray();

    private static readonly Type[] s_applianceTypes = s_ownTypes
        .Where(type => type.IsSubclassOf(typeof(Appliance)) && !type.IsAbstract)
        .ToArray();

    [TestMethod]
    public void TheInterface_DeclaresTheOperationsCommonToAllAppliances()
    {
        Assert.IsTrue(typeof(IAppliance).IsInterface);

        string[] operations = typeof(IAppliance).GetMethods()
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .ToArray();

        CollectionAssert.IsSubsetOf(
            new[] { "StartRepair", "AddOperation", "CompleteRepair", "DeliverToOwner", "IsOverdue", "CalculateRepairCost" },
            operations);
    }

    [TestMethod]
    public void TheAbstractClass_ImplementsTheInterfaceAndLeavesTheCostToEachType()
    {
        Assert.IsTrue(typeof(Appliance).IsAbstract);
        Assert.IsTrue(typeof(IAppliance).IsAssignableFrom(typeof(Appliance)));
        Assert.IsTrue(typeof(Appliance).GetMethod(nameof(Appliance.CalculateRepairCost))!.IsAbstract);
        Assert.IsTrue(typeof(Appliance).GetProperty(nameof(Appliance.TypeName))!.GetMethod!.IsAbstract);
    }

    [TestMethod]
    public void AtLeastThreeApplianceTypes_DeriveFromTheAbstractClass()
    {
        Assert.IsTrue(s_applianceTypes.Length >= 3, $"Only {s_applianceTypes.Length} appliance types were found.");
        CollectionAssert.IsSubsetOf(
            new[] { typeof(Refrigerator), typeof(WashingMachine), typeof(AirConditioner) }, s_applianceTypes);
    }

    [TestMethod]
    public void EveryApplianceType_OverridesTheCostCalculationItself()
    {
        foreach (Type type in s_applianceTypes)
        {
            MethodInfo method = type.GetMethod(nameof(Appliance.CalculateRepairCost))!;

            Assert.AreEqual(type, method.DeclaringType, $"{type.Name} does not override CalculateRepairCost.");
            Assert.AreEqual(typeof(Appliance), method.GetBaseDefinition().DeclaringType);
            Assert.IsFalse(method.IsAbstract);
            Assert.AreEqual(type, type.GetProperty(nameof(Appliance.TypeName))!.DeclaringType);
        }
    }

    [TestMethod]
    public void EveryField_IsPrivate()
    {
        List<string> exposed = s_ownTypes
            .Where(type => !type.IsEnum)
            .SelectMany(type => type.GetFields(DeclaredMembers))
            .Where(field => !field.IsPrivate)
            .Select(field => $"{field.DeclaringType!.Name}.{field.Name}")
            .ToList();

        Assert.AreEqual(0, exposed.Count, "Fields that are not private: " + string.Join(", ", exposed));
    }

    [TestMethod]
    public void IntakeData_CannotBeChangedAfterCreation()
    {
        string[] intakeData =
        {
            nameof(Appliance.SerialNumber), nameof(Appliance.Brand), nameof(Appliance.Model),
            nameof(Appliance.Owner), nameof(Appliance.FaultDescription), nameof(Appliance.ReceivedDate),
        };

        foreach (string name in intakeData)
        {
            Assert.IsFalse(typeof(Appliance).GetProperty(name)!.CanWrite, $"{name} must be read-only.");
        }

        foreach (Type type in s_applianceTypes)
        {
            Assert.IsTrue(
                type.GetProperties(DeclaredMembers).All(property => !property.CanWrite),
                $"The properties of {type.Name} must be read-only.");
        }
    }

    [TestMethod]
    public void Status_ChangesOnlyFromInsideTheClass()
    {
        PropertyInfo status = typeof(Appliance).GetProperty(nameof(Appliance.Status))!;

        Assert.IsTrue(status.GetMethod!.IsPublic);
        Assert.IsTrue(status.SetMethod!.IsPrivate);
    }

    [TestMethod]
    public void RecordsOfThePast_AreImmutable()
    {
        Type[] immutableTypes =
        {
            typeof(Customer), typeof(RepairOperation), typeof(StatusChangedEventArgs), typeof(DeliveryOverdueEventArgs),
        };

        foreach (Type type in immutableTypes)
        {
            Assert.IsTrue(
                type.GetProperties(DeclaredMembers).All(property => !property.CanWrite),
                $"{type.Name} must not have settable properties.");
            Assert.IsTrue(
                type.GetFields(DeclaredMembers).All(field => field.IsInitOnly),
                $"{type.Name} must keep its data in read-only fields.");
        }
    }

    [TestMethod]
    public void Tariffs_AreConstants()
    {
        FieldInfo[] tariffs = s_applianceTypes.Append(typeof(Appliance))
            .SelectMany(type => type.GetFields(DeclaredMembers))
            .Where(field => field.FieldType == typeof(decimal))
            .ToArray();

        Assert.IsTrue(tariffs.Length >= 8, "The tariff constants were not found.");
        Assert.IsTrue(tariffs.All(field => field.IsStatic && field.IsInitOnly), "A tariff can be modified.");
    }

    [TestMethod]
    public void StatusChanges_AreAnnouncedThroughADelegateAndEvents()
    {
        Assert.IsTrue(typeof(StatusChangedEventHandler).IsSubclassOf(typeof(MulticastDelegate)));

        Assert.AreEqual(
            typeof(StatusChangedEventHandler), typeof(IAppliance).GetEvent(nameof(IAppliance.StatusChanged))!.EventHandlerType);
        Assert.AreEqual(
            typeof(StatusChangedEventHandler), typeof(Appliance).GetEvent(nameof(Appliance.StatusChanged))!.EventHandlerType);
        Assert.AreEqual(
            typeof(StatusChangedEventHandler), typeof(RepairCenter).GetEvent(nameof(RepairCenter.RepairCompleted))!.EventHandlerType);
        Assert.AreEqual(
            typeof(EventHandler<DeliveryOverdueEventArgs>),
            typeof(RepairCenter).GetEvent(nameof(RepairCenter.DeliveryOverdue))!.EventHandlerType);
    }

    [TestMethod]
    public void TheValidator_IsAStaticClass()
    {
        Type validator = typeof(InputValidator);

        // A static class is compiled as abstract and sealed: it can be neither created nor inherited.
        Assert.IsTrue(validator.IsAbstract && validator.IsSealed);
        Assert.IsTrue(validator.GetMethods(DeclaredMembers).All(method => method.IsStatic));
        Assert.AreEqual(0, validator.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length);
    }

    [TestMethod]
    public void TheCenter_KeepsOneListOfTheContractTypeAndStaticCounts()
    {
        Type center = typeof(RepairCenter);

        Assert.IsTrue(center.GetFields(DeclaredMembers).Any(field => field.FieldType == typeof(List<IAppliance>)));
        Assert.IsTrue(center.GetProperty(nameof(RepairCenter.TotalRegistered))!.GetMethod!.IsStatic);
        Assert.IsTrue(center.GetProperty(nameof(RepairCenter.UnderRepairCount))!.GetMethod!.IsStatic);
        Assert.IsFalse(center.GetProperty(nameof(RepairCenter.TotalRegistered))!.CanWrite);
    }

    [TestMethod]
    public void TheCenter_NeverNamesAConcreteApplianceType()
    {
        IEnumerable<Type> typesUsed = typeof(RepairCenter).GetMembers(DeclaredMembers)
            .SelectMany(TypesInSignature)
            .SelectMany(Unwrap);

        foreach (Type type in typesUsed)
        {
            Assert.IsFalse(
                typeof(Appliance).IsAssignableFrom(type),
                $"RepairCenter mentions {type.Name}; it must know appliances through IAppliance only.");
        }
    }

    private static IEnumerable<Type> TypesInSignature(MemberInfo member)
    {
        switch (member)
        {
            case FieldInfo field:
                return new[] { field.FieldType };
            case PropertyInfo property:
                return new[] { property.PropertyType };
            case MethodInfo method:
                return method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType);
            default:
                return Array.Empty<Type>();
        }
    }

    private static IEnumerable<Type> Unwrap(Type type)
    {
        return type.IsGenericType ? type.GetGenericArguments().SelectMany(Unwrap).Append(type) : new[] { type };
    }
}
