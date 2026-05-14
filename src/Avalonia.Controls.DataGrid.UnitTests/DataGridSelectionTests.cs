using System;
using System.Collections;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Xunit;

namespace Avalonia.Controls.DataGridTests;

public class DataGridSelectionTests
{
    [AvaloniaFact]
    public void Moving_Unselected_Item_Does_Not_Affect_Selection()
    {
        var items = CreateData();
        var target = CreateTarget(items, selectedIndex: 0);
        var raised = 0;
        
        target.SelectionChanged += (_, _) => raised++;
        items.Move(2, 4);

        Assert.Same(items[0], target.SelectedItem);
        Assert.Equal(0, target.SelectedIndex);
        Assert.Equal(0, raised);
    }

    [AvaloniaFact]
    public void Move_With_No_Selection_Raises_Nothing()
    {
        var items = CreateData();
        var target = CreateTarget(items);
        var raised = 0;
        target.SelectionChanged += (_, _) => raised++;

        items.Move(1, 3);

        Assert.Null(target.SelectedItem);
        Assert.Equal(-1, target.SelectedIndex);
        Assert.Equal(0, raised);
    }

    [AvaloniaFact]
    public void MoveRange_In_Place_Raises_Nothing()
    {
        var items = CreateData();
        var target = CreateTarget(items, selectedIndex: 2);
        var selected = items[2];
        var raised = 0;
        target.SelectionChanged += (_, _) => raised++;

        // Move 3 items from index 1 to index 1 - no-op.
        items.MoveRange(1, 3, 1);

        Assert.Same(selected, target.SelectedItem);
        Assert.Equal(2, target.SelectedIndex);
        Assert.Equal(0, raised);
    }

    [AvaloniaFact]
    public void Move_Down_With_No_Selection_Raises_Nothing()
    {
        var items = CreateData();
        var target = CreateTarget(items);
        var snapshot = items.ToArray();
        var raised = 0;

        target.SelectionChanged += (_, _) => raised++;
        items.Move(0, 3);

        Assert.Equal(0, raised);
    }

    [AvaloniaFact]
    public void Move_Up_With_No_Selection_Raises_Nothing()
    {
        var items = CreateData();
        var target = CreateTarget(items);
        var snapshot = items.ToArray();
        var raised = 0;

        target.SelectionChanged += (_, _) => raised++;
        items.Move(3, 0);

        Assert.Equal(0, raised);
    }

    [AvaloniaFact]
    public void MoveRange_Down_With_No_Selection_Raises_Nothing()
    {
        var items = CreateData();
        var target = CreateTarget(items);
        var snapshot = items.ToArray();
        var raised = 0;

        target.SelectionChanged += (_, _) => raised++;
        items.MoveRange(1, 2, 4);

        Assert.Equal(0, raised);
    }

    [AvaloniaFact]
    public void MoveRange_Up_With_No_Selection_Raises_Nothing()
    {
        var items = CreateData();
        var target = CreateTarget(items);
        var snapshot = items.ToArray();
        var raised = 0;

        target.SelectionChanged += (_, _) => raised++;
        items.MoveRange(3, 2, 0);

        Assert.Equal(0, raised);
    }

    [AvaloniaFact]
    public void Moving_Selected_Item_Down_Preserves_SelectedItem()
    {
        var items = CreateData();
        var target = CreateTarget(items, selectedIndex: 1);

        items.Move(1, 3);

        // TODO: Decide what the expected behavior should be here. Should the selected item move
        // with the item, should the selection be cleared, or should the selection stay at index 1
        // (current behavior)?
        Assert.Same(items[3], target.SelectedItem);
        Assert.Equal(3, target.SelectedIndex);
    }

    [AvaloniaFact]
    public void Moving_Selected_Item_Up_Preserves_SelectedItem()
    {
        var items = CreateData();
        var target = CreateTarget(items, selectedIndex: 3);

        items.Move(3, 0);

        // Whereas Moving_Selected_Item_Preserves_SelectedItem shows that SelectedIndex is preserved when
        // moving selection down, this test shows that when moving selection up, the selected index moves
        // down to a previously unselected row.
        Assert.Equal(0, target.SelectedIndex);
        Assert.Same(items[0], target.SelectedItem);
    }

    [AvaloniaFact]
    public void Moving_Selected_Item_Down_Raises_Nothing()
    {
        var items = CreateData();
        var target = CreateTarget(items, selectedIndex: 1);
        var toMove = items[1];
        var raised = 0;

        // TODO: Decide what the expected behavior should be here. Currently "Item 1" is deselected
        // and then "Item 2" is selected.
        target.SelectionChanged += (_, e) => ++raised;

        items.Move(1, 3);

        Assert.Equal(0, raised);
    }
    
    [AvaloniaFact]
    public void Moving_Selected_Item_Up_Raises_Nothing()
    {
        var items = CreateData();
        var target = CreateTarget(items, selectedIndex: 3);
        var toMove = items[1];
        var raised = 0;

        // TODO: Decide what the expected behavior should be here. Currently "Item 1" is deselected
        // and then "Item 2" is selected.
        target.SelectionChanged += (_, e) => ++raised;

        items.Move(3, 0);

        Assert.Equal(0, raised);
    }

    [AvaloniaFact]
    public void Move_Of_Filtered_Out_Item_Is_NoOp_For_View()
    {
        var items = CreateData();
        var view = new DataGridCollectionView(items)
        {
            Filter = o => ((Model)o).Name != "Item 2"
        };
        var target = CreateTarget(view, selectedIndex: 0);
        var selected = items[0];
        var viewBefore = view.Cast<Model>().ToArray();
        var raised = 0;
        
        target.SelectionChanged += (_, _) => raised++;

        // Filtered-out item moves should be invisible to the view: no exception, no
        // selection change, view ordering unchanged.
        items.Move(2, 4);

        Assert.Equal(viewBefore, view.Cast<Model>());
        Assert.Same(selected, target.SelectedItem);
        Assert.Equal(0, raised);
    }

    private static AvaloniaList<Model> CreateData(int count = 5)
    {
        var items = Enumerable.Range(0, count).Select(x => new Model($"Item {x}"));
        return [.. items];
    }

    private static DataGrid CreateTarget(IList items, int selectedIndex = -1)
    {
        var root = new Window
        {
            Width = 200,
            Height = 100,
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
            Columns =
            {
                new DataGridTextColumn { Header = "Name", Binding = new Binding("Name") }
            },
            ItemsSource = items,
        };

        root.Content = target;
        root.Show();

        if (selectedIndex != -1)
            target.SelectedItem = items[selectedIndex];

        return target;
    }

    private class Model(string name)
    {
        public string Name { get; set; } = name;
        public override string ToString() => Name;
    }
}
