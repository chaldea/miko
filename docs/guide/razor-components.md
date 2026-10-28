# Razor Components

Miko uses **Razor** as its UI authoring language. You write `.razor` components — the
same syntax you know from Blazor — and `Miko.Razor.Compiler` (a source generator)
compiles them ahead of time into native draw calls. There is no browser and no runtime
HTML/CSS parsing.

## A component

A component is a `.razor` file mixing markup with C# in a `@code` block:

```razor
@page "/"
@namespace MyApp.Pages

<div class="home">
    <h1 class="home-title">Welcome to Miko</h1>
    <p class="home-subtitle">Your Razor desktop app is up and running.</p>

    <div class="counter">
        <button class="counter-btn" @onclick="Increment">Clicked @_count times</button>
    </div>
</div>

@code {
    private int _count = 0;

    private void Increment()
    {
        _count++;
    }
}
```

The familiar Razor building blocks work as expected: `@expression` interpolation,
`@if` / `@foreach` control flow, `@onclick` and other event directives, `@bind` data
binding, `@code` blocks for component logic, and the `@inject`, `@using`, `@inherits` and
`@namespace` directives.

Markup tags compile to Miko's own elements, which have a fixed set of tags and attributes —
see [Elements](./elements.md). An attribute or tag outside that set is reported at compile time
(warnings RZ10100 and RZ10101).

## Parameters and child content

A component exposes parameters with `[Parameter]`; content written between its tags arrives in
`ChildContent`:

```razor
@* Card.razor *@
<div class="card">
    <div class="card-title">@Title</div>
    <div class="card-body">@ChildContent</div>
</div>

@code {
    [Parameter] public string Title { get; set; } = "";
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
```

```razor
<Card Title="Hello">
    <p>Body content projected into ChildContent.</p>
</Card>
```

A component with several `RenderFragment` parameters takes them as child elements named after
the parameter; the default content then goes in an explicit `<ChildContent>`:

```razor
<IonButton>
    <Start><IonIcon Icon="@Ionicons.Star" /></Start>
    <ChildContent>Favorite</ChildContent>
</IonButton>
```

`[CascadingParameter]` receives a value from an ancestor `<CascadingValue>`; `[Inject]` (or
`@inject`) resolves a service registered on `builder.Services`.

## Component identity and `@key`

When a component re-renders, the child components it renders again keep their instances, as in
Blazor. A kept child keeps its fields, its event subscriptions and any async work in progress.
`OnInitialized` runs only once per instance. The parent assigns the parameters it passes again
and `OnParametersSet` runs; a parameter the parent does not pass keeps its value. `OnDispose`
runs once, when a render no longer produces the child.

A child is kept when it has the same identity in the new render:

- the same place in the markup: a component inside an `@if` block and the one after the block
  are two different children, so a block appearing or disappearing does not hand one child's
  instance to the other;
- the same position among the children rendered from that place, for example inside an
  `@foreach` loop;
- the same `@key`, when one is given. A keyed child is matched by its key, wherever it is in the
  list.

Give list items a `@key` when the list can be reordered, or when items can be inserted or
removed anywhere but the end. Without a key the instances stay in place, so the state of a
removed item moves to the item that took its position:

```razor
@foreach (var todo in _todos)
{
    <TodoRow @key="todo.Id" Todo="todo" />
}
```

`@key` on an element does the same for the components inside it, and moves the element's focus
and caret with the item. Keys must be unique among the items of one list; a duplicate key throws
`InvalidOperationException`.

## Long lists: `Virtualize`

A list renders every item it holds, and every rendered item is styled, laid out and kept in memory.
For a list that can grow to hundreds or thousands of items, use `Virtualize` (namespace
`Miko.Components.Virtualization`): it renders only the items that can be seen plus a few on either
side, and stands in for the rest with two empty elements of their height. Building, styling, laying
out and scrolling the list then cost the same for ten thousand items as for twenty.

```razor
@using Miko.Components.Virtualization

<IonContent>
    <IonList>
        <Virtualize Items="@_users" ItemKey="@(user => user.Id)" ItemSize="72" Context="user">
            <IonItem>
                <IonLabel>
                    <h2>@user.Name</h2>
                    <p>@user.Email</p>
                </IonLabel>
            </IonItem>
        </Virtualize>
    </IonList>
</IonContent>
```

The API is Blazor's `Virtualize<TItem>`, plus `ItemKey`:

