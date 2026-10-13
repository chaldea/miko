# IonicDemo.iOS

This example targets Xcode 27 / `net10.0-ios27.0` and requires iOS 15 or later.
The .NET iOS bindings currently require the `XCODE_27_0_PREVIEW` opt-in in the
project file.

Run from this directory, selecting an available simulator:

```sh
dotnet run --device "iPhone 17"
```

Or run from the repository root:

```sh
dotnet run --project examples/Ionic/IonicDemo.iOS --device "iPhone 17"
```

Use `xcrun simctl list devices available` to find a simulator name or UUID.
The project sets `RunWorkingDirectory` so the iOS launcher can resolve the app
bundle when invoked from another directory.

The app uses the UIKit scene lifecycle required by iOS 27: `Info.plist` declares
scene support, `AppDelegate.GetConfiguration` selects `SceneDelegate`, and
`SceneDelegate.WillConnect` creates the window using its `UIWindowScene`.

Rendering goes through the Miko.iOS Metal host (`MikoMetalView`), which uses the
host Mac's GPU inside the Simulator. The earlier OpenGL ES host only had the
Simulator's software renderer and ran at roughly 1–6 FPS (ISSUE-147).

If an SDK change or incremental build leaves stale AOT outputs (for example,
`Failed to load AOT module '...'` with a dependent assembly GUID mismatch),
rebuild once, then run again:

```sh
dotnet build -t:Rebuild
dotnet run --device "iPhone 17"
```

An IDE connection warning on port 10000 is expected when starting a Debug build
without an IDE debugger attached; it does not prevent the app from running.
