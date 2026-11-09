# Remote Desktop (VNC)

`MikoVnc` (in `Miko.Components`) is a VNC client: the remote screen fills the component, and a toolbar
floats over its top edge. It works with the usual servers (TightVNC, TigerVNC, RealVNC, UltraVNC,
x11vnc, libvncserver, macOS Screen Sharing with a VNC password) on every Miko platform. The protocol is
managed code, with no native library and no NuGet dependency.

- **Toolbar:** desktop name, resolution and bitrate. Buttons for the soft keyboard (phones), the keys bar,
  zoom out / in, settings, fullscreen, disconnect, and collapse. Collapsed, it leaves a small tab at the
  top edge.
- **Keys bar:** the keys a phone keyboard lacks: Esc, Tab, Ctrl / Alt / Shift / Win (they latch until the
  next key), arrows, Del, Home / End, PgUp / PgDn, Ctrl+Alt+Del and F1–F12.
- **Settings:** picture quality (lossless, high, balanced, low), fit or 1:1, view only, refresh.
- **Password:** when the server needs one and `Password` is not set, a dialog asks for it. A wrong
  password asks again, with the reason.
- **No host:** without `Host` the component shows a connect form (address, password, quality).

## Setup

No service registration is needed: reference `Miko.Components` and place the component.

```razor
@using Miko.Components.Vnc

<MikoVnc Host="192.168.1.20" Texts="@VncTexts.Chinese" Style="@(new Style { Width = Length.Percent(100), Height = Length.Percent(100) })" />
```

`Port` defaults to 5900; display `n` is port `5900 + n`. The connect form and `VncAddress.TryParse`
accept `host`, `host:display`, `host::port` and `[ipv6]:port`.

On phones, register the native services so that fullscreen can turn the device to landscape and hide the
system bars, and so a held touch gives haptic feedback:

```csharp
builder.UseAndroidNative();   // iOS: UseIosNative(); desktop: UseDesktopNative()
```

An Android activity should declare `WindowSoftInputMode = SoftInput.AdjustResize` and
`ConfigurationChanges` (orientation, screen size, keyboard) so that the keyboard and rotation do not
recreate it. iOS needs `NSLocalNetworkUsageDescription` in `Info.plist` for servers on the LAN.

## Input

| Input | On the remote desktop |
| --- | --- |
| Mouse | One to one: left, middle and right buttons, the wheel (each notch is a button 4 / 5 click; horizontal 6 / 7). |
| Touch tap | Left click where the finger is. Two taps make a double click. |
| Touch hold (½ s) | Right click, released where it was held (a ring shows the hold). |
| Hold, then drag | Left-button drag from where the finger went down (select text, move windows). |
| Drag | Pans the picture when zoomed in. When the whole screen fits, it scrolls (the wheel). |
| Keyboard | Keys and typed text go to the remote screen while it has focus. Click it (desktop), or use the keyboard button (phones). |

The text a key types is sent as typed, so the local layout applies, and so do dead keys and the IME
(Chinese input arrives as Unicode keysyms). Shortcuts are sent by key: with Ctrl, Alt or ⌘ / Win held,
the key goes out under its US-layout name, so Ctrl+C is Control + `c` on every layout. On the desktop,
Escape goes to the remote side even in fullscreen; leave fullscreen with the toolbar button.

The remote pointer is drawn locally at the mouse or finger position, in the shape the server sends. With a
mouse the system cursor is hidden over the screen (`cursor: none`), so there is no lag and only one cursor.

The core events this relies on can be used by any component: right / middle buttons reach
`OnPointerDown` / `OnPointerUp` with `Button` set, and they never produce a click. `OnWheel` (`@onwheel`)
can `PreventDefault()` the scroll. `OnKeyUp` (`@onkeyup`) and `KeyboardEventArgs.Code` give the physical
key. A `pointerdown` handler that calls `PreventDefault()` keeps focus where it is, as in a browser.

## Parameters

| Parameter | Type | Default | |
| --- | --- | --- | --- |
| `Host` | `string?` | `null` | Server host or IP. `null` shows the connect form. Changing it reconnects. |
| `Port` | `int` | `5900` | TCP port. |
| `Password` | `string?` | `null` | The VNC password. When `null`, the dialog asks if the server needs one. |
| `AutoConnect` | `bool` | `true` | Connect when shown; otherwise a button waits. |
| `ViewOnly` | `bool` | `false` | Watch without sending input (`ViewOnlyChanged`). |
| `Quality` | `VncQuality` | `High` | `Lossless`, `High`, `Balanced`, `Low`; the settings change it live (`QualityChanged`). |
| `Scaling` | `VncScaling` | `Fit` | `Fit` (the whole screen) or `Actual` (1:1, following the pointer). |
| `Shared` | `bool` | `true` | Let other viewers stay connected. |
| `ShowToolbar` | `bool` | `true` | |
| `Texts` | `VncTexts` | English | `VncTexts.Chinese` is built in; `with { … }` changes single texts. |
| `Connected` | `EventCallback<VncDesktopInfo>` | | Name, size, protocol and security of the remote desktop. |
| `Disconnected` | `EventCallback<Exception?>` | | The failure, or `null` when the session ended cleanly. |
| `ClipboardReceived` | `EventCallback<string>` | | Text the remote side copied. |
| `Error` | `EventCallback<Exception>` | | Connection and protocol failures (also shown in the view). |
| `Class`, `Style` | | | On the root. The component fills its parent. |

