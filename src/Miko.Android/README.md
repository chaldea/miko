# Miko.Android

Android host for [Miko](https://github.com/chaldea/miko), the native .NET UI engine that renders
Razor components with SkiaSharp. It provides the GL surface, touch and soft-keyboard input, the
app lifecycle, and the Android implementations of `Miko.Native` and video playback
(MediaCodec). Android 8.0 (API 26) and later.

```csharp
[Activity(MainLauncher = true, Theme = "@android:style/Theme.Material.Light.NoActionBar",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.UiMode
        | ConfigChanges.FontScale | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden)]
public class MainActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(MikoAndroidApp.CreateView(this,
            () => MyApp.App.CreateContext(builder => builder.UseAndroidNative())));
    }
}
```

- `UseAndroidNative()` registers the native capabilities and the platform services components
  use (haptics, orientation, back button). `UseAndroidVideo()` registers the video backend.
- `ConfigurationChanges` keeps the Activity, and with it the app's state, across rotation, dark
  mode and font-size changes.

The easiest start is the template: `dotnet new install Miko.Templates::1.0.0-preview.1`, then
`dotnet new miko-multiplatform -o MyApp`.
See the [documentation](https://chaldea.github.io/miko/) and the
[Android publishing guide](https://chaldea.github.io/miko/guide/publishing-android).
