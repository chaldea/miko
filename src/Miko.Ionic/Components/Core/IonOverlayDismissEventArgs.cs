namespace Miko.Ionic.Components;

/// <summary>
/// Payload for an overlay's dismiss event (<c>ion-action-sheet</c>, <c>ion-alert</c>, …). Mirrors
/// Ionic's <c>OverlayEventDetail</c> (<c>{ role, data }</c>).
/// <para>
/// <see cref="Role"/> is the tapped button's role (<c>"cancel"</c>, <c>"destructive"</c>, a custom
/// role, or <c>"backdrop"</c> when the backdrop was tapped). <see cref="Data"/> is the arbitrary
/// value attached to the tapped button (or any inputs' values for an alert).
/// </para>
/// </summary>
public sealed class IonOverlayDismissEventArgs
{
    /// <summary>The role of the button that dismissed the overlay, or <c>"backdrop"</c>.</summary>
    public string? Role { get; }

    /// <summary>Data attached to the dismissing button (or collected input values).</summary>
    public object? Data { get; }

    public IonOverlayDismissEventArgs(string? role, object? data)
    {
        Role = role;
        Data = data;
    }

    /// <summary>True when the overlay was dismissed via a cancel button or the backdrop.</summary>
    public bool IsCancel =>
        string.Equals(Role, "cancel", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Role, "backdrop", StringComparison.OrdinalIgnoreCase);
}
