using Miko.Events;

namespace Miko.Ionic.Components;

/// <summary>
/// Gesture bridge between <see cref="IonContent"/> and its fixed-slot refresher. The refresher is
/// intentionally not hit-testable while inactive, so the content host owns the pointer handlers
/// and forwards only gestures that start in its scroll container.
/// </summary>
internal sealed class IonRefresherContext
{
    private static readonly AsyncLocal<IonRefresherContext?> CurrentScope = new();
    private IonRefresher? _refresher;

    internal static IonRefresherContext? Current => CurrentScope.Value;

    internal static IDisposable Push(IonRefresherContext context)
    {
        var previous = CurrentScope.Value;
        CurrentScope.Value = context;
        return new Scope(previous);
    }

    internal void Attach(IonRefresher refresher) => _refresher = refresher;

    internal void Detach(IonRefresher refresher)
    {
        if (ReferenceEquals(_refresher, refresher)) _refresher = null;
    }

    internal void PointerDown(PointerEventArgs args) => _refresher?.HandleContentPointerDown(args);
    internal void PointerMove(PointerEventArgs args) => _refresher?.HandleContentPointerMove(args);
    internal void PointerUp(PointerEventArgs args) => _refresher?.HandleContentPointerUp(args);
    internal void PointerCancel(PointerEventArgs args) => _refresher?.HandleContentPointerCancel(args);

    private sealed class Scope : IDisposable
    {
        private readonly IonRefresherContext? _previous;
        public Scope(IonRefresherContext? previous) => _previous = previous;
        public void Dispose() => CurrentScope.Value = _previous;
    }
}
