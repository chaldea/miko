# Razor Components

Miko uses Razor as the UI language, compiled ahead-of-time by `Miko.Razor.Compiler`. The syntax matches Blazor. There is no runtime HTML/CSS parsing.

## A component

```razor
@page "/"
@namespace MyApp.Pages

<div class="home">
    <h1 class="home-title">Welcome to Miko</h1>
    <p class="home-subtitle">Your Razor app is running.</p>

    <div class="counter">
        <button class="counter-btn" @onclick="Increment">Clicked @_count times</button>
    </div>
</div>

@code {
    private int _count = 0;
    private void Increment() => _count++;
}
```

Supported Razor building blocks: `@expression` interpolation, `@if` / `@foreach` control flow, `@onclick` and other event directives, `@bind`, `@code` blocks, `@inject`, `@using`, `@inherits`, `@namespace`.

> Markup tags map to Miko elements — see [elements/index.md](elements/index.md) for the exact set (there is no `<textarea>`, `<form>`, `<section>`, etc.).

## Routing

```razor
@page "/about"
@page "/info"          @* a component may declare several routes *@
@namespace MyApp.Pages

<h1>About</h1>
```

Routes are discovered automatically when the app calls `UseGeneratedRoutes()`. Navigate in code:

```razor
@inject Miko.Routing.NavigationManager Navigation

<button @onclick="@(() => Navigation.NavigateTo("/"))">Home</button>
```

## Layouts

A layout inherits `LayoutComponentBase` and renders `@Body`:

```razor
@inherits Miko.Components.LayoutComponentBase
@namespace MyApp

<div class="layout">
    <div class="main-content">
        @Body
    </div>
</div>
```

Register: `builder.UseDefaultLayout<MainLayout>();`

## Parameters & child content

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

Use it:

```razor
<Card Title="Hello">
    <p>Body content projected into ChildContent.</p>
</Card>
```

`[CascadingParameter]` receives a `CascadingValue<T>` from an ancestor. `[Inject]` (or `@inject`) pulls a service from DI.

## Data binding

```razor
<input @bind="_name" />
<p>Hello, @_name</p>

@code { private string _name = ""; }
```

## Events

```razor
<button @onclick="Save">Save</button>
<button @onclick="@(() => Remove(item))">X</button>

@code {
    private void Save() { /* ... */ }
    private void Remove(Item i) { /* ... */ }
}
```

After a synchronous handler runs, the component re-renders automatically — no manual `StateHasChanged()` needed. See [events.md](events.md) for the full model.

## `EventCallback` (child → parent)

```razor
@* Child.razor *@
<button @onclick="OnClicked">@Label</button>
@code {
    [Parameter] public string Label { get; set; } = "";
    [Parameter] public EventCallback OnClicked { get; set; }
}
```

```razor
@* Parent.razor *@
<Child Label="Refresh" OnClicked="RefreshAsync" />
@code { private async Task RefreshAsync() { await ReloadData(); } }
```

## Lifecycle (Blazor-compatible)

| Method | When |
| --- | --- |
| `OnInitialized()` / `OnInitializedAsync()` | Once, when first ready. |
| `OnParametersSet()` / `OnParametersSetAsync()` | Each time parameters are set. |
| `OnDispose()` | When the component is discarded. |

Async hooks that return an incomplete `Task` render immediately with current state and re-render automatically when the task completes:

```razor
@inject HttpClient Http
@code {
    private Data? _data;
    protected override async Task OnInitializedAsync()
    {
        _data = await Http.GetFromJsonAsync<Data>("/api/data");
        // StateHasChanged is called automatically when the task completes.
    }
}
```

Call `StateHasChanged()` explicitly only to show intermediate state mid-method (e.g. a loading spinner before an `await`).

## Hot reload

`builder.EnableHotReload();` plus `MikoHotReloadHandler.Initialize(context.GetHotReloadService());` in the entry point. Editing a `.razor` file then updates the running app without restart.
