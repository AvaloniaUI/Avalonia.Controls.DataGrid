using System;

namespace Avalonia.Controls.Utils;

internal sealed class AnonymousObserver<T>(Action<T> onNext, Action<Exception>? onError, Action? onCompleted) : IObserver<T>
{
    public void OnNext(T value)
        => onNext.Invoke(value);

    public void OnError(Exception error)
    {
        if (onError is not null)
            onError(error);
        else
            throw error;
    }

    public void OnCompleted()
        => onCompleted?.Invoke();
}
