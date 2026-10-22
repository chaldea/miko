# Changelog

All notable changes to Miko will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0-preview.1] - 2026-10-22

First public preview release of Miko — a native, cross-platform UI rendering engine for .NET that uses Razor as its layout DSL and draws every pixel with SkiaSharp.

### What's in the Box

**Core packages:**
- **Miko** — the rendering engine (DOM, styling, layout, painting, fonts, events, routing, components, animations)
- **Miko.Razor.Compiler** — source generator that compiles `.razor` components and discovers routes
- **Miko.Ionic** — Ionic-style Razor component library with Material Design and iOS modes (50+ components: tabs, side menu, icons, overlays, lists, forms, pickers, range, datetime, slides, and more)

**Platform hosts:**
- **Miko.Windowing** — desktop host for Windows, macOS (Metal), and Linux (OpenGL)
- **Miko.Android** — Android host (API 26+) with hardware-accelerated video decoding
- **Miko.iOS** — iOS host (15.0+) with Metal rendering and IME composition support

**Development tools:**
- **Miko.DevTools** — runtime DOM and layout tree inspector (Debug builds only)
- **Miko.Simulator** — device simulator for previewing mobile apps on desktop
- **Miko.McpServer** — Model Context Protocol server for agent-driven debugging (DOM inspection, screenshots, click simulation)

**Preview APIs (may change before 1.0 stable):**
- **Miko.Native** — 17 native capability interfaces modeled on Ionic Capacitor (camera, clipboard, filesystem, geolocation, haptics, keyboard, notifications, motion, network, screen reader, splash screen, status bar, toast, and more)
- **Miko.Components** — extended components including native video player with hardware decoding and danmaku (bullet comments)
- **Miko.Testing** — testing utilities for Miko apps

**Discontinued:**
- **Miko.Bootstrap** — not included in 1.0; `Miko.Ionic` is the official component library

### Highlights

#### Component lifecycle and reuse
- Child components are now **reused across parent re-renders** by identity (type + scope + sequence + `@key`), matching Blazor semantics. `OnInitialized` runs once per instance; `OnParametersSet` runs on every parent render.
- `@bind` two-way binding works correctly (compiler now collects `[Parameter]` properties from base classes including `ComponentBase.ChildContent`).
- `@ref` component references are assigned and updated correctly across re-renders.
- **Render batching**: `StateHasChanged` calls during event handlers are queued and flushed after the synchronous part completes, preventing redundant re-renders.
- Disposal is element-driven: discarded root trees (navigation, hot reload) are disposed without leaking reused component instances.
- Elements preserve runtime state (focus, caret, layout box) when moved across a re-render via `@key`.

#### Soft keyboard avoidance and IME
- **Android API 30+ and iOS**: the layout automatically moves content above the soft keyboard when an input is focused.
- **Android API 26–29**: relies on the system `windowSoftInputMode` setting.
- **iOS IME composition** is implemented (Japanese kana, Chinese pinyin); tap-to-position and selection handles are not yet supported.

#### Platform-specific improvements
- **macOS and iOS**: rendering moved from OpenGL to **Metal** via `CAMetalLayer`, matching the iOS path.
- **Linux**: `libSkiaSharp.so` is shipped via `SkiaSharp.NativeAssets.Linux` (no manual installation required).
- **Android**: apps satisfy the 16 KB page alignment requirement for Google Play (API 35+ targeting).
- **Desktop**: `Miko.DevTools` is referenced only in Debug configuration (`Condition="'$(Configuration)' == 'Debug'"`) so Release builds exclude it and its native dependencies automatically.