| Parameter | Default | |
|---|---|---|
| `Items` | | The items, an `ICollection<TItem>` in memory. Use this or `ItemsProvider`. |
| `ItemsProvider` | | Loads the items on demand (see below). Use this or `Items`. |
| template | | Written between the tags (`Context="name"` names the item, otherwise `context`), or in an `<ItemContent>` element. |
| `ItemSize` | `50` | The estimated height of an item in pixels, used until items have been measured. |
| `ItemKey` | index | A unique, stable key per item (Miko). See below. |
| `OverscanCount` | `3` | Items rendered above and below the visible ones. While the list scrolls, the 2 × `OverscanCount` extra items go mostly ahead of the scroll. |
| `MaxItemCount` | `100` | The most items rendered at once. |
| `Placeholder` | empty block | Shown for an item an `ItemsProvider` is still loading; `context` is a `PlaceholderContext` (`Index`, `Size`). |
| `EmptyContent` | | Shown when there are no items. |
| `SpacerElement` | `"div"` | Tag of the two spacer elements; `"tr"` when the items are table rows. |

`RefreshDataAsync()` (through `@ref`) runs the template again for the rendered items and, with an
`ItemsProvider`, asks it again for them.

- **Items can have any height.** `ItemSize` is only the starting estimate: every rendered item is
  measured, and items not rendered yet count as the average of the measured ones. When an item turns
  out taller or shorter than estimated above what is on screen — scrolling up through items never seen
  before — the list adjusts the scroll offset by the difference, so what is on screen stays where it is.
  The scrollbar's size can change slightly as estimates are replaced by measurements.
- **Put it inside the element that scrolls** — `IonContent`, or any box with `overflow-y: auto`. It can
  sit in an `IonList`: the items lay out as the list's children, as they would without `Virtualize`.
  Without a scrolling ancestor all of the list counts as visible and up to `MaxItemCount` items render.
- **Give `ItemKey` when items are inserted or removed anywhere but the end.** With a key, an item keeps
  its component instances and its measured height when other items are inserted before it, and
  inserting items above the visible ones (older messages at the top of a chat) does not move what is on
  screen. Without one, items are matched by index. Keys must be unique.
- **Items that stay on screen keep their elements while the list scrolls**: only the items scrolling in
  are built. After changing the data, re-render the component that holds the list (handlers do that by
  themselves) or call `RefreshDataAsync()` — the template then runs again for every rendered item.
- Selectors that depend on an item's position among its siblings (`:first-child`, `:last-child`,
  `:nth-child()`, `+`) see only the rendered items and the spacers around them. The list is vertical.

### Loading items on demand

With `ItemsProvider` the list asks for the range it is about to show and renders `Placeholder` for
items that are still loading:

```razor
<Virtualize TItem="Order" ItemsProvider="LoadOrders" ItemSize="56">
    <ItemContent Context="order">
        <IonItem><IonLabel>@order.Number</IonLabel></IonItem>
    </ItemContent>
    <Placeholder>
        <IonItem><IonSkeletonText Animated="true" /></IonItem>
    </Placeholder>
</Virtualize>

@code {
    private async ValueTask<ItemsProviderResult<Order>> LoadOrders(ItemsProviderRequest request)
    {
        var page = await _api.GetOrdersAsync(request.StartIndex, request.Count, request.CancellationToken);
        return new ItemsProviderResult<Order>(page.Items, page.TotalCount);
    }
}
```

The request's `CancellationToken` is cancelled when the user has scrolled on and a later request
replaced it. The list keeps the items of the last range it received and asks again when it scrolls
outside it, so cache in the provider if fetching is expensive; call `RefreshDataAsync()` when the
data behind it changes.

## Data binding

`@bind` keeps a field and an input in sync; `@bind-{Parameter}` does the same for a component
parameter that has a matching `{Parameter}Changed` callback:

```razor
<input @bind="_name" />
<p>Hello, @_name</p>

<IonToggle @bind-Checked="_enabled">Notifications</IonToggle>

@code {
    private string _name = "";
    private bool _enabled;
}
```