Methods (UI thread): `Connect()`, `Connect(host, port, password)`, `Disconnect()`, `SendText(text)`,
`SendKeys(keysyms…)` (pressed in order, released in reverse, e.g. `SendKeys(VncKeys.ControlLeft, 'c')`),
`SendCtrlAltDel()`, `SendClipboard(text)`, `SetQuality(…)`, `SetViewOnly(…)`, `ZoomIn()`, `ZoomOut()`,
`FitToScreen()`, `ActualSize()`, `ToggleFullscreen()`, `ToggleKeyboard()`, `RefreshScreen()`.
State: `State` (`Idle`, `Connecting`, `Authenticating`, `Connected`, `Disconnected`), `ErrorMessage`,
`Desktop`, `Framebuffer`, `Client`, `CurrentQuality`, `IsViewOnly`, `IsFullscreen`, `IsKeyboardActive`,
`Zoom`, `BytesPerSecond`.

### Clipboard

The component does not touch the device clipboard by itself. Connect the two directions with
`Miko.Native`'s `IClipboardService`:

```razor
<MikoVnc @ref="_vnc" Host="…" ClipboardReceived="@(text => Clipboard.WriteAsync(new() { String = text }))" />

@code {
    [Inject] IClipboardService Clipboard { get; set; } = null!;
    private MikoVnc? _vnc;

    private async Task PasteToRemote() => _vnc!.SendClipboard((await Clipboard.ReadAsync()).Value);
}
```

Standard RFB clipboard text is Latin-1. Characters outside Latin-1 become `?` (type them with `SendText`
instead).

## Protocol

| | Supported |
| --- | --- |
| Versions | RFB 3.3, 3.7, 3.8 (3.889 and 4.x servers speak 3.8) |
| Security | None, VNC authentication (DES challenge), Tight (with None / VNC inside) |
| Encodings | Raw, CopyRect, RRE, Hextile, Zlib, ZRLE, Tight (fill, copy / palette / gradient filters, JPEG) |
| Pseudo-encodings | Cursor (the pointer shape), DesktopSize, ExtendedDesktopSize, LastRect, DesktopName, compression and JPEG quality levels |
| Pixel format | 32-bit true colour, requested from the server whatever its own |

`Quality` picks the encodings: `Lossless` prefers ZRLE and never asks for JPEG. The other levels prefer
Tight, with JPEG for photographic areas (quality 8, 6 and 3) and more compression as they go down. Tight's
JPEG is decoded by the engine's image decoder. zlib (Zlib, ZRLE and Tight's four streams) and DES are
managed implementations, the same on every platform.

VNC authentication is not encryption: the password challenge is DES, and the session is plain TCP. Use it
on a trusted network, or tunnel it (SSH, VPN). VeNCrypt / TLS, RealVNC's RA2 and Apple's Diffie-Hellman
security are not supported; a server that offers only those is reported by name.

### Without the view

`VncClient` is public, for tools and tests:

```csharp
using var client = await VncClient.ConnectAsync(new VncConnectOptions("192.168.1.20") { Password = "…" });
client.FramebufferUpdated += () => { /* client.Framebuffer.Pixels: B, G, R, X rows */ };
client.Start();
client.SendPointer(100, 100, buttons: 1);
client.SendKey(VncKeys.Return, down: true);
client.SendKey(VncKeys.Return, down: false);
```

Events are raised on the connection's receive thread. Input methods never block, and pointer moves with
the same buttons coalesce.

## Limitations

- Only one finger is tracked, so there is no pinch zoom or two-finger scroll. Zoom with the toolbar, and
  scroll by dragging a picture that fits.
- A Windows server's lock screen turns its display off after a while, and many servers then capture a
  black screen. Moving the pointer (any touch or mouse move) wakes it.
- On Windows keyboards, AltGr arrives as Ctrl+Alt, so AltGr characters are sent as shortcuts. Type them
  through `SendText`, or use the remote side's layout.
- iOS hardware keyboards type text, but their arrows and modifiers do not reach the view yet.
