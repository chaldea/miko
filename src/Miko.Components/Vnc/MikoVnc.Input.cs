using Miko.Components.Vnc.Protocol;
using Miko.Events;
using Miko.Platform;

namespace Miko.Components.Vnc;

// Pointer, touch, wheel and keyboard input on the remote screen.
public sealed partial class MikoVnc
{
    private const float DragThreshold = 10;
    private const float WheelStep = 40;          // a desktop wheel notch
    private const float TouchScrollStep = 24;    // finger travel per scroll notch
    private static readonly TimeSpan HoldDelay = TimeSpan.FromMilliseconds(500);

    private enum Gesture { None, Down, Held, Drag, Pan, Scroll }

    private int _buttons;                         // RFB button mask of the mouse
    private float _wheelX, _wheelY;
    private Gesture _gesture;
    private float _touchStartX, _touchStartY, _touchLastX, _touchLastY, _touchScroll;
    private ITimer? _holdTimer;
    private int _holdGeneration;

    // Keys sent as pressed, by physical key (to release what was pressed even if modifiers changed since).
    private readonly Dictionary<MikoKey, uint> _pressedKeys = new();
    private readonly HashSet<uint> _heldModifiers = new();
    // Modifiers latched from the keys bar: pressed now, released after the next key.
    private readonly HashSet<uint> _sticky = new();

    private bool AcceptsInput => State == VncState.Connected && !IsViewOnly && _client != null;

    // ---- pointer -------------------------------------------------------------------

    private void OnScreenPointerDown(PointerEventArgs e)
    {
        // The press keeps focus (on a phone: the soft keyboard stays up) and is not a page gesture.
        e.PreventDefault();
        e.StopPropagation();
        if (!AcceptsInput) return;
        if (e.PointerType == PointerType.Mouse)
        {
            _buttons |= ButtonBit(e.Button);
            var (rx, ry) = MouseToRemote(e.OffsetX, e.OffsetY);
            SendPointer(rx, ry);
            // A click on the remote screen gives it the keyboard (a phone asks for the keyboard button instead).
            if (!IsTouchPlatform && !IsKeyboardActive) SetKeyboard(true, showSoftKeyboard: false);
        }
        else if (e.Button == MouseButton.Left)
        {
            TouchDown(e.OffsetX, e.OffsetY);
        }
    }

    private void OnScreenPointerMove(PointerEventArgs e)
    {
        if (!AcceptsInput) return;
        e.PreventDefault();
        e.StopPropagation();
        if (e.PointerType == PointerType.Mouse)
        {
            var (rx, ry) = MouseToRemote(e.OffsetX, e.OffsetY);
            SendPointer(rx, ry);
        }
        else
        {
            TouchMove(e.OffsetX, e.OffsetY);
        }
    }

    private void OnScreenPointerUp(PointerEventArgs e)
    {
        e.StopPropagation();
        if (!AcceptsInput) return;
        if (e.PointerType == PointerType.Mouse)
        {
            _buttons &= ~ButtonBit(e.Button);
            var (rx, ry) = MouseToRemote(e.OffsetX, e.OffsetY);
            SendPointer(rx, ry);
        }
        else if (e.Button == MouseButton.Left)
        {
            TouchUp(e.OffsetX, e.OffsetY);
        }
    }

    private void OnScreenPointerCancel(PointerEventArgs e)
    {
        if (_gesture == Gesture.Drag && _view.Pointer is { } p)
            _client?.SendPointer((int)p.X, (int)p.Y, 0);
        ResetGesture();
        ReleaseButtons();
    }

    private void OnScreenWheel(WheelEventArgs e)
    {
        e.PreventDefault();
        e.StopPropagation();
        if (!AcceptsInput) return;
        if (_view.Pointer == null)
        {
            var (rx, ry) = MouseToRemote(e.OffsetX, e.OffsetY);
            _view.Pointer = (rx, ry);
        }
        // Each notch is a press and release of button 4 / 5 (up / down) or 6 / 7 (left / right).
        _wheelY += e.DeltaY;
        _wheelX += e.DeltaX;
        while (_wheelY <= -WheelStep) { WheelPulse(8); _wheelY += WheelStep; }
        while (_wheelY >= WheelStep) { WheelPulse(16); _wheelY -= WheelStep; }
        while (_wheelX <= -WheelStep) { WheelPulse(32); _wheelX += WheelStep; }
        while (_wheelX >= WheelStep) { WheelPulse(64); _wheelX -= WheelStep; }
    }

