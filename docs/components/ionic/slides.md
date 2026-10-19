# Slides

Slides is a swipeable carousel that shows one slide at a time, with optional arrows, pagination bullets, a scrollbar, autoplay, looping, and a fade effect.

`IonSlides` is the carousel; each `IonSlide` inside it is one full-size slide that centers its content. This is a port of Ionic's legacy `ion-slides` (which wraps Swiper): only one slide per view is supported — Swiper's `slidesPerView`, free mode, zoom, and 3D effects are not ported, and there is no `options` object; the supported options are parameters.

## Basic usage

`IonSlides` fills its parent (`width` and `height` 100%), so give it — or its parent — a definite height. Slides follow a mouse or touch drag, resist past the first and last slide, and on release snap to the next slide when the drag covered half a slide or was a quick flick. A mostly vertical drag is left to the scroll container. `AllowTouchMove="false"` turns dragging off.

<!-- miko:example examples/slides/Basic.razor -->
```razor
@* IonSlides fills its parent (100% × 100%), so give it — or its parent — a height. *@
<IonSlides Style="@(new Style { Height = Length.Px(240) })">
    <IonSlide><h2>Welcome</h2></IonSlide>
    <IonSlide><h2>Swipe to continue</h2></IonSlide>
    <IonSlide><h2>All set</h2></IonSlide>
</IonSlides>
```
<!-- /miko:example -->

Put the `IonSlide`s directly in `IonSlides` (a `@foreach` is fine): the slide count is taken from the `IonSlide` elements at the top level of its content.

## Navigation, pagination, and scrollbar

`Navigation` adds previous/next arrows (disabled at the first/last slide unless `Loop` is set), `Pagination` adds one bullet per slide (tap a bullet to jump to it), and `Scrollbar` adds a bar whose thumb marks the current position.

<!-- miko:example examples/slides/Controls.razor -->
```razor
<IonSlides Navigation="true" Pagination="true" Scrollbar="true"
           Style="@(new Style { Height = Length.Px(240) })">
    <IonSlide>Slide 1</IonSlide>
    <IonSlide>Slide 2</IonSlide>
    <IonSlide>Slide 3</IonSlide>
</IonSlides>
```
<!-- /miko:example -->

## Active slide

`ActiveIndex` is the current slide (0-based; clamped to the slide count). `@bind-ActiveIndex` reports every change — drag, arrow, bullet, or autoplay — and moves the carousel when the page changes the value. Without a binding the carousel tracks its own position, but a re-render of the parent recreates it at the `ActiveIndex` you pass.

<!-- miko:example examples/slides/ActiveIndex.razor -->
```razor
@* @bind-ActiveIndex reports drags, arrows, bullets and autoplay, and moves the slides from code. *@
<IonSlides @bind-ActiveIndex="_index" Pagination="true"
           Style="@(new Style { Height = Length.Px(240) })">
    @foreach (var step in _steps)
    {
        <IonSlide><h2>@step</h2></IonSlide>
    }
</IonSlides>

<IonButton Disabled="@(_index == 0)" OnClick="@(() => _index--)">Back</IonButton>
<IonButton Disabled="@(_index == _steps.Length - 1)" OnClick="@(() => _index++)">Next</IonButton>

@code {
    private readonly string[] _steps = { "Account", "Profile", "Done" };
    private int _index;
}
```
<!-- /miko:example -->

`IonSlides` also has public `SlideToAsync`, `SlideNextAsync`, `SlidePrevAsync`, `StartAutoplay`, and `StopAutoplay` methods and `SlideCount`, `IsBeginning`, and `IsEnd` properties, but Miko's Razor compiler does not populate `@ref`, so drive the carousel through `ActiveIndex` instead.

## Loop and autoplay

`Autoplay` advances every `AutoplayDelay` milliseconds (default 3000); the timer restarts after every change and pauses while the user drags. Without `Loop`, autoplay stops at the last slide. `Loop` wraps past either end — the wrap animates back across the intermediate slides rather than continuing seamlessly as Swiper's cloned track does.

<!-- miko:example examples/slides/LoopAutoplay.razor -->
```razor
@* Advances every AutoplayDelay ms; Loop wraps from the last slide back to the first. *@
<IonSlides Autoplay="true" AutoplayDelay="4000" Loop="true" Pagination="true"
           Style="@(new Style { Height = Length.Px(240) })">
    <IonSlide>Featured 1</IonSlide>
    <IonSlide>Featured 2</IonSlide>
    <IonSlide>Featured 3</IonSlide>
</IonSlides>
```
<!-- /miko:example -->

## Fade effect

`Effect="fade"` stacks the slides and cross-fades between them instead of sliding the track. Unknown `Effect` values fall back to `"slide"`.

