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