    private static int ButtonBit(MouseButton button) => button switch
    {
        MouseButton.Middle => 2,
        MouseButton.Right => 4,
        _ => 1,
    };

    // With a mouse a zoomed picture follows the pointer: its place in the view is its place on the desktop.
    private (float X, float Y) MouseToRemote(float viewX, float viewY)
    {
        if (_view.CanPan) _view.Track(viewX, viewY);
        return _view.ToRemote(viewX, viewY);
    }

    private void SendPointer(float remoteX, float remoteY)
    {
        if (_view.Framebuffer is not { } fb) return;
        remoteX = Math.Clamp(remoteX, 0, fb.Width - 1);
        remoteY = Math.Clamp(remoteY, 0, fb.Height - 1);
        _view.Pointer = (remoteX, remoteY);
        _client?.SendPointer((int)remoteX, (int)remoteY, _buttons);
        Repaint();
    }

    private void WheelPulse(int bit)
    {
        if (_client is not { } client || _view.Pointer is not { } p) return;
        client.SendPointer((int)p.X, (int)p.Y, _buttons | bit);
        client.SendPointer((int)p.X, (int)p.Y, _buttons);
    }

    private void ReleaseButtons()
    {
        if (_buttons != 0 && _view.Pointer is { } p) _client?.SendPointer((int)p.X, (int)p.Y, 0);
        _buttons = 0;
    }

    // ---- touch ---------------------------------------------------------------------
    // Tap: left click. Hold: right click on release. Hold, then drag: left-button drag. Drag: pans a zoomed
    // picture, scrolls one that fits.

    private void TouchDown(float x, float y)
    {
        CancelHold();
        _gesture = Gesture.Down;
        _touchStartX = _touchLastX = x;
        _touchStartY = _touchLastY = y;
        _touchScroll = 0;
        int generation = ++_holdGeneration;
        var clock = Clock ?? TimeProvider.System;
        using (ExecutionContext.SuppressFlow())
            _holdTimer = clock.CreateTimer(_ => PostToRenderThread(() =>
            {
                if (generation != _holdGeneration || _gesture != Gesture.Down) return;
                _gesture = Gesture.Held;
                var (rx, ry) = _view.ToRemote(_touchStartX, _touchStartY);
                _view.Pointer = (rx, ry);
                _view.ShowHoldRing = true;
                PlatformService?.Haptics.Impact(HapticImpactStyle.Medium);
                Repaint();
            }), null, HoldDelay, Timeout.InfiniteTimeSpan);
    }

    private void TouchMove(float x, float y)
    {
        float moved = MathF.Sqrt((x - _touchStartX) * (x - _touchStartX) + (y - _touchStartY) * (y - _touchStartY));
        switch (_gesture)
        {
            case Gesture.Down when moved >= DragThreshold:
                CancelHold();
                _gesture = _view.CanPan ? Gesture.Pan : Gesture.Scroll;
                if (_gesture == Gesture.Scroll)
                {
                    var (sx, sy) = _view.ToRemote(_touchStartX, _touchStartY);
                    _view.Pointer = (sx, sy);
                    _client?.SendPointer((int)sx, (int)sy, 0);
                }
                break;
            case Gesture.Held when moved >= DragThreshold:
                // Hold, then move: press the left button where the finger went down and drag from there.
                _gesture = Gesture.Drag;
                _view.ShowHoldRing = false;
                var (dx, dy) = _view.ToRemote(_touchStartX, _touchStartY);
                _buttons = 1;
                SendPointer(dx, dy);
                break;
        }

        switch (_gesture)
        {
            case Gesture.Drag:
                var (rx, ry) = _view.ToRemote(x, y);
                SendPointer(rx, ry);
                // Near an edge of a zoomed picture, follow the finger.
                _view.Follow(rx, ry);
                break;
            case Gesture.Pan:
                _view.Pan(x - _touchLastX, y - _touchLastY);
                Repaint();
                break;
            case Gesture.Scroll:
                // Content follows the finger, as in a phone app: finger up = wheel down.
                _touchScroll += y - _touchLastY;
                while (_touchScroll <= -TouchScrollStep) { WheelPulse(16); _touchScroll += TouchScrollStep; }
                while (_touchScroll >= TouchScrollStep) { WheelPulse(8); _touchScroll -= TouchScrollStep; }
                break;
        }
        _touchLastX = x;
        _touchLastY = y;
    }

