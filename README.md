# Home Appliance Repair Center

A C# console application that manages a home appliance maintenance and repair center:
intake of appliances, the repair workflow, repair costs by appliance type, and notifying
customers when a repair is finished or runs late.

Assignment `TIC_IPG203_F25` of the course **Object-Oriented Programming and Design**
(البرمجة والتصميم غرضية التوجه), Technical Institute of Computer, Syrian Virtual University.

The full report (in Arabic) is in [`docs/Report.pdf`](docs/Report.pdf).

## Team

| # | Name | Student ID |
|---|------|------------|
| 1 |      |            |
| 2 |      |            |
| 3 |      |            |
| 4 |      |            |
| 5 |      |            |

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0), or Visual Studio 2022 (17.8 or newer)
  with the ".NET desktop development" workload.

## Running

**Visual Studio:** open `ApplianceRepairCenter.sln` and press <kbd>F5</kbd>.

**Command line**, from this folder:

```
dotnet run --project src/ApplianceRepairCenter             # the interactive menu
dotnet run --project src/ApplianceRepairCenter -- --demo   # the demo scenario only
dotnet test                                                # the automated tests
```

The demo scenario registers five appliances, repairs some of them and shows every feature
without typing any data. It is also the last entry of the menu, where it leaves its data in the
center so that you can go on working with it.

## What the system does

- Registers an appliance with its serial number, owner, reported fault, intake date and
  expected delivery date.
- Moves it through four states, always in this order:
  `Received` → `UnderRepair` → `Repaired` → `Delivered`.
- Records every repair operation: what was done, the hours it took and the spare parts used.
- Prices the repair with rules that differ by appliance type (amounts in Syrian pounds):

  | Type | Common part | What the type adds |
  |------|-------------|--------------------|
  | Refrigerator | inspection fee + parts + labor | cooling test priced per liter; 20% more labor for a no-frost model |
  | Washing machine | inspection fee + parts + labor | 25% more labor for a front-loading model; handling fee for a drum of 9 kg or more |
  | Air conditioner | inspection fee + parts + labor | refrigerant per started ton of cooling capacity; on-site visit fee for a split unit |

- Lists repaired appliances and appliances still under maintenance, reports the costs, and
  finds the appliances whose repair is overdue.
- Notifies the customer when a repair is completed or a delivery is overdue.

## Structure

```
ApplianceRepairCenter.sln
src/ApplianceRepairCenter/
    Program.cs        entry point: creates the center, subscribes the notifications, starts the menu
    Interfaces/       IAppliance - the contract the center works with
    Models/           Appliance (abstract), Refrigerator, WashingMachine, AirConditioner,
                      Customer, RepairOperation
    Enums/            ApplianceStatus
    Events/           StatusChangedEventHandler (delegate) and the event data classes
    Services/         RepairCenter - holds the list of appliances
    Validation/       InputValidator (static)
    UI/               console menu, input, output, customer notifications, demo scenario
tests/ApplianceRepairCenter.Tests/     MSTest unit tests
docs/                                  class diagram and the report
```

Only the classes in `UI/` read from the keyboard or write to the screen, so the rules of the
domain are tested without the console.

## Object-oriented design

![Class diagram](docs/class-diagram.png)

| Principle | Where |
|-----------|-------|
| Abstraction | `IAppliance` declares what the center needs from any appliance. The abstract class `Appliance` implements the shared part and leaves `CalculateRepairCost()` and `TypeName` abstract. |
| Inheritance | `Refrigerator`, `WashingMachine` and `AirConditioner` derive from `Appliance`; each adds only its own properties and pricing constants. |
| Polymorphism | Each type overrides `CalculateRepairCost()`. `RepairCenter` keeps one `List<IAppliance>` and totals the costs, finds overdue repairs and lists appliances without knowing their types. |
| Encapsulation | Every field is private. Intake data such as `SerialNumber` is read-only, `ExpectedDeliveryDate` validates what it is given, `Status` has a private setter, and `Operations` is a read-only view. |
| Delegates and events | The delegate `StatusChangedEventHandler`; the event `Appliance.StatusChanged`; the events `RepairCenter.RepairCompleted` and `RepairCenter.DeliveryOverdue`, handled by `CustomerNotifier`. |
| Static members | The static class `InputValidator` (serial number, phone, name, dates); the static properties `RepairCenter.TotalRegistered` and `RepairCenter.UnderRepairCount`. |

## Code style

The naming and formatting rules are in `.editorconfig`; Visual Studio applies them while typing.
To check them from the command line:

```
dotnet format --verify-no-changes
```
