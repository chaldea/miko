namespace Miko.Ionic.Components;

/// <summary>
/// A single alert button. Mirrors Ionic's <c>AlertButton</c> interface.
/// <para>
/// A string button (e.g. <c>['Action']</c>) is normalized to a button whose <see cref="Text"/> is
/// that string and whose <see cref="Role"/> is <c>"cancel"</c> only when the text is "cancel"
/// (case-insensitive) — see <see cref="FromText"/>. <see cref="Role"/> is stamped as
/// <c>alert-button-role-{role}</c>.
/// </para>
/// </summary>
public sealed class IonAlertButton
{
    /// <summary>The button label.</summary>
    public string? Text { get; set; }

    /// <summary>Optional role: <c>"cancel"</c>, <c>"destructive"</c>, or a custom string.</summary>
    public string? Role { get; set; }

    /// <summary>Extra CSS class(es) to add to the button.</summary>
    public string? CssClass { get; set; }

    /// <summary>Optional handler run when the button is tapped (cancel buttons included), before the
    /// alert dismisses. It cannot keep the alert open.</summary>
    public Action? Handler { get; set; }

    /// <summary>True when <see cref="Role"/> is <c>"cancel"</c> (case-insensitive).</summary>
    public bool IsCancel => string.Equals(Role, "cancel", StringComparison.OrdinalIgnoreCase);

    /// <summary>Builds a button from a bare string, as Ionic's alert does for string buttons: the
    /// text becomes the label, and the role is <c>"cancel"</c> only when the text is "cancel"
    /// (case-insensitive).</summary>
    public static IonAlertButton FromText(string text) => new()
    {
        Text = text,
        Role = string.Equals(text, "cancel", StringComparison.OrdinalIgnoreCase) ? "cancel" : null,
    };
}
