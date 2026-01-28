using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Headless;
using Avalonia.Threading;
using Xunit;

namespace Avalonia.Controls.DataGridTests;

public class LeakTests
{
    // Need to have the collection as field, so GC will not free it
    private readonly ObservableCollection<string> _observableCollection = new();

    [Fact]
    public async Task DataGrid_Is_Freed()
    {
        // When attached to INotifyCollectionChanged, DataGrid will subscribe to its events, potentially causing leak
        async Task<(Window Window, WeakReference<DataGrid> DataGrid)> Run()
        {
            using var session = HeadlessUnitTestSession.StartNew(typeof(Application));

            return await session.Dispatch(() =>
            {
                var dataGrid = new DataGrid { ItemsSource = _observableCollection };
                var window = new Window { Content = dataGrid };

                window.Show();

                // Do a layout and make sure that DataGrid gets added to visual tree.
                window.Show();
                Assert.IsType<DataGrid>(window.Presenter?.Child);

                // Clear the content and ensure the DataGrid is removed.
                window.Content = null;
                Dispatcher.UIThread.RunJobs();
                Assert.Null(window.Presenter.Child);

                return (window, new WeakReference<DataGrid>(dataGrid));
            }, CancellationToken.None);
        }

        var (window, dataGrid) = await Run();

        GC.Collect();
        GC.WaitForPendingFinalizers();

        GC.KeepAlive(window);
        Assert.False(dataGrid.TryGetTarget(out _));
    }
}
