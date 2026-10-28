using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Avalonia.Automation.Peers
{
#if !DATAGRID_INTERNAL
    public
#endif
    class DataGridRowAutomationPeer : ControlAutomationPeer, ISelectionItemProvider
    {
        public DataGridRowAutomationPeer(DataGridRow owner)
            : base(owner)
        {
        }

        private DataGridRow Row => (DataGridRow)Owner;

        /// <inheritdoc/>
        public bool IsSelected => Row is { OwningGrid: { } grid, Slot: >= 0 } row && grid.GetRowSelection(row.Slot);

        /// <inheritdoc/>
        public ISelectionProvider? SelectionContainer =>
            Row.OwningGrid is { } grid ? GetOrCreate(grid).GetProvider<ISelectionProvider>() : null;

        /// <inheritdoc/>
        public void Select()
        {
            EnsureEnabled();
            UpdateSelection(DataGridSelectionAction.SelectCurrent);
        }

        /// <inheritdoc/>
        public void AddToSelection()
        {
            EnsureEnabled();

            if (Row.OwningGrid?.SelectionMode == DataGridSelectionMode.Extended)
                UpdateSelection(DataGridSelectionAction.AddCurrentToSelection);
            else
                UpdateSelection(DataGridSelectionAction.SelectCurrent);
        }

        /// <inheritdoc/>
        public void RemoveFromSelection()
        {
            EnsureEnabled();

            if (IsSelected)
                UpdateSelection(DataGridSelectionAction.RemoveCurrentFromSelection);
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.DataItem;
        }

        protected override bool IsContentElementCore() => true;
        protected override bool IsControlElementCore() => true;

        private void UpdateSelection(DataGridSelectionAction action)
        {
            if (Row is { OwningGrid: { } grid, Slot: >= 0 } row)
                grid.UpdateSelectionAndCurrency(grid.CurrentColumnIndex, row.Slot, action, scrollIntoView: false);
        }
    }
}
