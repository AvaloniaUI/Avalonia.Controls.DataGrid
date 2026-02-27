using System;
using System.Threading;

namespace Avalonia.Controls.Utils;

internal sealed class ActionDisposable(Action dispose) : IDisposable
{
    private Action? _dispose = dispose;

    public void Dispose()
        => Interlocked.Exchange(ref _dispose, null)?.Invoke();
}
