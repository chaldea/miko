# Skeleton Text

Skeleton text is a gray placeholder bar shown in place of text or media while the real content loads.

`IonSkeletonText` renders one full-width bar, 10px tall (its `line-height`) with a 4px top and bottom margin. It has no content of its own.

## Basic usage

Set the bar's `Width` through `Style` to imitate lines of different length.

<!-- miko:example examples/skeleton-text/Basic.razor -->
```razor
@* A bar is full width by default; set Width through Style for ragged "text" lines. *@
<IonSkeletonText />
<IonSkeletonText Style="@(new Style { Width = Length.Percent(80) })" />
<IonSkeletonText Style="@(new Style { Width = Length.Percent(60) })" />
```
<!-- /miko:example -->

## Animated

`Animated="true"` makes the bar pulse between its base gray and a darker one — Miko's stand-in for Ionic's sliding shimmer, which needs an animatable gradient position.

<!-- miko:example examples/skeleton-text/Animated.razor -->
```razor
<IonSkeletonText Animated="true" Style="@(new Style { Width = Length.Percent(70) })" />
<IonSkeletonText Animated="true" Style="@(new Style { Width = Length.Percent(40) })" />
```
<!-- /miko:example -->

## Loading placeholders

Swap skeleton rows for the real content when it arrives. Inside an [`IonAvatar`](./avatar.md) or [`IonThumbnail`](./thumbnail.md) the bar drops its margins and fills the media box. Unlike Ionic it does not take the container's corner radius, so in a round avatar it shows as a square; add a `BorderRadius` through `Style` if that matters.

<!-- miko:example examples/skeleton-text/List.razor -->
```razor
@* Placeholder rows while data loads, then the real list. Inside IonAvatar / IonThumbnail
   a skeleton bar fills the media box. *@
@if (_loading)
{
    <IonList>
        @for (var i = 0; i < 3; i++)
        {
            <IonItem>
                <Start>
                    <IonAvatar>
                        <IonSkeletonText Animated="true" />
                    </IonAvatar>
                </Start>
                <ChildContent>
                    <IonLabel>
                        <h3><IonSkeletonText Animated="true" Style="@(new Style { Width = Length.Percent(50) })" /></h3>
                        <p><IonSkeletonText Animated="true" Style="@(new Style { Width = Length.Percent(80) })" /></p>
                    </IonLabel>
                </ChildContent>
            </IonItem>
        }
    </IonList>
}
else
{
    <IonList>
        <IonItem><IonLabel>Loaded item</IonLabel></IonItem>
    </IonList>
}
<IonButton Size="small" OnClick="@(() => _loading = !_loading)">Toggle loading</IonButton>

@code {
    private bool _loading = true;
}
```
<!-- /miko:example -->

## API

<!-- miko:api IonSkeletonText -->
Every component also accepts `Class` (extra CSS classes) and `Style` (an inline `Style` object) — see [Conventions](./index.md#conventions).

### IonSkeletonText

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Animated` | `bool` | `false` | If true, the bar pulses between two grays once a second (Ionic `animated`; Ionic's sliding shimmer gradient is modeled as a color pulse). The pulse uses the built-in md / ios colors, not the app's `SkeletonText` tokens. |
<!-- /miko:api -->

## Styling

### Theme tokens

<!-- miko:tokens SkeletonText -->
Theme token `IonicTheme.SkeletonText` (`SkeletonTextToken`). Assign only the values you want to change — the rest keep the mode defaults below. App-wide: `builder.AddIonic(o => o.Theme.SkeletonText = new SkeletonTextToken { … })`; for one subtree: a `ConfigProvider`. See [Theming](./theming.md).

| Token | Type | md | ios |
| --- | --- | --- | --- |
| `Background` | `Color` | `rgba(0, 0, 0, 0.07)` | `rgba(0, 0, 0, 0.07)` |
| `BackgroundAnimated` | `Color` | `rgba(0, 0, 0, 0.13)` | `rgba(0, 0, 0, 0.13)` |
<!-- /miko:tokens -->

The animated pulse always uses the built-in `md` / `ios` colors: changing `Background` / `BackgroundAnimated` (or a stylesheet `BackgroundColor`) restyles a static bar but not the pulse.

### CSS classes

App stylesheet rules always win over Ionic's own rules (see [Theming](./theming.md#overriding-with-app-styles)), so these classes can be targeted directly from `GlobalStyles.cs`.

| Selector | Element |
| --- | --- |
| `.ion-skeleton-text` | The bar. Also carries the mode (`md` / `ios`). |
| `.skeleton-text-animated` | `Animated`. |
| `.in-media` | The bar is inside an `IonAvatar` / `IonThumbnail`. |

```csharp
// GlobalStyles.cs — rounded, taller placeholder bars.
[".ion-skeleton-text"] = new()
{
    LineHeight = Length.Px(14),
    BorderRadius = new BorderRadius(Length.Px(4)),
},
```
