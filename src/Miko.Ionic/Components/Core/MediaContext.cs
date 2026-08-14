namespace Miko.Ionic.Components;

/// <summary>
/// Context provided by media container components (<see cref="IonAvatar"/>, <see cref="IonThumbnail"/>)
/// to their descendants. Components that need to adjust their appearance when nested inside media
/// containers (e.g. <see cref="IonSkeletonText"/> applying zero margin and 100% height) check for
/// this context via <c>[CascadingParameter]</c>.
/// </summary>
/// <remarks>
/// This mirrors Ionic's <c>hostContext('ion-avatar', el) || hostContext('ion-thumbnail', el)</c>
/// pattern used in skeleton-text.tsx to apply the <c>.in-media</c> class.
/// </remarks>
public sealed class MediaContext
{
    /// <summary>
    /// Singleton instance provided by media containers via <c>CascadingValue</c>.
    /// </summary>
    public static readonly MediaContext Instance = new();

    private MediaContext() { }
}
