using System.Collections;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Avalonia.Controls.DataGridTests;

public class DataGridCollectionViewTests
{
    [AvaloniaFact]
    public void MoveRange_With_Target_Inside_Block_Keeps_View_In_Sync()
    {
        var items = CreateData();
        var (_, view) = CreateTarget(items);

        items.MoveRange(1, 3, 2);

        Assert.Equal(items, view.Cast<Model>());
    }

    private static AvaloniaList<Model> CreateData(int count = 5)
    {
        var items = Enumerable.Range(0, count).Select(x => new Model($"Item {x}"));
        return [.. items];
    }

    private static (DataGrid, DataGridCollectionView) CreateTarget(IList items)
    {
        var view = new DataGridCollectionView(items);

        var root = new Window
        {
            Width = 200,
            Height = 100,
        };

        var target = new DataGrid
        {
            Columns =
            {
                new DataGridTextColumn { Header = "Name", Binding = new Binding("Name") }
            },
            ItemsSource = view,
        };

        root.Content = target;
        root.Show();
        return (target, view);
    }

    private class Model(string name)
    {
        public string Name { get; set; } = name;
    }
}
