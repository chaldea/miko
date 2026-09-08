# Miko.Components

Native components for Miko. The player requires a platform video backend; it does not depend on Ionic, Bootstrap, a browser, or FFmpeg.

```csharp
using Miko.Components.Player;

builder.Services.AddMikoPlayer();
builder.UseSystemVideo(); // Desktop. Android: UseAndroidVideo(); iOS: UseIosVideo().
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

Parameters: `Source`, `MimeType`, `Poster`, `AutoPlay`, `Loop`, `Muted`, `Volume`, `PlaybackRate`, `Class`, `Style`, `Danmaku`, and `DanmakuSettings`.

Instance methods: `Play()`, `Pause()`, `Seek(TimeSpan)`, `SetVolume(float)`, `SetMuted(bool)`, `SetPlaybackRate(float)`, `SetDanmakuSettings(...)`, `ToggleSettings()`, `ToggleFullscreen()`, and `Retry()`. `Controller` exposes playback snapshots and a `Changed` event. Call methods on the UI thread. The engine owns and disposes video sessions; disposing a controller does not dispose its backend.

The control bar includes play/pause, seeking, elapsed/total time, mute, danmaku, settings, and fullscreen. Settings include volume, speed, loop, opacity, text size, travel time, area, and density. Fullscreen fills the host viewport and restores the original layout when closed; it does not change the OS window mode or system bars. Unknown-duration live streams show `LIVE` and disable duration-based seeking. Buffered ranges are displayed only when the backend reports them. `Poster` currently follows the core video element's local-file support.

## Platform Backends

| Platform | System Pipeline | Notes |
| --- | --- | --- |
| Windows | Media Foundation Media Engine + D3D11 | HLS, audio, buffering, seeking and rate control; managed Vortice bindings, no bundled native decoder |
| Android | MediaPlayer + SurfaceTexture | HLS, audio, buffering; playback rate requires API 23 or newer |
| macOS / iOS | AVPlayer | Native HLS and audio; exposes buffering state |
| Linux | GStreamer playbin + appsink | Audio and HLS require installed GStreamer plugins and an audio sink |

`Miko.Video.FFmpeg` remains an explicitly selected optional extension with its existing video-only limitations. `AddMikoPlayer()` does not install or replace a decoder.

## Danmaku

`Scroll`, `Top`, and `Bottom` comments use media time, so pause, rate changes, seeks and loops stay synchronized. Supply a new immutable list when replacing comments. Rendering uses one canvas element, an indexed timeline, bounded visible entries and a 512-entry text cache. Default density is 80, configurable with `AddMikoPlayer(o => o.MaxVisibleDanmaku = 120)`. Congested comments are dropped rather than queued indefinitely. Seeking scans at most 2,048 recent candidates. The steady-state timeline loop allocates no managed memory per frame.

Icons are selected Ionicons assets already present in the repository (MIT); they are embedded at build time without a runtime dependency on `Miko.Ionic`.

See [design](Player/DESIGN.md) and [examples](../../examples/Components/README.md).
