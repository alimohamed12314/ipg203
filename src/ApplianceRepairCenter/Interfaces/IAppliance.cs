using System;
using System.Collections.Generic;
using ApplianceRepairCenter.Enums;
using ApplianceRepairCenter.Events;
using ApplianceRepairCenter.Models;

namespace ApplianceRepairCenter.Interfaces;

/// <summary>
/// Everything the repair center needs from an appliance, whatever its type: its intake data,
/// the steps of the repair workflow, the price of the repair and a notice when its status changes.
/// The center works with this contract only, so a new appliance type needs no change in it.
/// </summary>
public interface IAppliance
{
    /// <summary>Raised each time the appliance moves to another stage of the workflow.</summary>
    event StatusChangedEventHandler? StatusChanged;

    /// <summary>The number that identifies the appliance; it never changes.</summary>
    string SerialNumber { get; }

    /// <summary>The customer who brought the appliance in and will be told when it is ready.</summary>
    Customer Owner { get; }

    /// <summary>The fault the customer reported at intake.</summary>
    string FaultDescription { get; }

    /// <summary>The day the center received the appliance.</summary>
    DateTime ReceivedDate { get; }

    /// <summary>The day the appliance was promised back; it may be postponed, never before intake.</summary>
    DateTime ExpectedDeliveryDate { get; set; }

    /// <summary>The stage the appliance has reached.</summary>
    ApplianceStatus Status { get; }

    /// <summary>True once the repair is finished, whether or not the owner has collected the appliance.</summary>
    bool IsRepaired { get; }

    /// <summary>The maintenance work recorded so far, oldest first.</summary>
    IReadOnlyList<RepairOperation> Operations { get; }

    /// <summary>Hands the received appliance to a technician.</summary>
    void StartRepair();

    /// <summary>Records one piece of maintenance work done while the appliance is under repair.</summary>
    void AddOperation(RepairOperation operation);

    /// <summary>Declares the repair finished; the appliance then waits for its owner.</summary>
    void CompleteRepair();

    /// <summary>Hands the repaired appliance back to its owner.</summary>
    void DeliverToOwner();

    /// <summary>Tells whether the repair is still unfinished after the promised delivery date.</summary>
    bool IsOverdue(DateTime asOf);

    /// <summary>Prices the repair as it stands now. Each appliance type has its own rules.</summary>
    decimal CalculateRepairCost();
}
