# Publishing iOS Apps

This guide covers publishing Miko apps for **iOS** using the `Miko.iOS` host. The output is an **IPA** archive that can be distributed via TestFlight or the App Store.

## Requirements

- macOS with Xcode installed
- .NET 10 SDK with the `ios` workload (`dotnet workload install ios`)
- An Apple Developer account (required for signing and App Store distribution)
- A signing identity (certificate) and provisioning profile

## Configure signing

iOS apps must be signed with a certificate and provisioning profile. Add the following properties to `MyApp.iOS.csproj`:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <CodesignKey>iPhone Distribution: Your Name (XXXXXXXXXX)</CodesignKey>
  <CodesignProvision>Your Provisioning Profile Name</CodesignProvision>
</PropertyGroup>
```

- `CodesignKey` — the name of the signing identity (certificate) in your macOS Keychain. Use `security find-identity -v -p codesigning` to list available identities.
- `CodesignProvision` — the name of the provisioning profile installed on your Mac. Profiles are in `~/Library/MobileDevice/Provisioning Profiles/`.

## Build and archive

```bash
dotnet publish MyApp.iOS/MyApp.iOS.csproj \
  -c Release \
  -f net10.0-ios \
  -r ios-arm64 \
  -p:ArchiveOnBuild=true
```

The output is an `.xcarchive` in `bin/Release/net10.0-ios/ios-arm64/publish/`. You can also export an IPA from the archive:

```bash
xcodebuild -exportArchive \
  -archivePath bin/Release/net10.0-ios/ios-arm64/publish/MyApp.xcarchive \
  -exportPath bin/Release/net10.0-ios/ios-arm64/publish/ \
  -exportOptionsPlist ExportOptions.plist
```

See [Apple's documentation](https://developer.apple.com/documentation/xcode/distributing-your-app-for-beta-testing-and-releases) for creating an `ExportOptions.plist` file.

## App metadata

The following properties control the app's identity and versioning:

### In `MyApp.iOS.csproj`

```xml
<PropertyGroup>
  <ApplicationId>com.example.myapp</ApplicationId>
  <ApplicationVersion>1</ApplicationVersion>
  <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
</PropertyGroup>
```

- `ApplicationId` — the bundle identifier; it must match your provisioning profile and `CFBundleIdentifier` below. `dotnet new miko-multiplatform --appId com.mycompany.myapp` sets it together with the Android `ApplicationId`.
- `ApplicationVersion` — the build number (`CFBundleVersion`), incremented for every upload
- `ApplicationDisplayVersion` — the public version (`CFBundleShortVersionString`)

### In `Info.plist`

The template's `Info.plist` includes:

```xml
<key>CFBundleIdentifier</key>
<string>com.example.myapp</string>
<key>CFBundleShortVersionString</key>
<string>1.0</string>
<key>CFBundleVersion</key>
<string>1</string>
```

- `CFBundleIdentifier` must match `ApplicationId` in the csproj; `--appId` writes both
- Keep `CFBundleShortVersionString` / `CFBundleVersion` in step with `ApplicationDisplayVersion` / `ApplicationVersion` when you bump the version

## App icon

The template includes a placeholder icon (the .NET bot from the .NET MAUI template) in `Assets.xcassets/AppIcon.appiconset/`, referenced from `Info.plist` by `XSAppIconAssets`. The set contains one PNG per size listed in its `Contents.json`; replace every file with your artwork at the same pixel size. `Icon1024.png` is the App Store icon.

## Launch screen

iOS requires a launch screen. The template includes `Resources/LaunchScreen.storyboard`, a plain white view, referenced by `UILaunchStoryboardName` in `Info.plist`. Without a launch screen, iOS runs the app in a letterboxed compatibility mode, and **App Store Connect will reject the upload**.

Customize the storyboard in Xcode if you want a branded splash screen.

## Privacy manifest

App Store Connect requires apps to declare the "required reason" APIs they use. The template includes `Resources/PrivacyInfo.xcprivacy` (from the .NET MAUI template) with the categories the .NET runtime itself uses: file timestamps, system boot time and disk space. A user-defaults entry is present but commented out; enable it if your app uses `NSUserDefaults` (directly or through a library).

If your app uses additional privacy-sensitive APIs (camera, location, photo library, etc.), you must add entries to this file. See [Apple's Privacy manifest guide](https://developer.apple.com/documentation/bundleresources/privacy_manifest_files) for the full list.

## Scene lifecycle

The template uses the modern **UISceneSession** lifecycle:

- `Info.plist` declares `UIApplicationSceneManifest`
- `AppDelegate.GetConfiguration()` returns a configuration whose delegate is `SceneDelegate`
- `SceneDelegate.WillConnect` creates the window for its `UIWindowScene` and sets `MikoViewController` as the root

Background and foreground transitions need no code: `MikoViewController` stops rendering and pauses video when the app enters the background, resumes when it returns, and releases GPU caches on a memory warning.

## Minimum iOS version

The template sets a minimum of **iOS 15.0** in the iOS csproj (the build writes `MinimumOSVersion` into the app's `Info.plist` from it):

```xml
<SupportedOSPlatformVersion>15.0</SupportedOSPlatformVersion>
```

This is Miko's minimum supported iOS version. You can raise it if your app targets newer iOS features.

## Upload to App Store Connect

Once you have a signed IPA (either from `dotnet publish` + manual export, or from Xcode's archive organizer):

### Option 1: Xcode Transporter

1. Open **Transporter** (separate app, installed with Xcode)
2. Sign in with your Apple ID
3. Drag the IPA file into the window
4. Click **Deliver**

### Option 2: Command line

```bash
xcrun altool --upload-app \
  --type ios \
  --file MyApp.ipa \
  --username "your-apple-id@example.com" \
  --password "@keychain:Application Loader: your-apple-id@example.com"
```

Use an **app-specific password** (generated in your Apple ID account settings), not your main Apple ID password.

## TestFlight

After uploading to App Store Connect, the build appears in the **TestFlight** section within a few minutes (after automated processing completes). You can then:

- Add internal testers (up to 100, no review required)
- Add external testers (up to 10,000, requires a brief App Store review)

TestFlight builds expire after 90 days.

## App Store submission

Once testing is complete:

1. Go to **App Store Connect > My Apps > [Your App]**
2. Create a new version under **App Store** (if this is the first release)
3. Select the build from TestFlight
4. Fill in the release notes, screenshots, description, and app category
5. Submit for review

Apple's review typically takes 1–3 days.

## What to check before submitting

- [ ] App icon is replaced (not the default placeholder)
- [ ] Launch screen is present (`LaunchScreen.storyboard`)
- [ ] Bundle identifier matches the provisioning profile
- [ ] Privacy manifest lists all required-reason APIs your app uses
- [ ] `CFBundleVersion` is incremented from the previous build
- [ ] The app runs correctly on a real device (not just the simulator)
- [ ] Screenshots and metadata are ready in App Store Connect

If the app uses Miko.Native capabilities (camera, location, etc.), ensure the corresponding `Info.plist` usage description strings are present (e.g. `NSCameraUsageDescription`, `NSLocationWhenInUseUsageDescription`).
