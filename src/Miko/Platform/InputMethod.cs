using Miko.Common;

namespace Miko.Platform;

/// <summary>Describes the text client currently exposed to a native input method.</summary>
public sealed record InputMethodState(
    string Text,
    int CursorPosition,
    bool IsMultiline,
    InputMethodType InputType,
    RectF CursorRect)
{
    public int SelectionStart => CursorPosition;
    public int SelectionEnd => CursorPosition;
}

/// <summary>Hints used by platform keyboards when configuring their editor.</summary>
public enum InputMethodType
{
    Text,
    Password,
    Number,
    Multiline,
}

/// <summary>
/// Platform-neutral endpoint for native text input and IME composition.
/// Implementations own the native editor/connection; the engine owns the text client.
/// </summary>
public interface IInputMethod
{
    event Action<string>? TextCommitted;
    event Action? CompositionStarted;
    event Action<string>? CompositionUpdated;
    event Action<string?>? CompositionEnded;

    /// <summary>Updates or clears the native text client and its screen position.</summary>
    void SetState(InputMethodState? state);

    /// <summary>
    /// Explicitly asks the platform to present its soft keyboard for the current client.
    /// <para>This is an <b>action</b>, not state. <see cref="SetState"/> is published every frame and
    /// deduplicated against the previous value, so it cannot express "show the keyboard again" when
    /// nothing about the text client changed — which is exactly what happens when the user dismisses
    /// the keyboard with the IME's own hide button and then taps the same field again: focus never
    /// moved, so the state is identical. Hosts must honour this unconditionally.</para>
    /// </summary>
    void ShowKeyboard();
}

/// <summary>Descriptive alias for dependency-injection registrations.</summary>
public interface IInputMethodService : IInputMethod
{
}

/// <summary>Convenience base class for platform implementations and tests.</summary>
public abstract class InputMethodBase : IInputMethodService
{
    public event Action<string>? TextCommitted;
    public event Action? CompositionStarted;
    public event Action<string>? CompositionUpdated;
    public event Action<string?>? CompositionEnded;

    public abstract void SetState(InputMethodState? state);

    /// <summary>Hosts without a dismissable soft keyboard need no action here.</summary>
    public virtual void ShowKeyboard() { }

    protected void CommitText(string text)
    {
        if (!string.IsNullOrEmpty(text)) TextCommitted?.Invoke(text);
    }

    protected void StartComposition() => CompositionStarted?.Invoke();
    protected void UpdateComposition(string text) => CompositionUpdated?.Invoke(text ?? string.Empty);
    protected void EndComposition(string? text) => CompositionEnded?.Invoke(text);
}

/// <summary>Inert endpoint used when a host does not provide a native IME.</summary>
public sealed class NullInputMethod : IInputMethodService
{
    public event Action<string>? TextCommitted { add { } remove { } }
    public event Action? CompositionStarted { add { } remove { } }
    public event Action<string>? CompositionUpdated { add { } remove { } }
    public event Action<string?>? CompositionEnded { add { } remove { } }
    public void SetState(InputMethodState? state) { }
    public void ShowKeyboard() { }
}
