namespace Miko.Ionic.Components;

/// <summary>
/// Presence marker cascaded from an <see cref="IonListHeader"/> to descendant buttons.
/// Ionic uses <c>closest('ion-list-header')</c> to make an unset button fill default to clear.
/// </summary>
public sealed class IonListHeaderContext
{
    /// <summary>Singleton marker instance; list headers do not expose per-instance context state.</summary>
    public static readonly IonListHeaderContext Instance = new();

    private IonListHeaderContext() { }
}
