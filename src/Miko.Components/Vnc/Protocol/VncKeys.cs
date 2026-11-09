using Miko.Platform;

namespace Miko.Components.Vnc.Protocol;

/// <summary>X11 keysyms, the key identity RFB sends (RFC 6143 §7.5.4).</summary>
public static class VncKeys
{
    public const uint BackSpace = 0xFF08;
    public const uint Tab = 0xFF09;
    public const uint Return = 0xFF0D;
    public const uint Pause = 0xFF13;
    public const uint ScrollLock = 0xFF14;
    public const uint Escape = 0xFF1B;
    public const uint Home = 0xFF50;
    public const uint Left = 0xFF51;
    public const uint Up = 0xFF52;
    public const uint Right = 0xFF53;
    public const uint Down = 0xFF54;
    public const uint PageUp = 0xFF55;
    public const uint PageDown = 0xFF56;
    public const uint End = 0xFF57;
    public const uint Print = 0xFF61;
    public const uint Insert = 0xFF63;
    public const uint Menu = 0xFF67;
    public const uint NumLock = 0xFF7F;
    public const uint KeypadEnter = 0xFF8D;
    public const uint F1 = 0xFFBE;
    public const uint ShiftLeft = 0xFFE1;
    public const uint ShiftRight = 0xFFE2;
    public const uint ControlLeft = 0xFFE3;
    public const uint ControlRight = 0xFFE4;
    public const uint CapsLock = 0xFFE5;
    public const uint AltLeft = 0xFFE9;
    public const uint AltRight = 0xFFEA;

    /// <summary>Super_L: the Windows key on Windows servers, Super / ⌘ on others.</summary>
    public const uint SuperLeft = 0xFFEB;
    public const uint SuperRight = 0xFFEC;
    public const uint Delete = 0xFFFF;

    /// <summary>
    /// The keysym of a key that types no text (editing, navigation, function and modifier keys), or of a
    /// printable key by its US-layout character. 0 for <see cref="MikoKey.Unknown"/>.
    /// </summary>
    public static uint FromKey(MikoKey key, bool shift = false) => key switch
    {
        MikoKey.Backspace => BackSpace,
        MikoKey.Tab => Tab,
        MikoKey.Enter => Return,
        MikoKey.Escape => Escape,
        MikoKey.Delete => Delete,
        MikoKey.Home => Home,
        MikoKey.End => End,
        MikoKey.Left => Left,
        MikoKey.Right => Right,
        MikoKey.Up => Up,
        MikoKey.Down => Down,
        MikoKey.PageUp => PageUp,
        MikoKey.PageDown => PageDown,
        MikoKey.Insert => Insert,
        >= MikoKey.F1 and <= MikoKey.F12 => F1 + (uint)(key - MikoKey.F1),
        MikoKey.ShiftLeft => ShiftLeft,
        MikoKey.ShiftRight => ShiftRight,
        MikoKey.ControlLeft => ControlLeft,
        MikoKey.ControlRight => ControlRight,
        MikoKey.AltLeft => AltLeft,
        MikoKey.AltRight => AltRight,
        MikoKey.MetaLeft => SuperLeft,
        MikoKey.MetaRight => SuperRight,
        MikoKey.CapsLock => CapsLock,
        MikoKey.NumLock => NumLock,
        MikoKey.ScrollLock => ScrollLock,
        MikoKey.PrintScreen => Print,
        MikoKey.Pause => Pause,
        MikoKey.Menu => Menu,
        _ => UsCharacter(key, shift) is char c ? FromChar(c) : 0,
    };

    /// <summary>True for keys that are held: Shift, Control, Alt and Meta (left and right).</summary>
    public static bool IsModifier(MikoKey key) => key is MikoKey.ShiftLeft or MikoKey.ShiftRight or MikoKey.ControlLeft
        or MikoKey.ControlRight or MikoKey.AltLeft or MikoKey.AltRight or MikoKey.MetaLeft or MikoKey.MetaRight;

    /// <summary>The character a printable key types on a US layout, or null for keys that type no text.</summary>
    public static char? UsCharacter(MikoKey key, bool shift = false) => key switch
    {
        >= MikoKey.A and <= MikoKey.Z => (char)((shift ? 'A' : 'a') + (key - MikoKey.A)),
        >= MikoKey.D0 and <= MikoKey.D9 => shift ? ")!@#$%^&*("[key - MikoKey.D0] : (char)('0' + (key - MikoKey.D0)),
        MikoKey.Space => ' ',
        MikoKey.Grave => shift ? '~' : '`',
        MikoKey.Minus => shift ? '_' : '-',
        MikoKey.Equal => shift ? '+' : '=',
        MikoKey.LeftBracket => shift ? '{' : '[',
        MikoKey.RightBracket => shift ? '}' : ']',
        MikoKey.Backslash => shift ? '|' : '\\',
        MikoKey.Semicolon => shift ? ':' : ';',
        MikoKey.Apostrophe => shift ? '"' : '\'',
        MikoKey.Comma => shift ? '<' : ',',
        MikoKey.Period => shift ? '>' : '.',
        MikoKey.Slash => shift ? '?' : '/',
        >= MikoKey.Keypad0 and <= MikoKey.Keypad9 => (char)('0' + (key - MikoKey.Keypad0)),
        MikoKey.KeypadDecimal => '.',
        MikoKey.KeypadDivide => '/',
        MikoKey.KeypadMultiply => '*',
        MikoKey.KeypadSubtract => '-',
        MikoKey.KeypadAdd => '+',
        _ => null,
    };

    /// <summary>
    /// The keysym of a typed character: Latin-1 characters are their own keysym, others are
    /// <c>0x01000000 + code point</c>; control characters map to their keys (newline → Return).
    /// </summary>
    public static uint FromChar(int codePoint) => codePoint switch
    {
        '\n' or '\r' => Return,
        '\t' => Tab,
        '\b' => BackSpace,
        0x1B => Escape,
        0x7F => Delete,
        < 0x20 => 0,
        <= 0xFF => (uint)codePoint,
        _ => 0x01000000u + (uint)codePoint,
    };
}
