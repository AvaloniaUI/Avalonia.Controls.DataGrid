using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests;

public class DataGridAutomationTests
{
    [AvaloniaFact]
    public void Selecting_Row_Peer_Selects_Row()
    {
        var target = CreateTarget(DataGridSelectionMode.Single);
        var selectionChangedCount = 0;
        target.SelectionChanged += (_, _) => ++selectionChangedCount;

        var provider = GetRowProvider(target, 1);
        provider.Select();

        Assert.Equal(1, target.SelectedIndex);
        Assert.Equal(1, selectionChangedCount);
        Assert.True(provider.IsSelected);
    }

    [AvaloniaFact]
    public void Adding_Row_Peer_To_Selection_In_Single_Mode_Replaces_Selection()
    {
        var target = CreateTarget(DataGridSelectionMode.Single);
        target.SelectedIndex = 0;

        GetRowProvider(target, 2).AddToSelection();

        Assert.Equal(2, target.SelectedIndex);
        Assert.Single(target.SelectedItems);
        Assert.False(GetRowProvider(target, 0).IsSelected);
    }

    [AvaloniaFact]
    public void Adding_Row_Peer_To_Selection_In_Extended_Mode_Keeps_Existing_Selection()
    {
        var target = CreateTarget(DataGridSelectionMode.Extended);
        var items = (IList<Model>)target.ItemsSource;
        target.SelectedIndex = 0;

        GetRowProvider(target, 2).AddToSelection();

        Assert.Equal(new[] { items[0], items[2] }, target.SelectedItems.Cast<Model>().OrderBy(x => x.Name));
        Assert.True(GetRowProvider(target, 0).IsSelected);
        Assert.True(GetRowProvider(target, 2).IsSelected);
    }

    [AvaloniaFact]
    public void Selecting_Row_Peer_In_Extended_Mode_Replaces_Selection()
    {
        var target = CreateTarget(DataGridSelectionMode.Extended);
        var items = (IList<Model>)target.ItemsSource;
        target.SelectedItems.Add(items[0]);
        target.SelectedItems.Add(items[1]);

        GetRowProvider(target, 2).Select();

        Assert.Equal(new[] { items[2] }, target.SelectedItems.Cast<Model>());
    }

    [AvaloniaFact]
    public void Removing_Row_Peer_From_Selection_In_Extended_Mode_Keeps_Other_Rows()
    {
        var target = CreateTarget(DataGridSelectionMode.Extended);
        var items = (IList<Model>)target.ItemsSource;
        target.SelectedItems.Add(items[0]);
        target.SelectedItems.Add(items[1]);

        var provider = GetRowProvider(target, 1);
        provider.RemoveFromSelection();

        Assert.Equal(new[] { items[0] }, target.SelectedItems.Cast<Model>());
        Assert.False(provider.IsSelected);
    }

    [AvaloniaFact]
    public void Removing_Row_Peer_From_Selection_In_Single_Mode_Clears_Selection()
    {
        var target = CreateTarget(DataGridSelectionMode.Single);
        target.SelectedIndex = 1;

        GetRowProvider(target, 1).RemoveFromSelection();

        Assert.Equal(-1, target.SelectedIndex);
        Assert.Empty(target.SelectedItems);
    }

    [AvaloniaFact]
    public void Removing_Unselected_Row_Peer_From_Selection_Does_Nothing()
    {
        var target = CreateTarget(DataGridSelectionMode.Single);
        target.SelectedIndex = 0;

        GetRowProvider(target, 1).RemoveFromSelection();

        Assert.Equal(0, target.SelectedIndex);
    }

    [AvaloniaFact]
    public void Selecting_Row_Peer_Of_Disabled_DataGrid_Throws()
    {
        var target = CreateTarget(DataGridSelectionMode.Single);
        target.IsEnabled = false;

        var provider = GetRowProvider(target, 1);

        Assert.Throws<ElementNotEnabledException>(() => provider.Select());
        Assert.Equal(-1, target.SelectedIndex);
    }

    [AvaloniaFact]
    public void Invoking_Cell_Peer_Selects_Row_And_Begins_Edit()
    {
        var target = CreateTarget(DataGridSelectionMode.Single);
        var cell = GetCell(target, 1, 1);

        GetCellProvider(cell).Invoke();

        Assert.Equal(1, target.SelectedIndex);
        Assert.Same(target.Columns[1], target.CurrentColumn);
        Assert.IsType<TextBox>(cell.Content);
    }

    [AvaloniaFact]
    public void Invoking_Cell_Peer_Of_Read_Only_DataGrid_Selects_Row_Without_Editing()
    {
        var target = CreateTarget(DataGridSelectionMode.Single);
        target.IsReadOnly = true;
        var cell = GetCell(target, 2, 1);

        GetCellProvider(cell).Invoke();

        Assert.Equal(2, target.SelectedIndex);
        Assert.Same(target.Columns[1], target.CurrentColumn);
        Assert.IsType<TextBlock>(cell.Content);
    }

    [AvaloniaFact]
    public void Invoking_Cell_Peer_In_Extended_Mode_Replaces_Selection()
    {
        var target = CreateTarget(DataGridSelectionMode.Extended);
        var items = (IList<Model>)target.ItemsSource;
        target.SelectedItems.Add(items[0]);
        target.SelectedItems.Add(items[1]);

        GetCellProvider(GetCell(target, 2, 1)).Invoke();

        Assert.Equal(new[] { items[2] }, target.SelectedItems.Cast<Model>());
    }

    private static DataGrid CreateTarget(DataGridSelectionMode selectionMode)
    {
        var items = Enumerable.Range(0, 10).Select(x => new Model($"Item {x}", $"Value {x}")).ToList();

        var root = new Window
        {
            Width = 300,
            Height = 200,
            Styles =
            {
                new StyleInclude((Uri?)null)
                {
                    Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Simple.xaml")
                },
            }
        };

        var target = new DataGrid
        {
            // Template columns, because the editing theme of DataGridTextColumn needs an application theme.
            Columns = { CreateColumn("Name"), CreateColumn("Value") },
            ItemsSource = items,
            SelectionMode = selectionMode,
        };

        root.Content = target;
        root.Show();
        return target;
    }

    private static DataGridTemplateColumn CreateColumn(string header)
    {
        return new DataGridTemplateColumn
        {
            Header = header,
            CellTemplate = new FuncDataTemplate<Model>((_, _) => new TextBlock()),
            CellEditingTemplate = new FuncDataTemplate<Model>((_, _) => new TextBox()),
        };
    }

    private static DataGridRow GetRow(DataGrid target, int index)
    {
        return target.GetVisualDescendants().OfType<DataGridRow>().Single(x => x.Index == index);
    }

    private static DataGridCell GetCell(DataGrid target, int rowIndex, int columnIndex)
    {
        return GetRow(target, rowIndex).GetVisualDescendants().OfType<DataGridCell>()
            .Single(x => x.OwningColumn == target.Columns[columnIndex]);
    }

    private static ISelectionItemProvider GetRowProvider(DataGrid target, int index)
    {
        var peer = ControlAutomationPeer.CreatePeerForElement(GetRow(target, index));
        var provider = peer.GetProvider<ISelectionItemProvider>();
        Assert.NotNull(provider);
        return provider;
    }

    private static IInvokeProvider GetCellProvider(DataGridCell cell)
    {
        var peer = ControlAutomationPeer.CreatePeerForElement(cell);
        var provider = peer.GetProvider<IInvokeProvider>();
        Assert.NotNull(provider);
        return provider;
    }

    private class Model
    {
        public Model(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; set; }
        public string Value { get; set; }
    }
}
