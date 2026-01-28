using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Avalonia.Headless;
using Avalonia.Threading;
using JetBrains.dotMemoryUnit;
using Xunit;

namespace Avalonia.Controls.DataGridTests;

[DotMemoryUnit(FailIfRunWithoutSupport = false)]
public class LeakTests
{
    // Need to have the collection as field, so GC will not free it
    private readonly ObservableCollection<string> _observableCollection = new();

    public LeakTests(ITestOutputHelper output)
    {
        DotMemoryUnitTestOutput.SetOutputMethod(output.WriteLine);
    }

    [Fact(Skip = "Run with dotMemory")]
    [SuppressMessage("Usage", "xUnit1031:Do not use blocking task operations in test method", Justification = "Needed for dotMemoryUnit to work")]
    public void DataGrid_Is_Freed()
    {
        // When attached to INotifyCollectionChanged, DataGrid will subscribe to its events, potentially causing leak
        var run = async () =>
        {
            using var session = HeadlessUnitTestSession.StartNew(typeof(Application));

            return await session.Dispatch(
                () => {
                    var window = new Window
                    {
                        Content = new DataGrid
                        {
                            ItemsSource = _observableCollection
                        }
                    };

                    window.Show();

                    // Do a layout and make sure that DataGrid gets added to visual tree.
                    window.Show();
                    Assert.IsType<DataGrid>(window.Presenter?.Child);

                    // Clear the content and ensure the DataGrid is removed.
                    window.Content = null;
                    Dispatcher.UIThread.RunJobs();
                    Assert.Null(window.Presenter.Child);

                    return window;
                },
                CancellationToken.None);
        };

        var result = run().GetAwaiter().GetResult();

        dotMemory.Check(memory =>
            Assert.Equal(0, memory.GetObjects(where => where.Type.Is<DataGrid>()).ObjectsCount));

        GC.KeepAlive(result);
    }
}