#### Templates and scaffolding
- **Multiplatform template** includes a `.slnx` solution file with Desktop, Android, iOS, Simulator, and shared UI projects.
- **App icons and launch screens**: Android adaptive icon (foreground/background + fallback), iOS `Icon1024.png`, Android 12+ automatic splash from icon, iOS `LaunchScreen.storyboard`.
- **`--appId` option**: sets `ApplicationId` (Android), `CFBundleIdentifier` (iOS), and namespace consistently across all heads.
- **`--agent` option**: scaffolds `.miko/docs/` (the complete Miko documentation for the version the app was created with, including per-component parameter/event/slot/theming reference), `.miko/rules.md`, `llms.txt`, and the matching assistant config (`.claude/`, `.codex/`, or `.cursor/`) so AI agents can build Miko UI with full context.
- **Android**: `MainActivity` declares `ConfigurationChanges` so rotation, dark mode, and font scale changes do not restart the app.
- **iOS**: templates include `PrivacyInfo.xcprivacy` listing the required-reason APIs the .NET runtime uses.

#### Ionic component library
- **50+ components** ported from Ionic Framework with Material Design and iOS modes.
- **Token-based theming**: `IonicTheme` with 200+ design tokens; styles are registered on-demand per component type (not all 50+ sheets upfront).
- **Navigation**: `IonItem` `href` push, `IonBackButton` nav-pop, `IonTabs` root switch; `IonicPageTransitions` by mode (iOS push/pop, Material shared-axis).
- **Platform-aware**: mode flows from `IPlatformInfo` (iOS devices → `"ios"`, others → `"md"`); components use `ClassMapper` to build CSS classes conditionally.
- **Overlays**: `IonActionSheet`, `IonAlert`, `IonLoading`, `IonModal`, `IonPopover`, `IonToast` with enter/leave animations.
- **Forms**: `IonInput`, `IonTextarea`, `IonCheckbox`, `IonRadio`, `IonToggle`, `IonSelect`, `IonSearchbar`, `IonRange`, `IonSegment`.
- **Lists**: `IonList`, `IonItem`, `IonItemSliding`, `IonReorderGroup` (drag-to-reorder), `IonInfiniteScroll`, `IonRefresher` (pull-to-refresh).
- **Pickers**: `IonPicker`, `IonDatetime` (calendar view only; time picker, min/max, multi-date not yet implemented).
- **Media**: `IonSlides` (swiper-style carousel with gesture support), `IonThumbnail`, `IonAvatar`, `IonImg`.
- **Icons**: `IonIcon` with resolution order (name → md/ios mode-specific → fallback), external `src` via `IResourceAssemblyProvider`.
- **Haptics**: selection ticks on `IonPickerColumn` and `IonRange` (Android via `View.PerformHapticFeedback`, iOS via `UIImpactFeedbackGenerator`).

#### Layout and styling
- **Grid layout** (subset): no `repeat()`, `minmax()`, or named areas; `justify-items` / `justify-self` inline-axis alignment.
- **Table layout**: rows, cells, column width distribution.
- **Inline formatting context** unified: text nodes are split into `LayoutFragments` per line; line breaks only at spaces or CJK boundaries; atomic inline boxes do not wrap mid-box.
- **CSS logical properties**: `ComputedStyle` stores logical values canonically (`InlineSize`, `BlockStart`, etc.); physical properties are facades mapped via computed `Direction` / `WritingMode`.
- **Aspect ratio**: `aspect-ratio` property with stretch interaction (stretch pins the main axis).
- **Cascade layers**: `StyleSheet.Layer` (default 0); higher-layer rules always beat lower-layer rules regardless of specificity. `Miko.Ionic` uses layer -1 so app rules override component styles.
- **Scoped restyles**: three-level mutation tracking (whole tree / subtree / content-only) + selector bloom filter for ancestor matching → steady-state restyle is proportional to what changed, not the tree size.

#### Performance
- **Layout caching**: keyed by `MutationVersion` + constraints; steady-state 0 B when idle (no allocations after the first frame).
- **Text runs and fonts cached per frame**: text segmentation by script/font is reused across elements; `SKFont` and bitmap image wrappers are pooled.
- **Reuse the window render target**: desktop wraps the GL framebuffer as a `GRBackendRenderTarget`, reused across frames (not recreated per frame).
- **Length struct compacted**: 16 bytes → 12 bytes (shaved 3 KB per element, ~1 MB per 300-element page).
- **Paint-only animations**: navigation transitions and overlay enter/leave animate via a paint-only layer (`RenderLayer` with offset and opacity) without restyle/layout.
- **SaveLayer bounds**: opacity &lt; 1 now passes tight bounds to `SaveLayer` (not the entire clip region).