<!-- miko:example examples/slides/Fade.razor -->
```razor
<IonSlides Effect="fade" Navigation="true"
           Style="@(new Style { Height = Length.Px(240) })">
    <IonSlide>Morning</IonSlide>
    <IonSlide>Afternoon</IonSlide>
    <IonSlide>Evening</IonSlide>
</IonSlides>
```
<!-- /miko:example -->

Arrow, bullet, autoplay, and `ActiveIndex` changes animate over a fixed 300ms (as does the fade). `Speed` only sets the duration of the settle animation after a drag is released.

## API

<!-- miko:api IonSlides IonSlide -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonSlides

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `ActiveIndex` | `int` | `0` | Index of the active slide shown in the viewport. Mirrors Swiper's `initialSlide` / `activeIndex`; supports two-way binding via [`ActiveIndexChanged`](#ionslides). Two-way bindable: `@bind-ActiveIndex`. |
| `AllowTouchMove` | `bool` | `true` | If false, the slides cannot be dragged (Swiper's `allowTouchMove`). |
| `Autoplay` | `bool` | `false` | If true, advance automatically every [`AutoplayDelay`](#ionslides) milliseconds (Swiper's `autoplay`). Inert when no `MikoDispatcher` is available (a bare unit test), since there is no render loop to marshal the tick onto. |
| `AutoplayDelay` | `int` | `3000` | Milliseconds between automatic advances (Swiper's `autoplay.delay`). |
| `Effect` | `string` | `"slide"` | The transition effect: `"slide"` (default, the track slides horizontally) or `"fade"` (slides stack and cross-fade). Unknown values fall back to `"slide"`. |
| `Loop` | `bool` | `false` | If true, advancing past the last slide wraps to the first and vice versa (Swiper's `loop`), and the arrows are never disabled. The wrap animates back across the intermediate slides; there is no seamless cloned track as in Swiper. |
| `Navigation` | `bool` | `false` | If true, render the previous/next navigation arrows (Swiper's `navigation`). Unless [`Loop`](#ionslides) is set, the arrow at the first / last slide is disabled. |
| `Pagination` | `bool` | `false` | If true, render the pagination bullets (Swiper's `pagination`). |
| `Scrollbar` | `bool` | `false` | If true, render the scrollbar (Swiper's `scrollbar`). |
| `Speed` | `int` | `300` | Duration in milliseconds of the settle animation after a drag is released (Swiper's `speed`). Arrow, bullet, autoplay and [`ActiveIndex`](#ionslides) changes, and the fade cross-fade, use the stylesheet's fixed 300 ms transition instead. |

| Event | Type | Description |
| --- | --- | --- |
| `ActiveIndexChanged` | `EventCallback<int>` | Raised when the active slide changes, whether by drag, arrow, bullet or autoplay. |

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |

### IonSlide

| Slot | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | Content placed between the component's tags. |
<!-- /miko:api -->

## Styling

### Theme tokens

<!-- miko:tokens Slides -->
Theme token `IonicTheme.Slides` (`SlidesToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.Slides = new SlidesToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `BulletBackground` | `Color` | `#333333` | `#333333` |
| `BulletBackgroundActive` | `Color` | `#0054e9` | `#0054e9` |
| `NavigationColor` | `Color` | `#0054e9` | `#0054e9` |
| `ScrollBarBackground` | `Color` | `rgba(0, 0, 0, 0.1)` | `rgba(0, 0, 0, 0.1)` |
| `ScrollBarBackgroundActive` | `Color` | `rgba(0, 0, 0, 0.5)` | `rgba(0, 0, 0, 0.5)` |
<!-- /miko:tokens -->

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.swiper-container` | The `IonSlides` host (also `slides-md` / `slides-ios`, `swiper-horizontal`, and the mode class). |
| `.swiper-fade` | `Effect="fade"`. |
| `.swiper-navigation-disabled` | `Navigation` is off. |
| `.swiper-wrapper` | The track that moves; also carries `swiper-wrapper-at-{index}`. |
| `.swiper-slide` | An `IonSlide` (centers its content, 18px text). `swiper-slide-active` marks the visible slide under the fade effect. |
| `.swiper-button-prev`, `.swiper-button-next` | The arrows; `.swiper-button-disabled` at an end. |
| `.swiper-pagination`, `.swiper-pagination-bullet`, `.swiper-pagination-bullet-active` | The bullets. |
| `.swiper-scrollbar`, `.swiper-scrollbar-drag` | The scrollbar and its thumb. |

```csharp
// GlobalStyles.cs — left-aligned, padded slide content: <IonSlides Class="onboarding">…</IonSlides>
[".swiper-container.onboarding .swiper-slide"] = new()
{
    JustifyContent = JustifyContent.FlexStart,
    TextAlign = TextAlign.Left,
    Padding = new Padding(24),
},
```
