# Book and Comic Readers

`Miko.Components.Reader` provides `BookReader` and `ComicReader`. Both include mobile reading controls, a directory, appearance and settings sheets, loading/error states and retry. They use native Miko elements and drawing, without a WebView or Ionic dependency. No service registration is required beyond the usual Miko app setup.

## Mobile reading UI

Readers start in immersive mode with small chapter and page labels. Tap the page once to reveal the back/title bar and bottom toolbar; tap again to hide them. The toolbar offers previous/next, a progress slider, directory, appearance, day/night and reader-specific settings. Double-tapping an image zooms it without toggling the toolbar.

Directory, appearance and settings open as bottom sheets. Tap **Done**, the surrounding dimmed area, or the system back button to dismiss a sheet. The content scrolls on short screens; controls respect the host's safe-area insets. Opening controls does not resize the reading surface or change text pagination. Android back/Escape closes a sheet first, then hides the controls, then invokes `BackRequested` so the application can return to its shelf.

Book settings include font, size, line/paragraph spacing, margins and paged/scroll mode. Comic settings include single/double pages, reading direction, orientation, fit-width and zoom. Appearance includes light, sepia, eye-care and dark backgrounds, plus **reading dimness**: an in-app shade over the reading surface, not system screen brightness. All settings also remain available as component parameters.

Set `Immersive="false"` to show controls initially. `ShowToolbar="false"` disables the tap-to-show toolbar; an application can still open a built-in sheet with `ToggleSettings()`, `ToggleAppearance()` or `ToggleContents()`. `ShowControls()`, `HideControls()`, `ToggleControls()` and `ClosePanel()` provide explicit control.

## Local files

```razor
@using Miko.Components.Reader

<BookReader Source="@book" FontSize="22" LineSpacing="1.7f"
            Theme="ReaderTheme.Sepia" Texts="@ReaderTexts.Chinese" />

@code {
    private readonly FileReaderSource book = new("library/book.epub");

    protected override void OnDispose()
    {
        // The reader cancels its own requests on removal; the application owns the file source.
        _ = book.DisposeAsync();
    }
}
```

Use `ComicReader` with a `.cbz` or `.cbr` source:

```razor
<ComicReader Source="@comic" PageMode="ComicPageMode.Double"
             Direction="ReaderDirection.RightToLeft"
             Orientation="ReaderOrientation.Horizontal" FitWidth="true" />
```

The default height is 600 logical pixels. Pass a `Style` with your desired height, or `Height = Length.Percent(100)` inside a parent with a definite height, to fill the available space. The caller owns each source; when its lifetime ends, await `DisposeAsync()` where an asynchronous owner is available. Components do not dispose sources because a source can be shared.

| Format | Behavior |
| --- | --- |
| `.txt` | UTF-8 with BOM detection; configurable `TextEncoding`. Split into sections at newline/grapheme boundaries. |
| `.epub` | EPUB 2/3 container and spine order, NCX/nav directory labels, lazy chapter text. |
| `.pdf` | Original pages rendered on demand, including scanned pages. Optional extracted text reflow. |
| `.cbz` / `.cbr` | ZIP/RAR images loaded on demand, with natural ordering (`2.jpg` before `10.jpg`). |

EPUB is rendered as plain chapter text: publication CSS, embedded illustrations and fixed layouts are not reproduced. PDF original pages keep their fonts and layout; typography controls apply to text content. For PDF text reflow, construct the source with `new ReaderFileOptions { ReflowPdf = true }`; scans require OCR outside this component. DRM, encrypted books and password-protected archives are not supported.

## Remote APIs

Directory metadata and individual pages are separate operations. Implement `IReaderSource`, or use delegates with your existing `HttpClient`, authentication and API routes:

```csharp
using System.Net.Http.Json;
using Miko.Components.Reader;

// http is an application-owned client with BaseAddress/authentication already configured.
IReaderSource source = new DelegateReaderSource(
    async ct => await http.GetFromJsonAsync<ReaderDocument>("books/42/directory", ct)
        ?? throw new InvalidDataException("Empty directory response"),
    async (entry, ct) => new ReaderTextContent(
        await http.GetStringAsync($"books/42/chapters/{Uri.EscapeDataString(entry.Id)}", ct)));

// For comics, the page callback instead returns:
// new ReaderImageContent(await http.GetByteArrayAsync(pageUrl, ct))
```

The directory response can be `{ "title": "A book", "entries": [{ "id": "chapter-1", "title": "Chapter 1" }] }`. IDs must be unique and non-empty. A custom provider can map IDs to signed URLs, request additional metadata, or obtain image bytes through any protocol. No whole-file download is required. Contents and returned buffers must be immutable; providers must tolerate overlapping calls and honor cancellation when possible.

