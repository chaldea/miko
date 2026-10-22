# Changelog

All notable changes to Miko are documented in this file. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0-preview.1]

First public preview. Miko is a native, cross-platform UI engine for .NET: Razor components are
compiled into native element trees and drawn with SkiaSharp, with no browser or WebView. This
preview is for building complete apps with `Miko` and `Miko.Ionic` on Windows, macOS, Linux,
Android and iOS. APIs may still change before 1.0.

### Packages

| Package | Status |
| --- | --- |
| `Miko`, `Miko.Razor.Compiler`, `Miko.Ionic` | Core of the release |
| `Miko.Windowing`, `Miko.Android`, `Miko.iOS` | Platform hosts |
| `Miko.Templates` | `dotnet new miko-razor` (desktop) and `miko-multiplatform` (Desktop / Simulator / Android / iOS) |
| `Miko.Native` | Preview: capability interfaces (camera, clipboard, filesystem, …) whose API may change |
| `Miko.Components`, `Miko.Testing` | Preview |
| `Miko.DevTools`, `Miko.Simulator`, `Miko.McpServer` | Development tools; never ship them in a release build |

`Miko.Bootstrap` is discontinued and is not published.

Supported platforms: Windows 10/11, macOS, Linux (glibc), Android 8.0 (API 26) and later, iOS 15
and later.

### Release readiness

- **Mobile app lifecycle.** The Android and iOS hosts stop rendering in the background, pause
  playing videos and resume them on return, and free GPU caches on memory warnings; apps need no
  code for this. `IAppService.OnPause` / `OnResume` / `OnAppStateChange` now fire.
- **Soft keyboard.** The page is laid out above the keyboard (footers sit on it) and the focused
  field scrolls into view (Android 11+ / API 30+, iOS).
- **iOS input methods.** Composing input methods (pinyin, kana, …) work through `UITextInput`.
- **Android configuration changes.** Rotation, dark mode, font-scale and display-size changes no
  longer recreate the Activity and rebuild the app (templates and examples declare
  `ConfigurationChanges`; the host refreshes its density).
- **Packaging.** Android native libraries are 16 KB page-aligned (Google Play); Linux desktop apps
  carry `libSkiaSharp.so`; DevTools is no longer pulled into Android/iOS packages and desktop heads
  reference it in Debug builds only. Every package includes `THIRD-PARTY-NOTICES.txt`.
- **Templates.**
  - `--appId` sets the Android `ApplicationId` and the iOS `ApplicationId` / `CFBundleIdentifier`;
    the default is derived from the project name and is always a valid identifier.
  - Project names that are not C# identifiers (`-n my-app`) now build.
  - Default app icons (from the .NET MAUI template), an iOS launch screen, privacy manifest and
    scene lifecycle, Android target API 36 with the `INTERNET` permission, and a `.slnx` solution
    for the multiplatform template.
  - `--agent claude|codex|cursor` scaffolds the Miko documentation for the app's Miko version into
    `.miko/` plus the assistant's configuration. The Claude preset pre-approves only the Miko MCP
    server, not shell or file-write access.
  - The `full` layout's demo images are generated placeholders, no longer third-party photos and
    logos.

### Known limitations

No accessibility (screen readers see one canvas), no right-to-left or complex text shaping, a CSS
subset (no `:nth-child`, `:has`, `position: sticky`, `filter`, `object-fit`, grid `repeat()` /
areas), styles written in C# only, single-pointer touch, `IonDatetime` calendar view only, no list
virtualization, and no trimming / Native AOT for desktop apps. See the
[known limitations](docs/guide/known-limitations.md) page for details.

### Upgrade notes

- Preview packages are not picked up by default: install the templates with
  `dotnet new install Miko.Templates::1.0.0-preview.1`, and add packages with `--prerelease` (or
  `--version 1.0.0-preview.1`).
- Write the template layout as `--layout=<value>`: `dotnet new` reads a separate `list` argument
  as its own `list` command.
- Builds from source produce `1.0.0-dev` packages, which sort below `1.0.0-preview.N`.
