# Known Limitations

This page lists the known boundaries and missing features in **Miko 1.0.0-preview.1**. These are not bugs — they describe what is intentionally out of scope for the first public preview release.

## Accessibility

Miko apps are **not accessible** in this release. Screen readers (TalkBack on Android, VoiceOver on iOS, UIA on Windows) see only a single canvas element. There is no semantic markup exposure, no focus order, and no announcements.

## Text and localization

### No RTL or bidi support

Right-to-left languages are not supported. The `Direction` property exists on `Style` but is not read by any layout or rendering code. Text always flows left-to-right.

### No complex text shaping

Miko does not use HarfBuzz or any advanced shaping engine. Complex scripts (Arabic, Indic languages, Thai, etc.) that require contextual glyph substitution or reordering will not render correctly. Basic Latin, CJK, and simple scripts work.

## CSS subset

Miko implements a subset of CSS. The following features are **not supported**:

### Selectors

- `:nth-child()`, `:nth-of-type()`, `:nth-last-child()`, `:nth-last-of-type()`
- `:has()`, `:is()`, `:where()`
- `::placeholder`

### Positioning and layout

- `position: sticky`
- Grid layout does not support `repeat()`, `minmax()`, or named grid areas / lines

### Visual effects

- `filter` and `backdrop-filter`
- `object-fit` and `object-position` for images
- `clip-path`
- `text-shadow`

### Styles are C# only

Stylesheets must be built in C# as `StyleSheet` objects. There is no `.css` file parsing or loading.

## Input and interaction

### Single-pointer touch only

Multi-touch is not supported. Android and iOS track only the primary pointer. Pinch-to-zoom and rotation gestures are not available.

### Soft keyboard on Android 8–10

On Android 8.0–10 (API 26–29) the soft keyboard may cover input fields: the window's `windowSoftInputMode` decides what happens. On Android 11 (API 30) and later and on iOS, the page is laid out above the keyboard and the focused field scrolls into view.

### iOS IME composition

The iOS text input implementation supports IME composition (Japanese kana, Chinese pinyin), but **tap-to-position inside text** and **selection handles** are not implemented. You can type and compose, but tapping inside the text does not move the caret (it stays where typing left it).

## Component libraries

### Miko.Native is preview

The `Miko.Native` package (camera, filesystem, clipboard, notifications, etc.) is a **preview API**. Interfaces and behavior may change between preview releases.

### IonDatetime is calendar-only

`IonDatetime` shows only the calendar view. Time pickers, min/max date constraints, and multi-date selection are not implemented. Localization support is limited to the invariant culture.

### No list virtualization

There is no built-in list virtualization. Long lists rendered with `IonInfiniteScroll` keep all loaded items in the DOM. Very long lists (thousands of items) will consume memory and slow down rendering.

## Publishing

### Desktop trimming and Native AOT

Trimming and Native AOT for desktop apps are not officially supported in 1.0.0-preview.1. The templates and examples do not enable trimming by default. iOS and Android use full AOT and are tested.

## What's next

These limitations describe the 1.0 preview boundaries. Many of them — accessibility, RTL, complex text shaping, sticky positioning, desktop trimming — are planned for future releases. See the [project repository](https://github.com/chaldea/miko) for the roadmap and issue tracker.
