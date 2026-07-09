using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests;

public class DataGridRowGroupCollapseTests
{
    [AvaloniaFact]
    public void InitiallyCollapsed_Generates_No_Expanded_Rows_And_Counts_Only_Headers()
    {
        var view = CreateGroupedView(groups: 10, rowsPerGroup: 10);
        var target = CreateTarget(view, initiallyCollapsed: true);

        // No data rows are realized when collapsed.
        Assert.Empty(GetRows(target));
        Assert.NotEmpty(GetGroupHeaders(target));

        // Only the 10 group headers are visible; the 100 child rows are accounted for but hidden.
        Assert.Equal(10, target.VisibleSlotCount);
        Assert.Equal(110, target.SlotCount);
    }

    [AvaloniaFact]
    public void InitiallyCollapsed_Has_Smaller_Scroll_Extent_Than_Expanded()
    {
        var collapsed = CreateTarget(CreateGroupedView(10, 10), initiallyCollapsed: true);
        var expanded = CreateTarget(CreateGroupedView(10, 10), initiallyCollapsed: false);

        // VisibleSlotCount and LastVisibleSlot drive the scroll extent. Only the 10 headers are
        // visible, and the last reachable slot is group 9's header (99), not a hidden row.
        Assert.Equal(10, collapsed.VisibleSlotCount);
        Assert.Equal(99, collapsed.LastVisibleSlot);

        // Expanded: all 110 slots are visible, down to the last data row.
        Assert.Equal(110, expanded.VisibleSlotCount);
        Assert.Equal(109, expanded.LastVisibleSlot);
    }

    [AvaloniaFact]
    public void ExpandRowGroup_After_InitialCollapse_Reveals_Only_That_Group()
    {
        var view = CreateGroupedView(groups: 5, rowsPerGroup: 10);
        var target = CreateTarget(view, initiallyCollapsed: true);

        Assert.Empty(GetRows(target));
        Assert.Equal(5, target.VisibleSlotCount);

        // Expanding a group that was never expanded must not throw on a null materialized header.
        var g0 = (DataGridCollectionViewGroup)view.Groups[0];
        target.ExpandRowGroup(g0, expandAllSubgroups: false);
        target.UpdateLayout();

        // 5 headers + g0's 10 rows are now visible; the total slot count is unchanged.
        Assert.Equal(15, target.VisibleSlotCount);
        Assert.Equal(55, target.SlotCount);

        // No phantom rows: every realized row belongs to g0.
        var g0Items = g0.Items.Cast<object>().ToList();
        var rows = GetRows(target);
        Assert.NotEmpty(rows);
        Assert.All(rows, r => Assert.Contains(r.DataContext, g0Items));
    }

    [AvaloniaFact]
    public void Default_FlagFalse_Leaves_Groups_Expanded_Unchanged()
    {
        var view = CreateGroupedView(groups: 10, rowsPerGroup: 10);
        var target = CreateTarget(view, initiallyCollapsed: false);

        // Default behavior: every slot is visible and data rows are realized.
        Assert.Equal(110, target.VisibleSlotCount);
        Assert.Equal(110, target.SlotCount);
        Assert.NotEmpty(GetRows(target));
    }

    [AvaloniaFact]
    public void InitiallyCollapsed_Virtualized_Scroll_Keeps_Extent_And_Collapsed_State()
    {
        var view = CreateGroupedView(groups: 30, rowsPerGroup: 5);
        var target = CreateTarget(view, initiallyCollapsed: true);

        // 30 headers + 150 rows. Extent spans only the headers: last reachable slot is group 29's
        // header at slot 174 (29 * 6), not its hidden rows at 175..179.
        Assert.Equal(30, target.VisibleSlotCount);
        Assert.Equal(180, target.SlotCount);
        Assert.Equal(174, target.LastVisibleSlot);
        Assert.Empty(GetRows(target));

        // Scroll a header in the middle of the list into view. A top-level header slot is never
        // collapsed, so this scrolls without expanding anything.
        var gMid = (DataGridCollectionViewGroup)view.Groups[15];
        target.ScrollIntoView(gMid, null);
        target.UpdateLayout();

        // Still collapsed after scrolling mid-list; counts and extent are unchanged.
        Assert.Empty(GetRows(target));
        Assert.Equal(30, target.VisibleSlotCount);
        Assert.Equal(180, target.SlotCount);
        Assert.Equal(174, target.LastVisibleSlot);
        Assert.Contains(GetGroupHeaders(target), h => ReferenceEquals(h.DataContext, gMid));
    }

