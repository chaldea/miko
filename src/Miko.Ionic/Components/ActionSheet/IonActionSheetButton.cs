namespace Miko.Ionic.Components;

/// <summary>
/// A single action-sheet button. Mirrors Ionic's <c>ActionSheetButton</c> interface.
/// <para>
/// The <see cref="Role"/> drives styling and placement: the first <c>"cancel"</c> button is pulled
/// into its own bottom group; <c>"destructive"</c> buttons render in the destructive (danger) color;
/// <c>"selected"</c> renders bold. Every button — cancel included — runs its <see cref="Handler"/>
/// on tap and then dismisses the sheet with its role and <see cref="Data"/>.
/// </para>
/// </summary>
public sealed class IonActionSheetButton
{
    /// <summary>The button label.</summary>
    public string? Text { get; set; }

    /// <summary>Optional role: <c>"cancel"</c>, <c>"destructive"</c>, <c>"selected"</c>, or a custom
    /// string. Stamped as <c>action-sheet-{role}</c>.</summary>
    public string? Role { get; set; }

    /// <summary>Optional leading icon (an Ionicons name).</summary>
    public string? Icon { get; set; }

    /// <summary>Extra CSS class(es) to add to the button.</summary>
    public string? CssClass { get; set; }

    /// <summary>Arbitrary data returned with the dismiss event when this button is tapped.</summary>
    public object? Data { get; set; }

    /// <summary>When true, the button is drawn dimmed and tapping it does nothing. Ignored for
    /// cancel buttons, as in Ionic.</summary>
    public bool Disabled { get; set; }

    /// <summary>Optional handler run when the button is tapped, before the sheet dismisses.</summary>
    public Action? Handler { get; set; }

    /// <summary>True when <see cref="Role"/> is <c>"cancel"</c> (case-insensitive).</summary>
    public bool IsCancel => string.Equals(Role, "cancel", StringComparison.OrdinalIgnoreCase);
}
