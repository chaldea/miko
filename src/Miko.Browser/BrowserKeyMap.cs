using Miko.Platform;

namespace Miko.Browser;

/// <summary>Maps KeyboardEvent.code (a physical, US-layout key) to Miko's platform-neutral key.</summary>
internal static class BrowserKeyMap
{
    internal static MikoKey ToMikoKey(string? code) => code switch
    {
        "Backspace" => MikoKey.Backspace,
        "Delete" => MikoKey.Delete,
        "ArrowLeft" => MikoKey.Left,
        "ArrowRight" => MikoKey.Right,
        "ArrowUp" => MikoKey.Up,
        "ArrowDown" => MikoKey.Down,
        "Home" => MikoKey.Home,
        "End" => MikoKey.End,
        "PageUp" => MikoKey.PageUp,
        "PageDown" => MikoKey.PageDown,
        "Insert" => MikoKey.Insert,
        "Enter" or "NumpadEnter" => MikoKey.Enter,
        "Tab" => MikoKey.Tab,
        "Escape" => MikoKey.Escape,
        "ControlLeft" => MikoKey.ControlLeft,
        "ControlRight" => MikoKey.ControlRight,
        "ShiftLeft" => MikoKey.ShiftLeft,
        "ShiftRight" => MikoKey.ShiftRight,
        "AltLeft" => MikoKey.AltLeft,
        "AltRight" => MikoKey.AltRight,
        "MetaLeft" or "OSLeft" => MikoKey.MetaLeft,
        "MetaRight" or "OSRight" => MikoKey.MetaRight,
        "CapsLock" => MikoKey.CapsLock,
        "NumLock" => MikoKey.NumLock,
        "ScrollLock" => MikoKey.ScrollLock,
        "PrintScreen" => MikoKey.PrintScreen,
        "Pause" => MikoKey.Pause,
        "ContextMenu" => MikoKey.Menu,
        "Space" => MikoKey.Space,
        "Backquote" => MikoKey.Grave,
        "Minus" => MikoKey.Minus,
        "Equal" => MikoKey.Equal,
        "BracketLeft" => MikoKey.LeftBracket,
        "BracketRight" => MikoKey.RightBracket,
        "Backslash" => MikoKey.Backslash,
        "Semicolon" => MikoKey.Semicolon,
        "Quote" => MikoKey.Apostrophe,
        "Comma" => MikoKey.Comma,
        "Period" => MikoKey.Period,
        "Slash" => MikoKey.Slash,
        "Numpad0" => MikoKey.Keypad0,
        "Numpad1" => MikoKey.Keypad1,
        "Numpad2" => MikoKey.Keypad2,
        "Numpad3" => MikoKey.Keypad3,
        "Numpad4" => MikoKey.Keypad4,
        "Numpad5" => MikoKey.Keypad5,
        "Numpad6" => MikoKey.Keypad6,
        "Numpad7" => MikoKey.Keypad7,
        "Numpad8" => MikoKey.Keypad8,
        "Numpad9" => MikoKey.Keypad9,
        "NumpadDecimal" => MikoKey.KeypadDecimal,
        "NumpadDivide" => MikoKey.KeypadDivide,
        "NumpadMultiply" => MikoKey.KeypadMultiply,
        "NumpadSubtract" => MikoKey.KeypadSubtract,
        "NumpadAdd" => MikoKey.KeypadAdd,
        "F1" => MikoKey.F1,
        "F2" => MikoKey.F2,
        "F3" => MikoKey.F3,
        "F4" => MikoKey.F4,
        "F5" => MikoKey.F5,
        "F6" => MikoKey.F6,
        "F7" => MikoKey.F7,
        "F8" => MikoKey.F8,
        "F9" => MikoKey.F9,
        "F10" => MikoKey.F10,
        "F11" => MikoKey.F11,
        "F12" => MikoKey.F12,
        { } value when value.Length == 4 && value.StartsWith("Key", StringComparison.Ordinal) && value[3] is >= 'A' and <= 'Z'
            => MikoKey.A + (value[3] - 'A'),
        { } value when value.Length == 6 && value.StartsWith("Digit", StringComparison.Ordinal) && value[5] is >= '0' and <= '9'
            => MikoKey.D0 + (value[5] - '0'),
        _ => MikoKey.Unknown,
    };

    internal static bool IsEditingOrNavigationKey(MikoKey key) => key is
        MikoKey.Backspace or MikoKey.Delete or MikoKey.Left or MikoKey.Right or MikoKey.Up or MikoKey.Down or
        MikoKey.Home or MikoKey.End or MikoKey.PageUp or MikoKey.PageDown or MikoKey.Enter or MikoKey.Tab;
}
