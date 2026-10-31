# Miko.Components

Native components for Miko. The player requires a platform video backend; it does not depend on Ionic, Bootstrap, a browser, or FFmpeg.

```csharp
using Miko.Components.Player;

builder.Services.AddMikoPlayer();
builder.UseSystemVideo(); // Desktop. Android: UseAndroidVideo(); iOS: UseIosVideo().
builder.UseAndroidNative(); // Phones: fullscreen locks landscape and hides the system bars; haptics. iOS: UseIosNative().
```

```razor
@using Miko.Components.Player
@using Miko.Components.Player.Danmaku

<MikoPlayer Source="https://example.com/video.m3u8" AutoPlay="true"
            Danmaku="@comments" DanmakuSettings="@settings" />

@code {
    private IReadOnlyList<DanmakuItem> comments = [
        new(TimeSpan.FromSeconds(1), "Hello"),
        new(TimeSpan.FromSeconds(2), "Pinned", DanmakuMode.Top)
    ];
    private DanmakuSettings settings = new() { FontSize = 22, Opacity = .9f, MaxVisible = 80 };
}
```

The library is written as C# components, so consumers use the existing Miko Razor compiler without requiring an additional compiler in this package.

## Playback

Parameters: `Source`, `MimeType`, `Poster`, `AutoPlay`, `Loop`, `Muted`, `Volume`, `PlaybackRate`, `Class`, `Style`, `Danmaku`, `DanmakuSettings`, `LongPressPlaybackRate` (default 2; 0 disables the long press) and `ControlsHideDelay` (default 5 s; zero keeps the controls up).

Instance methods: `Play()`, `Pause()`, `Seek(TimeSpan)`, `SetVolume(float)`, `SetMuted(bool)`, `SetPlaybackRate(float)`, `SetDanmakuSettings(...)`, `ShowControls()`, `HideControls()`, `ToggleSettings()`, `ToggleFullscreen()`, and `Retry()`; state: `IsFullscreen`, `IsSettingsOpen`, `AreControlsVisible`, `IsLongPressActive`. `Controller` exposes playback snapshots and a `Changed` event. Call methods on the UI thread. The engine owns and disposes video sessions; disposing a controller does not dispose its backend.

The player is the picture: 16:9 of its width (or the height its `Style` sets), with the controls drawn over it on a gradient — play/pause, seeking, elapsed/total time, mute, danmaku, settings, and fullscreen. During playback they fade out after `ControlsHideDelay`; pausing brings them back.

| Gesture | Touch | Mouse |
| --- | --- | --- |
| Tap / click on the picture | Shows or hides the controls (after the 300 ms double-tap window) | Plays or pauses |
| Double tap | Plays or pauses | — |
| Long press | Plays at `LongPressPlaybackRate` until released, with a "» 2x" hint and a light haptic tick | Same |
| Mouse move | — | Shows the controls |

Settings — speed, loop, volume, danmaku on/off, opacity, text size, travel time, area and density — open over the picture: as a translucent drawer on the right on Android and iOS (touch-sized rows; the controls make way), as a pop-up above the settings button on the desktop. A tap outside, Back or Escape closes them.

Fullscreen covers the whole screen. With the platform's native services registered (`UseAndroidNative()` / `UseIosNative()`), it locks landscape, keeps the screen on and hides the system bars — Android hides the status and navigation bars and lets the picture run under the camera cutout, iOS hides the status bar and auto-hides the home indicator — and the controls keep clear of the cutout through the safe area. Back leaves fullscreen. On the desktop it fills the window without changing the OS window mode. Unknown-duration live streams show `LIVE` and disable duration-based seeking. Buffered ranges are displayed only when the backend reports them. `Poster` currently follows the core video element's local-file support.

## Platform Backends

| Platform | System Pipeline | Notes |
| --- | --- | --- |
| Windows | Media Foundation Media Engine + D3D11 | HLS, audio, buffering, seeking and rate control; managed Vortice bindings, no bundled native decoder |
| Android | MediaPlayer + SurfaceTexture | HLS, audio, buffering; playback rate requires API 23 or newer |
| macOS / iOS | AVPlayer | Native HLS and audio; exposes buffering state |
| Linux | GStreamer playbin + appsink | Audio and HLS require installed GStreamer plugins and an audio sink |

`Miko.Video.FFmpeg` remains an explicitly selected optional extension with its existing video-only limitations. `AddMikoPlayer()` does not install or replace a decoder.

## Danmaku

`Scroll`, `Top`, and `Bottom` comments use media time, so pause, rate changes, seeks and loops stay synchronized. Supply a new immutable list when replacing comments; `DanmakuItem.Color` is a `Miko.Common.Color` (its alpha is replaced by the opacity setting). Rendering uses one canvas element, an indexed timeline, bounded visible entries and a 512-entry text cache. Comments are laid out with the engine's text pipeline, so mixed scripts (CJK with kana or Latin) get their fallback fonts. Default density is 80, configurable with `AddMikoPlayer(o => o.MaxVisibleDanmaku = 120)`. Congested comments are dropped rather than queued indefinitely. Seeking scans at most 2,048 recent candidates. The steady-state timeline loop allocates no managed memory per frame.

Icons are selected Ionicons assets already present in the repository (MIT); they are embedded at build time without a runtime dependency on `Miko.Ionic`.

The player draws through `Miko.Graphics` (`IDrawingContext`, `IGraphicsFactory`) and does not reference SkiaSharp; see [Custom Drawing](../../docs/engine/drawing.md).

See [design](Player/DESIGN.md) and [examples](../../examples/Components/README.md).
