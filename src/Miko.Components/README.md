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

## Map

`MikoMap` draws a north-up Web Mercator map, markers and attribution on a Miko canvas. Drag to pan, use the wheel to zoom around the pointer, or tap the zoom buttons.

```csharp
using Miko.Components.Map;

builder.Services.AddMikoMap(); // Miko.MockServer at http://127.0.0.1:8080
// To use the AMap static map Web Service, register builder.Services.AddAMap(webServiceKey).
```

```razor
@using Miko.Components.Map

<MikoMap @bind-Camera="_camera" Markers="@_places" />

@code {
    private MapCamera _camera = MapCamera.Beijing;
    private readonly MapMarker[] _places = [new(MapCamera.Beijing.Center, "Palace Museum")];
}
```

Coordinates are longitude then latitude in the provider’s datum. AMap expects GCJ-02. Camera zoom uses 256-pixel XYZ levels 3–18. The engine keeps the last frame under the markers while a pan, zoom or resize loads a replacement. A viewport larger than 1024 pixels uses a lower-resolution frame covering the same area. At the minimum zoom, up to four adjacent images cover the world. Tap a failed map to retry, or inspect `MikoMap.Error` and call `Retry()`.

An `IMapProvider` supplies attribution and an encoded image for the center, zoom, width and height in `MapImageRequest`. Register a custom provider in DI or pass a server base address to `AddMikoMap(baseUrl)`. The [map example](../../examples/Components/README.md#map-example) supports both providers on Desktop, Android, iOS and Miko Simulator.

## Network Camera

`MikoWebcam` shows a network camera: the live picture with a toolbar (play / stop, quality, bitrate, fullscreen), snapshot / privacy / flip actions, and a pan / tilt disk. The camera is an `IWebcamProvider`; Ezviz (萤石) is built in.

```csharp
using Miko.Components.Webcam;

builder.Services.AddMikoWebcam(o => { o.AppKey = "…"; o.AppSecret = "…"; });   // or o.AccessToken
```

```razor
<MikoWebcam Device="@(WebcamDevice.ParseEzopen("ezopen://VERIFYCODE@open.ys7.com/SERIAL/1.live", out _))" Texts="@WebcamTexts.Chinese" />
```

The picture streams straight from the camera on the local network over RTSP (user `admin`, the verification code as password) and is decoded by the platform (VideoToolbox on macOS / iOS, MediaCodec on Android), with no Ezviz native SDK. The paid cloud HLS relay is used only with `EnableCloudRelay`. `AddMikoRtsp()` makes any `<video src="rtsp://…">` play. See [Network Camera](../../docs/components/webcam.md) and [design](Webcam/DESIGN.md).

## Remote Desktop (VNC)

`MikoVnc` is a VNC client: the remote screen with a collapsible toolbar (keyboard, keys bar with Esc / Tab / Ctrl / Alt / Win / F-keys / Ctrl+Alt+Del, zoom, settings, fullscreen), a password dialog when the server asks, and a connect form when no host is given.

```razor
<MikoVnc Host="192.168.1.20" Texts="@VncTexts.Chinese" />
```

The protocol is managed code (`VncClient`): RFB 3.3–3.8; None, VNC and Tight security; Raw, CopyRect, RRE, Hextile, Zlib, ZRLE and Tight with JPEG. zlib and DES are managed too, with no native package. See [Remote Desktop](../../docs/components/vnc.md) and [design](Vnc/DESIGN.md).

## Book and Comic Readers

`BookReader` and `ComicReader` share lazy directory/page loading, navigation, cancellation and a bounded cache. `FileReaderSource` reads TXT, EPUB, PDF, CBZ and CBR; `IReaderSource` and `DelegateReaderSource` also support separate remote directory/content APIs.

```razor
@using Miko.Components.Reader

<BookReader Source="@book" FontSize="22" Theme="ReaderTheme.Sepia" />
<ComicReader Source="@comic" PageMode="ComicPageMode.Double" Direction="ReaderDirection.RightToLeft" />
```

Books support typography, page margins, themes, eye care, measured pagination and scrolling. Comics support single/double pages, both reading directions and axes, fit-width, double-tap zoom and dragging. Both start with immersive mobile layouts: tap to reveal navigation/progress and open directory, appearance or settings sheets. Controls respect safe areas and do not resize the reading surface. `Immersive="false"` shows controls initially; `BackRequested` connects the reader to application navigation, and `Dimness` shades content without changing system brightness. The application owns and disposes sources; components own their loading controllers and decoded images.

The [reader example](../../examples/Components/MikoApp.Reader/README.md) provides a shared bookshelf with Desktop, Android and iOS launch projects. Selecting an ebook or comic opens the matching reader with built-in settings; returning to the shelf retains its settings and chapter/page index for the session.

EPUB currently reflows chapter text without publication CSS or illustrations. PDFs preserve original pages by default, including scans; `ReaderFileOptions.ReflowPdf` enables extracted text and adjustable typography, without OCR. See [readers](../../docs/Miko.Docs.Web/Content/components/reader.md), [design](Reader/DESIGN.md) and [examples](../../examples/Components/README.md).

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