    private void TouchUp(float x, float y)
    {
        CancelHold();
        var gesture = _gesture;
        _gesture = Gesture.None;
        _view.ShowHoldRing = false;
        switch (gesture)
        {
            case Gesture.Down:
                Click(_touchStartX, _touchStartY, 1);
                break;
            case Gesture.Held:
                Click(_touchStartX, _touchStartY, 4);
                break;
            case Gesture.Drag:
                var (rx, ry) = _view.ToRemote(x, y);
                _buttons = 0;
                SendPointer(rx, ry);
                break;
        }
        Repaint();
    }

    private void Click(float viewX, float viewY, int button)
    {
        var (rx, ry) = _view.ToRemote(viewX, viewY);
        _buttons = 0;
        SendPointer(rx, ry);
        if (_view.Pointer is not { } p || _client is not { } client) return;
        client.SendPointer((int)p.X, (int)p.Y, button);
        client.SendPointer((int)p.X, (int)p.Y, 0);
    }

    private void CancelHold()
    {
        _holdGeneration++;
        _holdTimer?.Dispose();
        _holdTimer = null;
    }

    private void ResetGesture()
    {
        CancelHold();
        _gesture = Gesture.None;
        _view.ShowHoldRing = false;
    }

    // ---- keyboard ------------------------------------------------------------------

    private void OnScreenKeyDown(KeyboardEventArgs e)
    {
        if (!AcceptsInput || _client is not { } client) return;
        var code = e.Code;
        if (code == MikoKey.Unknown) return;
        e.StopPropagation();
        SyncModifiers(e);

        if (VncKeys.IsModifier(code))
        {
            e.PreventDefault();
            uint modifier = VncKeys.FromKey(code);
            if (_heldModifiers.Add(modifier)) client.SendKey(modifier, true);
            return;
        }

        bool stickyShortcut = HasStickyShortcut;
        bool shortcut = e.CtrlKey || e.AltKey || e.MetaKey || stickyShortcut;
        // A key that types: its text arrives next (layout and dead keys applied) and is sent from there.
        if (VncKeys.UsCharacter(code) != null && !shortcut) return;

        // Named keys, and shortcuts (the key's US character: Ctrl+C is Control + c on every layout).
        e.PreventDefault();
        uint keysym = VncKeys.FromKey(code, shift: false);
        if (keysym == 0) return;
        if (_pressedKeys.TryGetValue(code, out var previous) && previous != keysym) client.SendKey(previous, false);
        _pressedKeys[code] = keysym;
        client.SendKey(keysym, true);
        if (_sticky.Count > 0)
        {
            // A latched modifier applies to one key: release both now (the key's own release finds nothing).
            client.SendKey(keysym, false);
            _pressedKeys.Remove(code);
            // The platform still delivers the key's text (no modifier is physically held): that was this key.
            if (stickyShortcut && VncKeys.UsCharacter(code) != null) _swallowText = code;
            ReleaseSticky();
        }
    }

    // A printable key already sent as a shortcut (with a latched modifier): its text is not sent again.
    private MikoKey? _swallowText;

    private void OnScreenKeyUp(KeyboardEventArgs e)
    {
        if (_client is not { } client) return;
        var code = e.Code;
        if (_swallowText == code) _swallowText = null;
        if (VncKeys.IsModifier(code))
        {
            uint modifier = VncKeys.FromKey(code);
            if (_heldModifiers.Remove(modifier) && !_sticky.Contains(modifier)) client.SendKey(modifier, false);
            e.PreventDefault();
            return;
        }
        if (_pressedKeys.Remove(code, out var keysym))
        {
            client.SendKey(keysym, false);
            e.PreventDefault();
        }
    }