Which elements support `@bind` is listed in [Elements](./elements.md#reference).

## Preformatted text & code blocks

Ordinary markup whitespace is insignificant in Miko: the compiler trims the newlines and
indentation between tags, and the engine collapses runs of spaces like a browser with
`white-space: normal`. The `<pre>` element opts out of all of that — everything inside it
is preserved verbatim (spaces, tabs, and line breaks), matching `white-space: pre`:

```razor
<pre><code language="csharp">public class Demo
{
    public string Id { get; set; }
}</code></pre>
```

`<pre>` is a block element with a monospace font; `<code>` is its inline, semantic
companion (also monospace) and carries two Miko-specific attributes:

- `language` — the code language (`csharp`, `javascript`, `typescript`, `json`, `xml`,
  `css`, `sql`, `python`, `bash`, plus common aliases like `c#`, `js`, `py`, `sh`).
  Setting it enables syntax highlighting by default.
- `highlight` — set to `"false"` to render the code without coloring.

Highlighting is applied at paint time (the DOM keeps a single text node), uses a
built-in regex-driven tokenizer with a VS Light+ palette, and needs no external assets.
It is provided by the `Miko.Highlight.ISyntaxHighlighter` service — the built-in
`SyntaxHighlighter` is registered by default, and you can replace it (custom palette,
extra languages, a full parser) by registering your own implementation:

```csharp
builder.Services.AddSingleton<ISyntaxHighlighter, MyHighlighter>();
// or
builder.UseSyntaxHighlighter<MyHighlighter>();
```

## Routing

A component becomes a routable page when it declares a `@page` directive:

```razor
@page "/about"
@namespace MyApp.Pages

<h1>About</h1>
<p>Hello from Miko!</p>
```

Routes are discovered automatically — drop a new `.razor` file with a `@page` directive
into `Pages/` and it is wired up by the source generator when you call
`UseGeneratedRoutes()`. A component may declare several `@page` directives to answer on more
than one route.

Navigate programmatically by injecting `NavigationManager`:

```razor
@inject Miko.Routing.NavigationManager Navigation

<button @onclick="GoHome">Home</button>

@code {
    private void GoHome() => Navigation.NavigateTo("/");
}
```

## Layouts

A layout is a component that inherits `LayoutComponentBase` and renders `@Body` where
the routed page should appear:

```razor
@inherits Miko.Components.LayoutComponentBase

<div class="layout">
    <div class="main-content">
        @Body
    </div>
</div>
```

Register it as the default layout for every page in your app configuration with
`UseDefaultLayout<MainLayout>()`.

## Configuring the app — `MikoAppBuilder`

Apps are assembled with the fluent `MikoAppBuilder`. `CreateDefault()` registers the core
services (layout engine, render engine, dispatcher, navigation, etc.) and returns a
builder you configure:

```csharp
using Miko.Hosting;

var builder = MikoAppBuilder.CreateDefault();

builder.UseTitle("My App");
builder.UseSize(1024, 768);
builder.UseGeneratedRoutes();        // routes from .razor @page directives
builder.UseDefaultLayout<MainLayout>();
builder.EnableHotReload();

var app = builder.Build();           // returns a MikoAppContext
```

### Builder reference

| Method | Purpose |
| --- | --- |
| `CreateDefault()` | Static factory that registers the default services. |
| `UseTitle(string)` | Window / app title. |
| `UseSize(int width, int height)` | Initial viewport size in pixels. |
| `UseRootComponent(Func<Element>)` | Use an explicit root component factory. |
| `UseStyleSheets(List<StyleSheet>)` | Replace the stylesheet list. |
| `AddStyleSheet(StyleSheet)` | Append a single stylesheet. |
| `UseGeneratedRoutes()` | Wire up routes discovered from `@page` directives (generated extension). |
| `UseRouter(params Assembly[])` / `UseRouter(Action<Router>)` | Configure routing manually. |
| `UseDefaultLayout<TLayout>()` / `UseDefaultLayout(Type)` | Default layout for routed pages. |
| `UseFonts(Action<FontBuilder>)` | Register custom fonts (see [Fonts & Text](./fonts.md)). |
| `AddGlobalKeyHandler(Func<MikoKey, bool>)` | Handle global key presses. |
| `UseLogging(Action<ILoggingBuilder>)` | Configure logging. |
| `EnableHotReload()` | Turn on `.razor` hot reload. |
| `AddResourceAssembly(Assembly)` | Make an assembly's embedded resources loadable through `res://` paths. |
| `AddIonic(...)` | Register the Ionic component library (`Miko.Ionic`, see [Ionic components](../components/ionic/index.md)). |
| `AddDevTools(...)` | Runtime DOM / layout, console and resource inspector (`Miko.DevTools`). |
| `AddMikoNative()` | Register the native capability interfaces (`Miko.Native`, see [Native Capabilities](./native.md)). |
| `Build()` | Build the platform-agnostic `MikoAppContext`. |

`Services` is an `IServiceCollection`, so you can register your own services (HTTP
clients, view models, etc.) and inject them into components with `@inject`.

## Hosting the context

`Build()` returns a platform-agnostic `MikoAppContext`. Each platform host consumes it:

```csharp
// Desktop (Miko.Windowing)
context.RunDesktop();

// Android (Miko.Android)
SetContentView(MikoAndroidApp.CreateView(this, App.CreateContext));

// iOS (Miko.iOS)
Window.RootViewController = new MikoViewController(App.CreateContext());
```

## Hot reload

Call `EnableHotReload()` on the builder, then initialize the generated hot-reload handler
in your entry point:

```csharp
var context = App.CreateContext();
MikoHotReloadHandler.Initialize(context.GetHotReloadService());
context.RunDesktop();
```

With this in place, editing a `.razor` component updates the running app without a
restart.

## Next steps

- [Styling](./styling.md) — apply styles with stylesheets and selectors.
- [Events](./events.md) — handle pointer and keyboard input.
- [Async & Lifecycle](./async.md) — `OnInitializedAsync`, `EventCallback`, and data loading.
