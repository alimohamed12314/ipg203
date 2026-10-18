namespace ApplianceRepairCenter.Events;

/// <summary>
/// The shape of any method that wants to be told when an appliance moves to another stage.
/// </summary>
/// <param name="sender">The object that raised the event.</param>
/// <param name="e">Which appliance changed, and from which status to which.</param>
public delegate void StatusChangedEventHandler(object? sender, StatusChangedEventArgs e);