The controller fetches only the selected chapter/page (two for a spread). It caches at most six entries and 32 MiB of encoded/text content; larger entries are displayed without being cached. Old requests are cancelled, and late results are ignored after navigation, a source change or disposal. File limits are configured with `ReaderFileOptions`: `MaxEntryBytes`, `MaxEntries`, `TextChunkLength`, `PdfScale` and `MaxPdfPixels`.

## Book controls

| Parameter | Default | Meaning |
| --- | --- | --- |
| `FontFamily` / `FontSize` | `serif` / `20` | Font family/fallback chain and logical pixel size |
| `LineSpacing` | `1.6` | Line height multiplier |
| `ParagraphSpacing` | `12` | Space between paragraphs, in pixels |
| `PageMargin` | `24` | Page padding in pixels |
| `Theme` | `Light` | `Light`, `Sepia`, `Dark` |
| `EyeCare` | `false` | Soft green paper and dark text |
| `Background` / `Foreground` | unset | Optional colors overriding the theme |
| `Mode` | `Paged` | `Paged` or `Scroll` within the current chapter |

Pagination uses measured text lines and recalculates when typography or the viewport changes, keeping the current text position near the same passage. `PageIndex` and `PageCount` describe the current chapter's visual pages after layout. Previous/next first navigate within the chapter and then load the adjacent chapter; going back enters its final page. Scrolling supports the wheel and dragging. Cross-chapter navigation uses the toolbar or directory. Original PDF pages support double-tap zoom and dragging.

## Comic controls

`PageMode` selects `Single` or `Double`. `Direction` selects `LeftToRight` or `RightToLeft`, affecting horizontal page order and arrow/swipe navigation. `Orientation` selects `Horizontal` or `Vertical`. `FitWidth` displays the whole available width and lets tall images pan, starting at the top. Otherwise pages fit entirely inside the viewport.

Double click/tap toggles between normal size and `DoubleTapZoom` (default 2, bounded to 1–8). Dragging pans enlarged pages; dragging a fitted page by more than 40 pixels turns the spread along its configured axis. `ToggleZoom()`, `ResetZoom()` and `Zoom` are also available. The final odd page is displayed by itself.

## Shared API

`Source`, `Index` / `IndexChanged` (supports `@bind-Index`), `ShowToolbar`, `Immersive`, `BackRequested`, `Dimness`, `Texts`, `Class` and `Style` are common parameters. `Dimness` ranges from 0 to 0.75. `Index` always means the zero-based directory entry, so text repagination does not change the chapter index. `Texts` defaults to English; `ReaderTexts.Chinese` is included, and records support `with` overrides.

Call `NextAsync()`, `PreviousAsync()`, `GoToAsync(index)` or `RetryAsync()` on the UI thread. `Snapshot` exposes the state, directory, current index, content and error; `Error` reports provider and image decoding failures. `Controller` exposes the shared state machine for integrations; its `Changed` event may run on a background thread and must be dispatched before updating UI.

The progress slider previews while dragging and loads content only on release. Text progress approximates each chapter as an equal share of the book, since unloaded chapter lengths are unknown. Seeking within that share locates a visual page after layout; 100% reaches the final page. Image progress uses directory page indices.

Click/tap the reading surface to focus keyboard navigation. Page Up/Down and Space turn pages; arrows follow the configured comic direction/axis. No soft keyboard is requested.

## Runnable example

```sh
dotnet run --project examples/Components/MikoApp.Reader.Desktop
dotnet run --project examples/Components/MikoApp.Reader.Desktop -- /path/to/book.epub
dotnet run --project examples/Components/MikoApp.Reader.Desktop -- --verify /tmp/miko-reader-preview

# Android emulator or connected device; requires the Android workload/SDK.
dotnet build examples/Components/MikoApp.Reader.Android -t:Run

# macOS with Xcode and the iOS workload; select a booted simulator as needed.
dotnet build examples/Components/MikoApp.Reader.iOS -t:Run -r iossimulator-arm64
```

All three launch projects share a **bookshelf** with ebook/comic filters and four built-in sample titles. Selecting an item opens its reader; the back button returns to the shelf. A desktop file argument adds that file to the shelf, automatically choosing comics for CBZ/CBR. Use `--comic /path/to/book.pdf` to open a PDF as a comic instead. No file picker or platform storage permission is needed for the built-in samples.

The demo retains settings and directory index in memory when returning to the shelf and reopening a title; it does not persist them across application restarts or save the visual page within a text chapter. The verification mode exercises touch navigation/settings and writes shelf, book, comic, panel and desktop screenshots without opening a window. See the [sample README](https://github.com/chaldea/miko/blob/main/examples/Components/MikoApp.Reader/README.md) for launch and verification details.
