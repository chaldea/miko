namespace Miko.Ionic.Components;

/// <summary>
/// Payload for an overlay's dismiss event (<c>ion-action-sheet</c>, <c>ion-alert</c>, …). Mirrors
/// Ionic's <c>OverlayEventDetail</c> (<c>{ role, data }</c>).
/// <para>
/// <see cref="Role"/> is the tapped button's role (<c>"cancel"</c>, <c>"destructive"</c>, a custom
/// role), <c>"backdrop"</c> (backdrop tap), <c>"back"</c> (system back button), <c>"gesture"</c>
/// (sheet modal dragged closed), <c>"timeout"</c> (toast duration), or the role passed to
/// <c>DismissAsync</c>. <see cref="Data"/> is the value attached to the tapped action-sheet button,
/// an alert's input values, or the data passed to <c>DismissAsync</c>.
/// </para>
/// </summary>
public sealed class IonOverlayDismissEventArgs
{
    /// <summary>How the overlay was dismissed: the tapped button's role, or one of the roles listed
    /// on the type.</summary>
    public string? Role { get; }

    /// <summary>Data attached to the dismissing button, collected alert input values, or the data
    /// passed to <c>DismissAsync</c>.</summary>
    public object? Data { get; }

    public IonOverlayDismissEventArgs(string? role, object? data)
    {
        Role = role;
        Data = data;
    }

    /// <summary>True when <see cref="Role"/> is <c>"cancel"</c> or <c>"backdrop"</c>
    /// (case-insensitive); false for every other role, including <c>"back"</c>.</summary>
    public bool IsCancel =>
        string.Equals(Role, "cancel", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Role, "backdrop", StringComparison.OrdinalIgnoreCase);
}
