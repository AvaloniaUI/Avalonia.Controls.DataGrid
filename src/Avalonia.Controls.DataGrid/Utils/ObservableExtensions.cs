using System;

namespace Avalonia.Controls.Utils;

internal static class ObservableExtensions
{
    public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> action)
        => source.Subscribe(new AnonymousObserver<T>(action, null, null));

    public static IObservable<T> Skip<T>(this IObservable<T> source, int skipCount)
        => new SkipObservable<T>(source, skipCount);
}
