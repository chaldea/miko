namespace Miko.Ionic.Components;

/// <summary>Root overlay services supplied by <see cref="IonApp"/>.</summary>
public sealed class IonOverlayContext
{
    internal IonOverlayContext(IonOverlayRegistry registry)
    {
        Registry = registry;
    }

    internal IonOverlayRegistry Registry { get; }
}
