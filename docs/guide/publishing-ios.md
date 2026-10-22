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
  <ApplicationVersion>1.0.0</ApplicationVersion>
</PropertyGroup>
```

- `ApplicationId` — the bundle identifier (must match the one in your provisioning profile and the Android `ApplicationId` if you have a multiplatform app). Use `--appId` when scaffolding: `dotnet new miko-multiplatform --appId com.mycompany.myapp`
- `ApplicationVersion` — the version string (e.g. `1.0.0`, `1.2.3`)

### In `Info.plist`

The template's `Info.plist` includes:

```xml
<key>CFBundleIdentifier</key>
<string>com.example.myapp</string>
<key>CFBundleShortVersionString</key>
<string>1.0.0</string>
<key>CFBundleVersion</key>
<string>1</string>
```

- `CFBundleIdentifier` must match `ApplicationId` in the csproj
- `CFBundleShortVersionString` is the public version (e.g. `1.0.0`)
- `CFBundleVersion` is the build number, incremented for each TestFlight or App Store submission

`dotnet new` with `--appId` sets all three automatically.

## App icon

The template includes a placeholder app icon in `Assets.xcassets/AppIcon.appiconset/`. Replace `Icon1024.png` with your own 1024×1024 px icon. This single image is used to generate all required sizes.

The `Icon1024.png` file is also the **App Store icon** shown in the App Store listing.

## Launch screen

iOS requires a launch screen. The template includes `Resources/LaunchScreen.storyboard`, a plain white screen with the app name. Without a launch screen, iOS runs the app in a letterboxed compatibility mode, and **App Store Connect will reject the upload**.

Customize the storyboard in Xcode if you want a branded splash screen.

## Privacy manifest

iOS 17.2 and later require apps to declare the "required reason" APIs they use. The template includes `Resources/PrivacyInfo.xcprivacy` with the APIs used by the .NET runtime itself:

- File timestamp APIs (for file I/O)
- System boot time API (for timer implementations)
- Disk space APIs (for storage checks)
- User defaults APIs (for settings persistence)

If your app uses additional privacy-sensitive APIs (camera, location, photo library, etc.), you must add entries to this file. See [Apple's Privacy manifest guide](https://developer.apple.com/documentation/bundleresources/privacy_manifest_files) for the full list.

## Scene lifecycle

The template uses the modern **UISceneSession** lifecycle:

- `AppDelegate.GetConfiguration()` creates a `UISceneConfiguration`
- `SceneDelegate` implements `WillConnect`, `DidDisconnect`, `DidBecomeActive`, `WillResignActive`, `WillEnterForeground`, and `DidEnterBackground`

The Miko rendering loop runs only when the scene is active. Background and foreground transitions are handled automatically.

## Minimum iOS version

The template declares a minimum of **iOS 15.0** in `Info.plist`:

```xml
<key>MinimumOSVersion</key>
<string>15.0</string>
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