    // Typed text (hardware keyboard characters, the soft keyboard, IME results): each character a key press.
    private void OnScreenText(string text)
    {
        if (!AcceptsInput || _client is not { } client) return;
        // With Ctrl / Alt / ⌘ held the key was sent as a shortcut already (some platforms still deliver text).
        if (_heldModifiers.Overlaps(ShortcutModifiers)) return;
        if (_swallowText is { } swallowed)
        {
            _swallowText = null;
            var expected = VncKeys.UsCharacter(swallowed);
            if (expected != null && text.Length == 1 && char.ToLowerInvariant(text[0]) == expected) return;
        }
        foreach (var rune in text.EnumerateRunes())
        {
            uint keysym = VncKeys.FromChar(rune.Value);
            if (keysym == 0) continue;
            client.SendKey(keysym, true);
            client.SendKey(keysym, false);
        }
        ReleaseSticky();
    }

    private static readonly uint[] ShortcutModifiers =
        [VncKeys.ControlLeft, VncKeys.ControlRight, VncKeys.AltLeft, VncKeys.AltRight, VncKeys.SuperLeft, VncKeys.SuperRight];

    private bool HasStickyShortcut => _sticky.Overlaps(ShortcutModifiers);

    // A release the platform never delivered (focus moved while the key was down): let go of the modifier.
    private void SyncModifiers(KeyboardEventArgs e)
    {
        if (_client is not { } client) return;
        Release(e.ShiftKey, VncKeys.ShiftLeft, VncKeys.ShiftRight);
        Release(e.CtrlKey, VncKeys.ControlLeft, VncKeys.ControlRight);
        Release(e.AltKey, VncKeys.AltLeft, VncKeys.AltRight);
        Release(e.MetaKey, VncKeys.SuperLeft, VncKeys.SuperRight);

        void Release(bool down, uint left, uint right)
        {
            if (down) return;
            foreach (var k in new[] { left, right })
                if (!_sticky.Contains(k) && _heldModifiers.Remove(k)) client.SendKey(k, false);
        }
    }

    /// <summary>Latches a modifier from the keys bar (or lets go of it).</summary>
    internal void ToggleSticky(uint modifier)
    {
        if (_client is not { } client || !AcceptsInput) return;
        if (_sticky.Remove(modifier))
        {
            if (!_heldModifiers.Contains(modifier)) client.SendKey(modifier, false);
        }
        else
        {
            _sticky.Add(modifier);
            if (!_heldModifiers.Contains(modifier)) client.SendKey(modifier, true);
        }
        UpdateKeysBar();
    }

    /// <summary>A key from the keys bar: press and release, with the latched modifiers.</summary>
    internal void TapKey(uint keysym)
    {
        if (_client is not { } client || !AcceptsInput) return;
        client.SendKey(keysym, true);
        client.SendKey(keysym, false);
        ReleaseSticky();
    }

    private void ReleaseSticky()
    {
        if (_sticky.Count == 0) return;
        foreach (var k in _sticky)
            if (!_heldModifiers.Contains(k)) _client?.SendKey(k, false);
        _sticky.Clear();
        UpdateKeysBar();
    }

    // Lets go of everything pressed: focus left the remote screen, the session ends, view-only.
    private void ReleaseAllKeys()
    {
        if (_client is { } client)
        {
            foreach (var keysym in _pressedKeys.Values) client.SendKey(keysym, false);
            foreach (var k in _heldModifiers) client.SendKey(k, false);
            foreach (var k in _sticky) if (!_heldModifiers.Contains(k)) client.SendKey(k, false);
        }
        _pressedKeys.Clear();
        _heldModifiers.Clear();
        _sticky.Clear();
        UpdateKeysBar();
    }

    // ---- focus ---------------------------------------------------------------------

    /// <summary>Gives the remote screen the keyboard (on a phone, brings up the soft keyboard) or takes it away.</summary>
    public void ToggleKeyboard() => SetKeyboard(!IsKeyboardActive);

    private void SetKeyboard(bool active, bool showSoftKeyboard = true)
    {
        if (active && !AcceptsInput) return;
        IsKeyboardActive = active;
        if (Input is { } input)
        {
            if (active)
            {
                input.SetFocus(_screen);
                if (showSoftKeyboard) input.RequestInputMethodShow();
            }
            else if (_screen.HasState(Core.ElementState.Focus))
            {
                input.SetFocus(null);
            }
        }
        ApplyState();
        Repaint();
    }

    private void OnScreenBlur()
    {
        ReleaseAllKeys();
        if (!IsKeyboardActive) return;
        IsKeyboardActive = false;
        ApplyState();
    }
}
