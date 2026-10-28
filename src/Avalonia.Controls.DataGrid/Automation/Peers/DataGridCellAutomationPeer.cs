using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Controls.Automation.Peers;

#if !DATAGRID_INTERNAL
public
#endif
class DataGridCellAutomationPeer : ContentControlAutomationPeer, IInvokeProvider
{
    public DataGridCellAutomationPeer(DataGridCell owner)
        : base(owner)
    {
    }

    public new DataGridCell Owner => (DataGridCell)base.Owner;

    /// <summary>
    /// Makes the cell current, selects its row and begins editing the cell if it is not read-only.
    /// </summary>
    public void Invoke()
    {
        EnsureEnabled();

        if (Owner is { OwningGrid: { } grid, OwningRow.Slot: >= 0 and var slot, ColumnIndex: >= 0 and var column } &&
            grid.UpdateSelectionAndCurrency(column, slot, DataGridSelectionAction.SelectCurrent, scrollIntoView: false))
        {
            grid.BeginEdit();
        }
    }

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.Custom;
    }

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;
}
