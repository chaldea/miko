namespace Miko.Ionic.Components;

/// <summary>
/// A single action-sheet button. Mirrors Ionic's <c>ActionSheetButton</c> interface.
/// <para>
/// The <see cref="Role"/> drives both styling and dismiss semantics: <c>"cancel"</c> buttons are
/// pulled into their own bottom group and dismiss without running as a normal option;
/// <c>"destructive"</c> buttons render in the danger color (ios); <c>"selected"</c> marks the
/// chosen option. A <see cref="Handler"/> runs on tap before the sheet dismisses.
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

    /// <summary>When true the button is dimmed and non-interactive (ignored for cancel buttons).</summary>
    public bool Disabled { get; set; }

    /// <summary>Optional handler run when the button is tapped, before the sheet dismisses.</summary>
    public Action? Handler { get; set; }

    /// <summary>True when this button dismisses the sheet without acting as a normal option.</summary>
    public bool IsCancel => string.Equals(Role, "cancel", StringComparison.OrdinalIgnoreCase);
}
