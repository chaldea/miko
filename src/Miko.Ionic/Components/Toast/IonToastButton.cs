namespace Miko.Ionic.Components;

/// <summary>
/// A single toast button. Mirrors Ionic's <c>ToastButton</c> interface.
/// <para>
/// <see cref="Side"/> places the button in the start or end button group (default <c>"end"</c>).
/// A <see cref="Role"/> of <c>"cancel"</c> dismisses the toast without running the handler as a
/// normal action; any other role runs the <see cref="Handler"/> then dismisses. The role is also
/// stamped as <c>toast-button-{role}</c>.
/// </para>
/// </summary>
public sealed class IonToastButton
{
    /// <summary>The button label.</summary>
    public string? Text { get; set; }

    /// <summary>Optional leading icon (an Ionicons name).</summary>
    public string? Icon { get; set; }

    /// <summary>Which group the button belongs to: <c>"start"</c> or <c>"end"</c> (default). Anything
    /// other than <c>"start"</c> renders in the end group (matches toast.tsx).</summary>
    public string? Side { get; set; }

    /// <summary>Optional role: <c>"cancel"</c> or a custom string. Stamped as
    /// <c>toast-button-{role}</c>; a cancel role dismisses without running as a normal action.</summary>
    public string? Role { get; set; }

    /// <summary>Extra CSS class(es) to add to the button.</summary>
    public string? CssClass { get; set; }

    /// <summary>Optional handler run when a non-cancel button is tapped, before the toast dismisses.</summary>
    public Action? Handler { get; set; }

    /// <summary>True when this button dismisses the toast without acting as a normal option.</summary>
    public bool IsCancel => string.Equals(Role, "cancel", StringComparison.OrdinalIgnoreCase);

    /// <summary>True when this button belongs to the start group.</summary>
    public bool IsStart => string.Equals(Side, "start", StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the button shows only an icon (an icon but no text — toast.tsx
    /// <c>toast-button-icon-only</c>).</summary>
    public bool IsIconOnly => !string.IsNullOrEmpty(Icon) && string.IsNullOrEmpty(Text);
}
