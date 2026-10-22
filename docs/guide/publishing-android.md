# Publishing Android Apps

This guide covers publishing Miko apps for **Android** using the `Miko.Android` host. The output is an **APK** (for direct installation) or **AAB** (Android App Bundle, for Google Play).

## Requirements

- .NET 10 SDK with the `android` workload installed (`dotnet workload install android`)
- Android SDK (installed automatically by the workload)
- A signing keystore (required for Release builds and Google Play)

## Build a Release AAB

Google Play requires apps to be uploaded as an **Android App Bundle** (`.aab` file):

```bash
dotnet publish MyApp.Android/MyApp.Android.csproj \
  -c Release \
  -f net10.0-android \
  -p:AndroidPackageFormat=aab
```

The output is `bin/Release/net10.0-android/publish/com.example.myapp-Signed.aab`.

## Build a Release APK

To build an APK for direct installation or testing:

```bash
dotnet publish MyApp.Android/MyApp.Android.csproj \
  -c Release \
  -f net10.0-android
```

The output is `bin/Release/net10.0-android/publish/com.example.myapp-Signed.apk`.

## Signing

Android requires all Release builds to be signed. The templates do not include a keystore by default. You must create one and configure signing properties.

### Create a keystore

```bash
keytool -genkey -v \
  -keystore myapp.keystore \
  -alias myapp \
  -keyalg RSA \
  -keysize 2048 \
  -validity 10000
```

Keep the keystore file and password safe. If you lose them, you cannot update your app on Google Play.

### Configure signing in the csproj

Add the following properties to `MyApp.Android.csproj`:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <AndroidKeyStore>true</AndroidKeyStore>
  <AndroidSigningKeyStore>$(MSBuildProjectDirectory)/myapp.keystore</AndroidSigningKeyStore>
  <AndroidSigningKeyAlias>myapp</AndroidSigningKeyAlias>
  <AndroidSigningKeyPass>$(KEYSTORE_PASSWORD)</AndroidSigningKeyPass>
  <AndroidSigningStorePass>$(KEYSTORE_PASSWORD)</AndroidSigningStorePass>
</PropertyGroup>
```

**Important:** Never commit passwords to source control. Pass them via environment variables:

```bash
export KEYSTORE_PASSWORD="your_password_here"
dotnet publish MyApp.Android/MyApp.Android.csproj -c Release -f net10.0-android -p:AndroidPackageFormat=aab
```

## App metadata

The following properties in `MyApp.Android.csproj` control the app's identity and versioning:

```xml
<PropertyGroup>
  <ApplicationId>com.example.myapp</ApplicationId>
  <ApplicationVersion>1</ApplicationVersion>
  <ApplicationDisplayVersion>1.0.0</ApplicationDisplayVersion>
</PropertyGroup>
```

- `ApplicationId` — the package name (must match `CFBundleIdentifier` in your iOS project if you have one). Use `--appId` when scaffolding to set both: `dotnet new miko-multiplatform --appId com.mycompany.myapp`
- `ApplicationVersion` — integer version code, incremented for each Play Store release (users never see this)
- `ApplicationDisplayVersion` — the public version string (e.g. `1.0.0`, `1.2.3-beta`)

## AndroidManifest.xml

The template's `AndroidManifest.xml` declares:

- `targetSdkVersion="36"` (Android 14, API level 36) — required for new apps and updates on Google Play as of August 2026
- `minSdkVersion="26"` (Android 8.0) — Miko's minimum supported version
- `INTERNET` permission — required for network access in Release builds (Debug builds add it implicitly)

```xml
<uses-sdk android:minSdkVersion="26" android:targetSdkVersion="36" />
<uses-permission android:name="android.permission.INTERNET" />
```

The MainActivity declares `ConfigurationChanges` so that rotation, dark mode toggle, and font scale changes do not restart the entire app:

```xml
<activity
    android:name=".MainActivity"
    android:configChanges="orientation|screenSize|uiMode|screenLayout|smallestScreenSize|density|fontScale|keyboard|keyboardHidden"
    ...>
</activity>
```

## App icon and launch screen

### App icon

The template includes a default adaptive icon in `Resources/mipmap-*/`:

- `appicon_foreground.png` — foreground layer (with transparency)
- `appicon_background.png` — background layer (solid color or image)
- `appicon.png` — fallback for launchers that do not support adaptive icons

Replace these files with your own icon assets. The default icon is generated from the .NET MAUI template and is only a placeholder.

Declare them in `MainActivity`:

```csharp
[Activity(
    Icon = "@mipmap/appicon",
    RoundIcon = "@mipmap/appicon_round",
    ...)]
```

### Launch screen

Android 12 and later generate the launch screen automatically from the app icon. The template does not include a custom `SplashScreen` theme. If you need a custom splash screen, add a theme and reference it in the `Activity` attribute.

## Permissions and features

If your app uses Miko.Native capabilities (camera, location, notifications, etc.), you must declare the corresponding permissions in `AndroidManifest.xml`. For example:

```xml
<uses-permission android:name="android.permission.CAMERA" />
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
```

See the [Android permissions guide](https://developer.android.com/guide/topics/permissions/overview) for the full list.

## 16 KB page alignment

Google Play requires native libraries in apps targeting API 35+ to be aligned to 16 KB page boundaries. The Miko packages and their dependencies (`SkiaSharp`, `HarfBuzzSharp`) satisfy this requirement as of version 1.0.0-dev. If you see the `XA0141` warning, you are using an older version of a dependency that does not meet the alignment requirement.

## Testing

Test the signed AAB or APK on a real device or emulator:

```bash
# Install and run (device or emulator must be connected via adb)
adb install bin/Release/net10.0-android/publish/com.example.myapp-Signed.apk
adb shell am start -n com.example.myapp/crc64f8a89c5b5f7f9e0f.MainActivity
```

Or open the project in Visual Studio or Rider and deploy to a connected device.

## Upload to Google Play

1. Sign in to the [Google Play Console](https://play.google.com/console/)
2. Create a new app or select an existing one
3. Under **Release > Production** (or Internal testing / Closed testing / Open testing), create a new release
4. Upload the signed AAB file
5. Fill in the release notes and roll out

Google Play will generate APKs optimized for each device configuration from your AAB.
