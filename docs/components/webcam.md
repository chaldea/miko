# Network Camera (Webcam)

`MikoWebcam` (in `Miko.Components`) shows a network camera the way the vendors' apps do: the live
picture on top with a toolbar under it, a row of actions, and a pan / tilt disk below.

- **Toolbar:** play / stop, stream quality (SD / HD), received bitrate, fullscreen.
- **Actions:** snapshot, privacy mode, flip the picture, fullscreen. Actions the camera lacks are hidden.
- **Pan / tilt:** hold an arrow to turn the camera, release to stop. A camera that has pan and tilt but
  cannot take commands right now (offline in the cloud) shows the disk disabled, with the reason.
- **Fullscreen:** the picture fills the screen in landscape with the system bars hidden. The toolbar and a
  translucent pan / tilt disk sit over it, and a tap on the picture hides or shows them. Back leaves fullscreen.

The camera behind the view is an `IWebcamProvider`. The built-in one is **Ezviz** (萤石, `open.ys7.com`).
Register your own provider to support another vendor or a private protocol.

## Setup

```csharp
using Miko.Components.Webcam;

var builder = MikoAppBuilder.CreateDefault();
builder.Services.AddMikoWebcam(options =>
{
    options.AppKey = "<Open Platform app key>";
    options.AppSecret = "<app secret>";       // or: options.AccessToken = "<token from your server>";
});
builder.UseSystemVideo();      // desktop; Android: UseAndroidVideo(); iOS: UseIosVideo()
builder.UseAndroidNative();    // phones: fullscreen landscape, hidden system bars, haptics. iOS: UseIosNative()
```

```razor
@using Miko.Components.Webcam

<MikoWebcam Device="@_camera" Texts="@WebcamTexts.Chinese" Style="@(new Style { FlexGrow = 1 })" />

@code {
    // The verification code is printed on the device label: it is the camera's local password.
    private readonly WebcamDevice _camera = WebcamDevice.ParseEzopen("ezopen://ABCDEF@open.ys7.com/AB1234567/1.live", out _);
}
```

`WebcamDevice` can also be built directly: `new WebcamDevice("AB1234567", Channel: 1) { VerifyCode = "ABCDEF" }`.
Set `LocalAddress` (`"192.168.1.20"` or `"192.168.1.20:554"`) when the cloud does not know the camera's address.

Do not ship the app secret in a client app. Issue access tokens on your server
(`/api/lapp/token/get`) and set `AccessToken` instead.

## How the picture arrives

The Ezviz SDKs stream over Ezviz's private protocol (LAN direct → P2P → cloud relay), and that protocol
lives in closed native libraries. Miko does not bundle them. The cameras also speak standard **RTSP** on the
local network, with user `admin` and the verification code as the password. That is what the provider uses:

| Where the camera is | Stream | Cost |
| --- | --- | --- |
| Same network as the app | `rtsp://admin:<code>@<camera ip>/h264/ch<n>/<main\|sub>/av_stream`, straight from the camera | None |
| Elsewhere, with `EnableCloudRelay = true` | The cloud's HLS relay (`/api/lapp/v2/live/address/get`) | Counts against the account's paid traffic and viewer limits |
| Elsewhere, relay off (default) | None: the view says the camera is not on this network | — |

The cloud reports the camera's LAN address even when the camera is offline in the cloud. A camera without
internet access therefore still streams, but it cannot be steered: Ezviz takes pan / tilt commands only
through the cloud. Snapshots, privacy mode and flipping go through the cloud too.

RTSP playback is not Ezviz-specific. With `AddMikoRtsp()` (included in `AddMikoWebcam()`), any
`<video src="rtsp://user:password@host/path">` plays H.264 or H.265 over RTSP:

| Platform | Decoder | Notes |
| --- | --- | --- |
| macOS | VideoToolbox | Zero-copy into the Metal window |
| iOS | VideoToolbox | Zero-copy into Metal. Add `NSLocalNetworkUsageDescription` to `Info.plist`, or iOS blocks LAN access |
| Android | MediaCodec | Zero-copy into the OES texture. Needs the `INTERNET` permission |
| Windows, Linux | — | Not yet: the view shows "cannot decode live streams" |

