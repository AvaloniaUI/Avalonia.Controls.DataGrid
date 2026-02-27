using System;

namespace Avalonia.Controls.Utils;

internal sealed class SkipObservable<T>(IObservable<T> source, int skipCount) : IObservable<T>
{
    public IDisposable Subscribe(IObserver<T> observer)
    {
        var remaining = skipCount;

        return source.Subscribe(new AnonymousObserver<T>(
            value =>
            {
                if (remaining <= 0)
                    observer.OnNext(value);
                else
                    --remaining;
            },
            observer.OnError,
            observer.OnCompleted));
    }
}
