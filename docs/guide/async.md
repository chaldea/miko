# Async & Lifecycle

Miko components support `async`/`await` for both **lifecycle methods** and **event
handlers**, following Blazor's model. Continuations are marshalled back to the render
thread automatically, so your code never touches the DOM off-thread.

## Component lifecycle

`ComponentBase` exposes synchronous and asynchronous lifecycle hooks:

| Method | When it runs |
| --- | --- |
| `OnInitialized()` | Once, when the component is first ready. |
| `OnInitializedAsync()` | Once, asynchronously, when the component is first ready. |
| `OnParametersSet()` | Each time the component receives parameters. |
| `OnParametersSetAsync()` | Each time the component receives parameters, asynchronously. |
| `OnDispose()` | When the component instance is discarded. |

If an async hook returns an incomplete `Task`, the component renders immediately with its
current state, and **re-renders automatically when the task completes** — so a slow data
load never blocks the first paint.

```csharp
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

## Async event handlers

Event handlers can be `async Task` methods. Miko awaits the task and re-renders when it
finishes:

```razor
<button @onclick="LoadAsync">Load</button>

@code {
    private async Task LoadAsync()
    {
        await Task.Delay(100);
        // StateHasChanged is called automatically after the await completes.
    }
}
```

## Showing intermediate state

To update the UI partway through an async method (for example, to show a loading
indicator), call `StateHasChanged()` explicitly:

```csharp
@code {
    private bool _isLoading;

    private async Task LoadAsync()
    {
        _isLoading = true;
        StateHasChanged();          // render the loading state now

        _data = await Http.GetFromJsonAsync<Data>("/api/data");

        _isLoading = false;
        // StateHasChanged is called automatically when the handler completes.
    }
}
```

## `EventCallback`

Components can expose Blazor-style `EventCallback` / `EventCallback<T>` parameters so
parent components can pass handlers — including async ones — to children:

```razor
<!-- Child.razor -->
<button @onclick="OnClicked">@Label</button>

@code {
    [Parameter] public string Label { get; set; } = "";
    [Parameter] public EventCallback OnClicked { get; set; }
}
```

```razor
<!-- Parent.razor -->
<Child Label="Refresh" OnClicked="RefreshAsync" />

@code {
    private async Task RefreshAsync()
    {
        await ReloadData();
    }
}
```

Invoking an `EventCallback` awaits the handler and triggers a re-render on the receiving
component, matching Blazor's "render twice" behavior (once before the await, once after).

## How it works (render-thread safety)

DOM mutation must happen on the render thread. Miko guarantees this for async code:

- A `MikoSynchronizationContext` is installed around rendering and event dispatch.
- When a component tracks a pending task, it captures that context; when the task
  completes, the continuation is **posted back to the render thread** via a
  `MikoDispatcher`.
- The dispatcher drains its queued actions at the start of each frame, so any
  `StateHasChanged()` triggered by a completed `await` is applied before the next paint —
  never mid-traversal.

The practical upshot: write ordinary `async`/`await` code, and Miko keeps DOM updates on
the render thread for you.

## See it in action

The [`examples/Async`](/examples#async) sample (`MikoApp.AsyncDemo`) demonstrates async
lifecycle loading, `EventCallback`, and real HTTP calls between a Miko client and an
ASP.NET Core API.