The RTSP client is managed code: TCP interleaved (no UDP, so no NAT or firewall trouble), Digest and Basic
authentication, `rtsps://` over TLS. A dropped connection is retried five times with a growing delay before
the view shows an error. A wrong password or a missing stream fails at once. Audio is not played.

## Parameters

| Parameter | Type | Default | |
| --- | --- | --- | --- |
| `Device` | `WebcamDevice?` | `null` | The camera. Changing it reconnects. |
| `Quality` | `WebcamQuality` | `Smooth` | Starting quality; the quality button switches it (`QualityChanged`). |
| `AutoPlay` | `bool` | `true` | Connect as soon as the stream is resolved. |
| `PtzSpeed` | `PtzSpeed` | `Normal` | Turning speed of the disk. |
| `ShowPtz` / `ShowActions` | `bool` | `true` | Show the pan / tilt disk / the action row. |
| `Texts` | `WebcamTexts` | English | `WebcamTexts.Chinese` is built in; `with { … }` changes single texts. |
| `SnapshotTaken` | `EventCallback<WebcamSnapshot>` | | Every snapshot taken with the action. |
| `InfoLoaded` | `EventCallback<WebcamInfo>` | | The device as the provider reported it (name, features). |
| `Error` | `EventCallback<Exception>` | | Provider and stream failures (also shown in the view). |
| `Class`, `Style` | | | On the root. A `Height` in `Style` keeps the picture 16:9 and gives the rest to the panel. |

Methods (UI thread): `Play()`, `Stop()`, `Retry()`, `SetQualityAsync(…)`, `CaptureAsync()`,
`SetPrivacyAsync(bool)`, `MirrorAsync()`, `StartPtz(direction)` / `StopPtz()`, `ToggleFullscreen()`.
State: `State` (`Connecting`, `Playing`, `Stopped`, `Error`), `ErrorMessage`, `Info`, `Stream`,
`CurrentQuality`, `IsPrivacyEnabled`, `IsFullscreen`, `BytesPerSecond`, `ActivePtz`, `Hint`.

## Ezviz options

| Option | Default | |
| --- | --- | --- |
| `ApiBaseUrl` | `https://open.ys7.com` | `https://open.ezvizlife.com` outside China. |
| `AppKey`, `AppSecret` | | Tokens are issued and renewed automatically (on expiry and on error `10002`). |
| `AccessToken` | | A fixed token, used as is. |
| `PreferLocalNetwork` | `true` | Stream straight from the camera when it is reachable. |
| `EnableCloudRelay` | `false` | Fall back to the paid HLS relay. |
| `LocalUser`, `RtspPort` | `admin`, `554` | The camera's local login. |
| `LocalProbeTimeout` | 1.5 s | How long to try the camera's LAN address. |

`EzvizWebcamProvider.Api` calls Open Platform endpoints the provider does not wrap.

## Your own provider

```csharp
public sealed class MyCameraProvider : IWebcamProvider
{
    public Task<WebcamStream> GetLiveStreamAsync(WebcamDevice device, WebcamQuality quality, CancellationToken ct = default) =>
        Task.FromResult(new WebcamStream($"rtsp://admin:{device.VerifyCode}@{device.LocalAddress}/stream1",
            null, WebcamTransport.LocalNetwork, quality));
    // GetInfoAsync, StartPtzAsync / StopPtzAsync, CaptureAsync, SetPrivacyAsync, MirrorAsync …
}

builder.Services.AddSingleton<IWebcamProvider, MyCameraProvider>();   // before or after AddMikoWebcam()
builder.Services.AddMikoWebcam();
```

A feature the camera lacks throws `NotSupportedException`; leave it out of `WebcamInfo.Features` and the
view hides the action. Vendor errors are `WebcamException`s with the vendor's code. `WebcamTexts.Describe`
turns known codes into texts.

A stream can be any `<video>` source: RTSP, HLS, a file. A **private protocol** registers an
`IEncodedVideoSourceFactory` for its own scheme and returns that scheme from the provider. The factory opens
an `IEncodedVideoSource` that delivers H.264 / H.265 access units (Annex-B), and the platform decoder does
the rest. No player and no bundled decoder are needed:

```csharp
builder.Services.AddSingleton<IEncodedVideoSourceFactory, MyProtocolSourceFactory>();  // "myproto://…"
```

Throw a `VideoStreamException` with `isPermanent: true` for failures that reconnecting cannot fix (wrong
credentials). Other exceptions are retried.
