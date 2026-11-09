using Miko.Common;
using Miko.Platform;
using Silk.NET.Input;

namespace Miko.Windowing.Common;

/// <summary>Maps Silk.NET input types to platform-neutral Miko types.</summary>
public static class SilkKeyMap
{
    public static MikoKey ToMikoKey(Key key) => key switch
    {
        Key.Backspace => MikoKey.Backspace,
        Key.Delete => MikoKey.Delete,
        Key.Left => MikoKey.Left,
        Key.Right => MikoKey.Right,
        Key.Home => MikoKey.Home,
        Key.End => MikoKey.End,
        Key.Enter or Key.KeypadEnter => MikoKey.Enter,
        Key.Tab => MikoKey.Tab,
        Key.Escape => MikoKey.Escape,
        Key.ControlLeft => MikoKey.ControlLeft,
        Key.ControlRight => MikoKey.ControlRight,
        Key.ShiftLeft => MikoKey.ShiftLeft,
        Key.ShiftRight => MikoKey.ShiftRight,
        Key.AltLeft => MikoKey.AltLeft,
        Key.AltRight => MikoKey.AltRight,
        Key.F1 => MikoKey.F1,
        Key.F2 => MikoKey.F2,
        Key.F3 => MikoKey.F3,
        Key.F4 => MikoKey.F4,
        Key.F5 => MikoKey.F5,
        Key.F6 => MikoKey.F6,
        Key.F7 => MikoKey.F7,
        Key.F8 => MikoKey.F8,
        Key.F9 => MikoKey.F9,
        Key.F10 => MikoKey.F10,
        Key.F11 => MikoKey.F11,
        Key.F12 => MikoKey.F12,
        Key.Up => MikoKey.Up,
        Key.Down => MikoKey.Down,
        Key.PageUp => MikoKey.PageUp,
        Key.PageDown => MikoKey.PageDown,
        Key.Insert => MikoKey.Insert,
        Key.SuperLeft => MikoKey.MetaLeft,
        Key.SuperRight => MikoKey.MetaRight,
        Key.CapsLock => MikoKey.CapsLock,
        Key.NumLock => MikoKey.NumLock,
        Key.ScrollLock => MikoKey.ScrollLock,
        Key.PrintScreen => MikoKey.PrintScreen,
        Key.Pause => MikoKey.Pause,
        Key.Menu => MikoKey.Menu,
        Key.Space => MikoKey.Space,
        >= Key.A and <= Key.Z => MikoKey.A + (key - Key.A),
        >= Key.Number0 and <= Key.Number9 => MikoKey.D0 + (key - Key.Number0),
        Key.GraveAccent => MikoKey.Grave,
        Key.Minus => MikoKey.Minus,
        Key.Equal => MikoKey.Equal,
        Key.LeftBracket => MikoKey.LeftBracket,
        Key.RightBracket => MikoKey.RightBracket,
        Key.BackSlash => MikoKey.Backslash,
        Key.Semicolon => MikoKey.Semicolon,
        Key.Apostrophe => MikoKey.Apostrophe,
        Key.Comma => MikoKey.Comma,
        Key.Period => MikoKey.Period,
        Key.Slash => MikoKey.Slash,
        >= Key.Keypad0 and <= Key.Keypad9 => MikoKey.Keypad0 + (key - Key.Keypad0),
        Key.KeypadDecimal => MikoKey.KeypadDecimal,
        Key.KeypadDivide => MikoKey.KeypadDivide,
        Key.KeypadMultiply => MikoKey.KeypadMultiply,
        Key.KeypadSubtract => MikoKey.KeypadSubtract,
        Key.KeypadAdd => MikoKey.KeypadAdd,
        _ => MikoKey.Unknown,
    };

    public static MikoKeyModifiers GetModifiers(IKeyboard keyboard)
    {
        var modifiers = MikoKeyModifiers.None;
        if (keyboard.IsKeyPressed(Key.ControlLeft) || keyboard.IsKeyPressed(Key.ControlRight))
            modifiers |= MikoKeyModifiers.Control;
        if (keyboard.IsKeyPressed(Key.ShiftLeft) || keyboard.IsKeyPressed(Key.ShiftRight))
            modifiers |= MikoKeyModifiers.Shift;
        if (keyboard.IsKeyPressed(Key.AltLeft) || keyboard.IsKeyPressed(Key.AltRight))
            modifiers |= MikoKeyModifiers.Alt;
        if (keyboard.IsKeyPressed(Key.SuperLeft) || keyboard.IsKeyPressed(Key.SuperRight))
            modifiers |= MikoKeyModifiers.Meta;
        return modifiers;
    }

    public static StandardCursor ToStandardCursor(Cursor cursor) => cursor switch
    {
        Cursor.Pointer => StandardCursor.Hand,
        Cursor.Text => StandardCursor.IBeam,
        Cursor.Wait => StandardCursor.Wait,
        Cursor.NotAllowed => StandardCursor.NotAllowed,
        Cursor.Move => StandardCursor.ResizeAll,
        Cursor.Help => StandardCursor.Arrow,
        _ => StandardCursor.Default,
    };

    public static Events.MouseButton ToMikoButton(MouseButton button) => button switch
    {
        MouseButton.Middle => Events.MouseButton.Middle,
        MouseButton.Right => Events.MouseButton.Right,
        _ => Events.MouseButton.Left,
    };
}
