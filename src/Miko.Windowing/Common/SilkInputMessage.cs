using Miko.Common;
using Miko.Platform;

namespace Miko.Windowing.Common;

public enum SilkInputMessageKind
{
    PointerDown,
    PointerUp,
    PointerMove,
    Scroll,
    KeyDown,
    KeyUp,
    RepeatKey,
    TextInput,
    Resize,
}

/// <summary>Allocation-free native-window message passed to a host's render thread.</summary>
public readonly struct SilkInputMessage
{
    public SilkInputMessageKind Kind { get; }
    public float X { get; }
    public float Y { get; }
    public float DeltaX { get; }
    public float DeltaY { get; }
    public Events.MouseButton Button { get; }
    public MikoKey Key { get; }
    public MikoKeyModifiers Modifiers { get; }
    public string? Text { get; }

    private SilkInputMessage(
        SilkInputMessageKind kind,
        float x,
        float y,
        float deltaX,
        float deltaY,
        Events.MouseButton button,
        MikoKey key,
        MikoKeyModifiers modifiers,
        string? text)
    {
        Kind = kind;
        X = x;
        Y = y;
        DeltaX = deltaX;
        DeltaY = deltaY;
        Button = button;
        Key = key;
        Modifiers = modifiers;
        Text = text;
    }

    public static SilkInputMessage Pointer(
        SilkInputMessageKind kind,
        float x,
        float y,
        Events.MouseButton button = default)
        => new(kind, x, y, 0, 0, button, default, default, null);

    public static SilkInputMessage Scroll(float x, float y, float deltaX, float deltaY)
        => new(SilkInputMessageKind.Scroll, x, y, deltaX, deltaY, default, default, default, null);

    public static SilkInputMessage Keyboard(
        SilkInputMessageKind kind,
        MikoKey key,
        MikoKeyModifiers modifiers = default)
        => new(kind, 0, 0, 0, 0, default, key, modifiers, null);

    public static SilkInputMessage TextInput(string text)
        => new(SilkInputMessageKind.TextInput, 0, 0, 0, 0, default, default, default, text);

    public static SilkInputMessage Resize(int width, int height)
        => new(SilkInputMessageKind.Resize, width, height, 0, 0, default, default, default, null);
}
