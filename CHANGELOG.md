# Changelog

All notable changes to Miko are documented in this file. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Long lists

- **`Virtualize<TItem>`** (`Miko.Components.Virtualization`) renders only the items of a list that
  can be seen, plus a few on either side, with Blazor's API (`Items` or `ItemsProvider`,
  `ItemContent` / `Context`, `ItemSize`, `OverscanCount`, `MaxItemCount`, `Placeholder`,
  `EmptyContent`, `SpacerElement`, `RefreshDataAsync`) and an `ItemKey` that keeps items and what is on
  screen in place when items are inserted above. Items may have any height: they are measured as they
  are rendered, and the scroll offset is adjusted so measured heights never move what is on screen.
  Opening, loading more and scrolling a list cost the same at 100,000 items as at 100 — the Infinite
  Scroll demo now uses it. See [Razor Components](docs/guide/razor-components.md#long-lists-virtualize).
- **`RenderFragment<TValue>`.** Templated components (`<ItemContent Context="row">`) compile, and a
  component that redeclares a base parameter with `new` receives it.

### Text layout and localization

- **Right-to-left layout.** `Direction` (or the new `dir="rtl"` attribute) now lays text out from
  the right, orders mixed-direction text with the Unicode Bidirectional Algorithm (UAX #9,
  Unicode 15.1, conformance-tested), starts flex rows at the right and aligns flex columns and
  narrower blocks to the right edge. See
  [Localization & Right-to-Left](docs/guide/localization.md).
- **Complex-script shaping.** Arabic, Hebrew, the Indic scripts, Thai and other scripts that need
  contextual forms, ligatures or reordering are shaped with HarfBuzz, as are combining
  diacritics and emoji sequences. Latin, Greek, Cyrillic and CJK text keeps its existing metrics.
  Font fallback now resolves whole characters (surrogate pairs) and keeps marks with their base,
  and line breaking never splits a character cluster.
- **`TextAlign.Start` and `TextAlign.End`.** `Start` is the new initial value (left in LTR, right in
  RTL), and `<td>`, `.ion-text-start` / `.ion-text-end` and Bootstrap's `.text-start` /
  `.text-end` use the logical values.
- **Localization example.** `examples/Localization` uses `IStringLocalizer` with `.resx` resources
  and switches the running app between English, Chinese, Arabic, Hebrew and Hindi.
- **Layouts can re-render themselves.** A layout calling `StateHasChanged` (after its own event
  handler) used to drop its page; the page now stays and re-renders with the layout.

### Date and time picker

- **`IonDatetime` covers Ionic's whole component.** All presentations — `date`, `date-time`,
  `time-date` (calendar, or wheels with `PreferWheel`) and `time`, `month`, `month-year`, `year`
  (wheels) — the month/year wheels behind the calendar's month label, the time popover, `Min` /
  `Max`, `YearValues` … `MinuteValues`, `HourCycle`, `IsDateEnabled`, `Multiple` (`@bind-Values`),
  `HighlightedDates`, `ShowAdjacentDays`, the Clear button, the `Title` / `TimeLabel` / `Buttons`
  fragments, `Size`, `FormatOptions`, and `ConfirmAsync` / `CancelAsync` / `ResetAsync`. Done and
  Cancel close the modal or popover the datetime is in. A swipe on the calendar pages months.
- **Localized.** `Locale` (default: the current culture) drives month and day names, the order of
  the date wheels, the hour cycle and the day period, from .NET culture data.

### Upgrade notes

- `ComputedStyle.TextAlign` is `TextAlign.Start` when nothing sets it (it was `Left`). Code that
  compares it with `TextAlign.Left` should map it first: in a left-to-right element `Start` is
  drawn exactly like `Left`.
- `IonDatetime` now follows Ionic's defaults and value model. `Presentation` defaults to
  `"date-time"` (set `Presentation="date"` for the former calendar-only picker); the header is
  shown only with `ShowDefaultTitle="true"`; with `ShowDefaultButtons` a pick changes the value on
  Done instead of right away; and a pick keeps the time part of the value (`2026-07-20T10:30:00`)
  where it used to write `yyyy-MM-dd`. The footer buttons are `IonButton`s (`#cancel-button`,
  `#confirm-button`) instead of `.datetime-action-button`, and the calendar labels follow the
  current culture rather than the invariant one. `IonCalendarDay` is gone.

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
- **Android process death.** When the system reclaims a backgrounded app, returning to it restores
  the current page and its back stack instead of starting over (page-internal state is not kept).
- **Android touch.** Only the first finger is tracked; a second finger no longer makes the pointer
  jump.
- **Linux diagnostics.** GLFW and OpenGL startup failures are reported with their cause (stderr and
  the exception message) instead of a native crash or a generic error.
- **Packaging.** Android native libraries are 16 KB page-aligned (Google Play); Linux desktop apps
  carry `libSkiaSharp.so`; DevTools is no longer pulled into Android/iOS packages and desktop heads
  reference it in Debug builds only. Every package includes `THIRD-PARTY-NOTICES.txt`, a README and
  XML documentation; `Miko.Razor.Compiler` shrank from 34.5 MB to 0.8 MB (it no longer carries
  Roslyn, which the consuming compiler provides).
- **Templates.**
  - `--appId` sets the Android `ApplicationId` and the iOS `ApplicationId` / `CFBundleIdentifier`;
    the default is derived from the project name and is always a valid identifier.
  - Project names that are not C# identifiers (`-n my-app`) now build.
  - Default app icons (from the .NET MAUI template), an iOS launch screen, privacy manifest and
    scene lifecycle, Android target API 36 with the `INTERNET` permission, and a `.slnx` solution
    for the multiplatform template.
  - `--agent claude|codex|cursor` scaffolds the Miko documentation for the app's Miko version into
    `.miko/` plus the assistant's configuration. The Claude preset pre-approves only the Miko MCP
    server, not shell or file-write access. Only these scaffolds start the MCP debug server from
    the simulator head; with the default `--agent none` it is not referenced.
  - The `full` layout's demo images are generated placeholders, no longer third-party photos and
    logos.
- **Ionic: every public parameter takes effect.** Parameters that were declared but did nothing
  are either implemented or removed, and a docs test now rejects summaries that describe a
  parameter as inert.
  - New: `Href` (with `RouterDirection`) navigates on `IonButton`, `IonFabButton`, `IonItemOption`
    and `IonCard`, like `IonItem`; `Trigger` opens `IonActionSheet`, `IonAlert`, `IonLoading`,
    `IonToast`, `IonModal` and `IonPopover` (plus `TriggerAction` click / context-menu) from the
    element with that id; every component takes an `Id`; `IonSearchbar.Debounce`,
    `IonTextarea.AutoGrow`, `IonInputOtp.Inputmode`, and `Color` on `IonDatetime` /
    `IonDatetimeButton` now work.
  - `IonTextarea` now renders at its `Rows` height; it used to collapse to its padding.

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
- These Ionic parameters were removed because they had no effect. Code that sets them from C#
  no longer compiles; in Razor markup the attribute is silently ignored, so search for them:
  - `Translucent` on the six overlays and `IonFabButton` (and `IonPopoverOptions.Translucent`)
  - `Name` on `IonCheckbox`, `IonToggle`, `IonRadio`, `IonRadioGroup`, `IonInput`, `IonTextarea`,
    `IonSelect`, `IonSearchbar`; `Value` on `IonCheckbox` and `IonToggle`; `IonSelect.Required`
  - `Type` on `IonFabButton`, `IonItem`, `IonItemOption` and `IonMenu`; `IonCard.Target` / `Rel`
  - `IonContent.Fullscreen` / `ScrollEvents`, `IonFooter.Collapse`, `IonItemDivider.Sticky`
  - `IonModal.KeepContentsMounted`, `IonAccordionGroup.Animated`, `IonMenu.Animations`,
    `IonSegmentButton.ContentId`, `IonTabButton.Tab`, `IonDatetimeButton.Datetime`
  - `IonDatetime.Presentation` / `Size`, `IonRange.Snaps`, `IonInputOtp.Autocapitalize`,
    `IonPopover.Dismissable` (use `BackdropDismiss`), `IonSearchbar.Animated`
  - Theme tokens `InputToken.PlaceholderColor`, `SelectToken.LabelColor`,
    `SpinnerToken.SmallSize`