    [AvaloniaFact]
    public void InitiallyCollapsed_With_Empty_Group_Does_Not_Throw()
    {
        // An explicit group key that no item matches produces a header with no child rows.
        var view = CreateGroupedView(groups: 3, rowsPerGroup: 4, explicitGroupKeys: new object[] { "Empty" });
        var target = CreateTarget(view, initiallyCollapsed: true);

        // 3 populated groups + 1 empty group = 4 headers; 12 child rows hidden.
        Assert.Equal(4, target.VisibleSlotCount);
        Assert.Equal(16, target.SlotCount);
        Assert.Empty(GetRows(target));
    }

    [AvaloniaFact]
    public void Setting_Flag_After_Load_Is_NoOp()
    {
        var view = CreateGroupedView(groups: 5, rowsPerGroup: 10);
        var target = CreateTarget(view, initiallyCollapsed: false);

        Assert.Equal(55, target.VisibleSlotCount);

        // A late write does not reach the snapshot field, so already-generated groups are untouched.
        target.AreRowGroupsInitiallyCollapsed = true;
        target.UpdateLayout();

        Assert.Equal(55, target.VisibleSlotCount);
        Assert.NotEmpty(GetRows(target));
    }

    private static DataGridCollectionView CreateGroupedView(int groups, int rowsPerGroup, IEnumerable<object> explicitGroupKeys = null)
    {
        var items = new List<Item>();
        for (int g = 0; g < groups; g++)
        {
            string key = $"Group {g:00}";
            for (int r = 0; r < rowsPerGroup; r++)
            {
                items.Add(new Item(key, $"Item {g:00}-{r:00}"));
            }
        }

        var view = new DataGridCollectionView(items);
        var groupDescription = new DataGridPathGroupDescription("Group");
        if (explicitGroupKeys is not null)
        {
            foreach (var key in explicitGroupKeys)
            {
                groupDescription.GroupKeys.Add(key);
            }
        }
        view.GroupDescriptions.Add(groupDescription);
        return view;
    }

    private static DataGrid CreateTarget(DataGridCollectionView view, bool initiallyCollapsed)
    {
        var root = new Window
        {
            Width = 200,
            Height = 100,
            Styles =
            {
                new StyleInclude((Uri)null)
                {
                    Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Simple.xaml")
                },
            }
        };

        var target = new DataGrid
        {
            // Set before ItemsSource so the snapshot is taken when row groups are generated.
            AreRowGroupsInitiallyCollapsed = initiallyCollapsed,
            Columns =
            {
                new DataGridTextColumn { Header = "Name", Binding = new Binding("Name") }
            },
            ItemsSource = view,
            HeadersVisibility = DataGridHeadersVisibility.All,
        };

        root.Content = target;
        root.Show();
        return target;
    }

    private static IReadOnlyList<DataGridRow> GetRows(DataGrid target)
    {
        return target.GetSelfAndVisualDescendants().OfType<DataGridRow>().ToList();
    }

    private static IReadOnlyList<DataGridRowGroupHeader> GetGroupHeaders(DataGrid target)
    {
        return target.GetSelfAndVisualDescendants().OfType<DataGridRowGroupHeader>().ToList();
    }

    public class Item
    {
        public Item(string group, string name)
        {
            Group = group;
            Name = name;
        }

        public string Group { get; }
        public string Name { get; }
    }
}
