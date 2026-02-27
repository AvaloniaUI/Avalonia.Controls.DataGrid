using System;
using System.Threading;

namespace Avalonia.Controls.Utils;

internal sealed class CompositeDisposable(IDisposable[] disposables) : IDisposable
{
    private IDisposable[]? _disposables = disposables;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposables, null) is { } disposables)
        {
            foreach (var disposable in disposables)
                disposable.Dispose();
        }
    }
}
