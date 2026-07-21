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
binding, and `@code` blocks for component logic.

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
`UseGeneratedRoutes()`.

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
| `UseFonts(Action<FontBuilder>)` | Register custom fonts (see [Fonts & Text](/guide/fonts)). |
| `AddGlobalKeyHandler(Func<MikoKey, bool>)` | Handle global key presses. |
| `UseLogging(Action<ILoggingBuilder>)` | Configure logging. |
| `EnableHotReload()` | Turn on `.razor` hot reload. |
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

- [Styling](/guide/styling) — apply styles with stylesheets and selectors.
- [Events](/guide/events) — handle pointer and keyboard input.
- [Async & Lifecycle](/guide/async) — `OnInitializedAsync`, `EventCallback`, and data loading.