#### Resources and video
- **`res://` paths are assembly-agnostic**: no assembly name in the path; `EmbeddedResources` matches on a dot-boundary suffix. Moving a project or renaming an assembly never rewrites resource paths.
- **DevTools resource inspector**: shows what Miko downloaded (URL, size, duration) and what sits in the decode cache.
- **System-native video decoding**: Windows (Media Foundation D3D11VA), Linux (GStreamer VAAPI), macOS (AVFoundation VideoToolbox, zero-copy via `CVMetalTextureCache`), Android (MediaCodec), iOS (AVPlayer). **No bundled FFmpeg** in any platform host (third-party native binaries are opt-in only).
- **NV12 → RGB shader**: hardware decoders output NV12; SkiaSharp 3.119.1 does not export `SKYUVAInfo`, so Miko uses an `SKRuntimeEffect` (SkSL) shader to convert Y and UV planes to RGB.

#### MCP server and agent support
- **Model Context Protocol server** listens on `http://localhost:5800` when running the simulator with `builder.AddMikoMcpServer()`.
- Tools: `dom_tree`, `element_info`, `click_element`, `computed_style`, `box_model`, `evaluate_selector`, `screenshot`, `device_list`, `switch_device`, `rotate_device`.
- **Development only**: the MCP server has no authentication and should never ship in a distributed app.

#### Documentation
- **VitePress site** published to GitHub Pages at [chaldea.github.io/miko](https://chaldea.github.io/miko/).
- **Generated component reference**: `docs/components/ionic/**/*.md` are filled by `tools/Miko.DocGen` from reflection over `Miko.Ionic` + XML doc comments; `tests/Miko.Docs.Tests` guards drift.
- **Shipped to scaffolded apps**: the complete docs are packed into `Miko.Templates` and appear in `.miko/docs/` when scaffolding with `--agent`, so agents have the full reference for the version the app was created with.

### Known Limitations

See [Known Limitations](https://chaldea.github.io/miko/guide/known-limitations) for the full list. Key boundaries:
- No accessibility (screen readers see one canvas).
- No RTL or complex text shaping (no bidi, no HarfBuzz).
- CSS subset: no `:nth-child`, `:has`, `:is`, `::placeholder`, `position: sticky`, `filter`, `backdrop-filter`, `object-fit`.
- Styles are C# objects only; no `.css` file parsing.
- Single-pointer touch (no pinch/rotate gestures).
- iOS IME composition works; tap-to-position and selection handles do not.
- `IonDatetime` has calendar view only.
- No list virtualization (long `IonInfiniteScroll` lists keep all items in the DOM).
- Desktop trimming and Native AOT are not officially supported in 1.0.0-preview.1 (iOS and Android use full AOT and are tested).

### Upgrade Notes

- **Preview versions must be requested explicitly**: `dotnet new install Miko.Templates::1.0.0-preview.1` and `dotnet add package Miko --prerelease` (or `--version 1.0.0-preview.1`).
- **Local development**: the templates reference `1.0.0-dev` locally (when restored from the in-repo `packages/` feed). After `dotnet pack miko.slnx`, delete stale cached copies (`rm -rf ~/.nuget/packages/miko*`) before restoring again.
- **Released packages**: the published NuGet packages are versioned `1.0.0-preview.1`.

### Dependencies

- .NET 10.0
- SkiaSharp 3.119.1
- HarfBuzzSharp 8.4.0
- Silk.NET (desktop host only)
- Microsoft.CodeAnalysis.CSharp 4.12.0 (Razor compiler)

### Third-party notices

The repository includes `THIRD_PARTY_NOTICES.txt` listing all dependencies and their licenses.

---

## What's Next

The 1.0 stable release will focus on:
- Documentation polish and additional examples
- Performance profiling and optimization on mobile devices
- Stabilizing the `Miko.Native` API surface
- Addressing feedback from preview users

See the [GitHub repository](https://github.com/chaldea/miko) for the roadmap and issue tracker.
