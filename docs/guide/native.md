# Native Capabilities

`Miko.Native` defines cross-platform interfaces for device capabilities — camera, clipboard,
filesystem, geolocation, notifications, and more. Your app depends only on the interfaces;
each platform host supplies the implementation.

The interfaces follow the [Ionic Capacitor v8](https://ionicframework.com/docs/v8/native) API
surface, mapped to C#: `Promise<T>` becomes `Task<T>`, `number` becomes `double`, `any` becomes
`object?`, `Date` becomes `DateTimeOffset`, and JavaScript listeners become C# `event`s.

## Setup

Register the interfaces once in your shared app assembly:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Miko.Native;

var builder = MikoAppBuilder.CreateDefault();
builder.Services.AddMikoNative();
```

Then have each platform head install its implementations:

```csharp
// Desktop head (Miko.Windowing)
App.CreateContext(b => b.UseDesktopNative()).RunDesktop();

// Simulator head (Miko.Simulator)
App.CreateContext(b => b.UseSimulatorNative()).RunSimulator();

// Android Activity (Miko.Android)
MikoAndroidApp.CreateView(this, () => App.CreateContext(b => b.UseAndroidNative()));

// iOS AppDelegate (Miko.iOS)
new MikoViewController(App.CreateContext(b => b.UseIosNative()));
```

Order does not matter. `AddMikoNative()` registers defaults with `TryAdd` and the platform
packages override with `Replace`, so the real implementation always wins whether you call it
before or after.

## Using a capability

Inject the interface into any component:

```razor
@using Miko.Native.Camera

@code {
    [Inject] public ICameraService CameraService { get; set; } = default!;

    private async Task TakePhotoAsync()
    {
        var result = await CameraService.TakePhotoAsync(new TakePhotoOptions
        {
            Quality = 90,
            SaveToGallery = true,
        });

        _imagePath = result.WebPath;
    }
}
```

## When a capability is unavailable

Injection always succeeds — every interface has a default registration. Calling a capability the
current platform does not implement throws `PlatformNotSupportedException` rather than returning
empty or fabricated data, so a missing capability cannot be mistaken for a real answer:

```csharp
try
{
    var photo = await CameraService.TakePhotoAsync(new TakePhotoOptions());
}
catch (PlatformNotSupportedException)
{
    // No camera stack on this platform — fall back to a file picker.
}
```

Permission denials surface separately as `UnauthorizedAccessException`, and user cancellation as
`OperationCanceledException`.

## Capability support

| Interface | Android | iOS | Desktop | Simulator |
| --- | :-: | :-: | :-: | :-: |
| `IAppService` | ● | ● | ◐ | ◐ |
| `IBrowserService` | ● | ● | ◐ | ◐ |
| `ICameraService` | ● | ● | — | ◑ |
| `IClipboardService` | ● | ● | ◐ | ◐ |
| `IDeviceService` | ● | ● | ● | ◑ |
| `IFilesystemService` | ● | ● | ● | ● |
| `IGeolocationService` | ● | ● | — | ◑ |
| `IHapticsService` | ● | ● | — | ◑ |
| `IKeyboardService` | ◐ | ◐ | ◐ | ◐ |
| `ILocalNotificationService` | ◐ | ● | — | ◑ |
| `IMotionService` | ● | ● | — | — |
| `INetworkService` | ● | ● | ● | ● |
| `IPushNotificationService` | ◐ | ◐ | — | ◑ |
| `IScreenReaderService` | ● | ● | ◐ | ◑ |
| `ISplashScreenService` | — | — | — | ◑ |
| `IStatusBarService` | ● | ● | — | ◑ |
| `IToastService` | ● | ● | — | ◑ |

● full · ◐ partial (some members throw) · ◑ simulated · — not implemented

Two notes on the desktop column: `IClipboardService` and `IScreenReaderService` are Windows-only
(Linux and macOS have no dependency-free system stack, so they throw), and capabilities marked
partial are ones where the platform genuinely lacks the concept — for example `ExitAppAsync()` is
Android-only, and the iOS-specific keyboard members target a WebView that Miko does not use.

## Simulator behaviour

The simulator answers mobile-only capabilities with recognisable sample data and logs every call,
so you can exercise the whole chain — tap button, call camera, render result — on the desktop
instead of deferring it to a device. Sample URIs are prefixed `miko-simulator://` and
`IDeviceService` reports `IsVirtual = true`, so simulated values can never be mistaken for real
device data. `IDeviceService` tracks the device you pick in the simulator panel, which makes it
useful for checking platform-dependent UI.

Capabilities that already work on a desktop — filesystem, clipboard, network, browser — use the
real desktop implementations rather than simulated ones.

## Platform configuration

Permissions, manifests, and notification setup remain the app's responsibility; the interfaces
expose only permission state and business parameters.

- **Android** — declare permissions in `AndroidManifest.xml` (`CAMERA`, `ACCESS_FINE_LOCATION`,
  `POST_NOTIFICATIONS`, …). Apps using `ICameraService` must also forward `OnActivityResult`:

  ```csharp
  protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
  {
      if (MikoAndroidApp.HandleActivityResult(_view, requestCode, resultCode, data)) return;
      base.OnActivityResult(requestCode, resultCode, data);
  }
  ```

  Without this, camera and gallery calls wait forever — Android delivers the result to the
  Activity, not to the caller.

- **iOS** — declare usage descriptions in `Info.plist` (`NSCameraUsageDescription`,
  `NSLocationWhenInUseUsageDescription`, `NSPhotoLibraryUsageDescription`, …). Calling a
  capability without its description crashes the app.

- **Push notifications** — APNS certificates and FCM configuration, plus the corresponding
  `AppDelegate` / `FirebaseMessagingService` wiring, live in your app. Miko.Native does not
  bundle Firebase.

## Unsubscribing from events

Capabilities that stream data hand back a watch id or expose C# events. Release them when the
page goes away — a location watch left running keeps the GPS on:

```csharp
private string? _watchId;

protected override async Task OnInitializedAsync()
{
    _watchId = await GeolocationService.WatchPositionAsync(
        new PositionOptions { EnableHighAccuracy = true },
        (position, error) => { /* … */ });
}

public async ValueTask DisposeAsync()
{
    if (_watchId is not null) await GeolocationService.ClearWatchAsync(_watchId);
}
```

Low-level native subscriptions implement `INativeListener`; call `RemoveAsync()` to detach. It is
idempotent, so calling it from both an explicit teardown and `Dispose` is safe.
