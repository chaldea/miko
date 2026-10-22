# Miko.iOS

iOS host for [Miko](https://github.com/chaldea/miko), the native .NET UI engine that renders
Razor components with SkiaSharp. It provides the Metal view, touch and keyboard input (including
composing input methods), the app lifecycle, and the iOS implementations of `Miko.Native` and
video playback (AVPlayer). iOS 15 and later.

```csharp
[Register("SceneDelegate")]
public class SceneDelegate : UIWindowSceneDelegate
{
    public override UIWindow? Window { get; set; }

    public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        if (scene is not UIWindowScene windowScene) return;
        Window = new UIWindow(windowScene)
        {
            RootViewController = new MikoViewController(
                MyApp.App.CreateContext(builder => builder.UseIosNative()))
        };
        Window.MakeKeyAndVisible();
    }
}
```

`UseIosNative()` registers the native capabilities and the platform services components use
(haptics, orientation); `UseIosVideo()` registers the video backend.

The easiest start is the template: `dotnet new install Miko.Templates::1.0.0-preview.1`, then
`dotnet new miko-multiplatform -o MyApp`. It includes the scene lifecycle, app icon, launch screen
and privacy manifest an App Store build needs. See the
[documentation](https://chaldea.github.io/miko/) and the
[iOS publishing guide](https://chaldea.github.io/miko/guide/publishing-ios).
