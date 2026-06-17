# MikoApp.AsyncDemo

This example demonstrates async/await support in Miko components, including:

- **Async lifecycle methods** (`OnInitializedAsync`)
- **Async event handlers** via Blazor-style `EventCallback`
- **Real HTTP communication** between a Miko client and ASP.NET Core API
- **Loading states** that don't block the render thread
- **Two demos**: one using Bootstrap components, one using raw HTML elements

## Structure

- **MikoApp.AsyncDemo.Api** — ASP.NET Core minimal API serving product data
- **MikoApp.AsyncDemo** — Shared Miko Razor components (product search UI)
  - `Pages/Home.razor` — search UI built with Bootstrap components (`<Button>`, `<Table>`)
  - `Pages/HomeNative.razor` — the same UI built with raw `<button>`, `<table>`, `<input>`
- **MikoApp.AsyncDemo.Desktop** — Desktop startup project

## How to Run

### 1. Start the API (in one terminal)

```bash
cd examples/MikoApp.AsyncDemo.Api
dotnet run
```

The API will start on `http://localhost:5000`.

### 2. Start the Miko Client (in another terminal)

```bash
cd examples/MikoApp.AsyncDemo.Desktop
dotnet run
```

The desktop window opens with two nav buttons at the top — "Bootstrap Demo" (`/`)
and "Native Demo" (`/native`) — that switch between the two implementations.

## Features Demonstrated

### Async Lifecycle

Both pages load initial product data in `OnInitializedAsync()`:

```csharp
protected override async Task OnInitializedAsync()
{
    await LoadProductsAsync(null);
}
```

The component renders immediately with a loading indicator, then re-renders
automatically when the async method completes.

### Async Event Handlers (Blazor-style EventCallback)

Component parameters use `EventCallback` — the same pattern as Blazor. The Razor
compiler accepts `Action`, `Func<Task>`, or another `EventCallback`, and binds the
callback to the **consuming** component so it re-renders when the handler completes:

```razor
@* Bootstrap component — OnClick is an EventCallback parameter *@
<Button Variant="primary" OnClick="SearchAsync">Search</Button>

@* Native element — @onclick is compiled to EventCallback<MouseEventArgs> *@
<button class="btn btn-primary" @onclick="SearchAsync">Search</button>

@code {
    // Both sync (Action) and async (Func<Task>) handlers are supported uniformly.
    private async Task SearchAsync()
    {
        await LoadProductsAsync(_searchQuery);
    }
}
```

The component re-renders **twice**: once after the synchronous portion (showing the
loading indicator) and again after the `await` completes (showing the results) —
matching Blazor's behavior. The render thread is never blocked.

### Defining EventCallback Parameters

To expose an event from your own component, use `EventCallback` (no payload) or
`EventCallback<T>` (with payload), exactly like Blazor:

```csharp
[Parameter] public EventCallback OnClick { get; set; }

private async Task HandleClick(MouseEventArgs _)
{
    await OnClick.InvokeAsync();  // awaits the consumer's handler AND re-renders it
}
```

### Thread Safety

All async continuations (`await` resumptions) automatically marshal back to the render
thread via `MikoSynchronizationContext` + `MikoDispatcher`. You can safely call
`StateHasChanged()` or mutate component state from async methods without explicit locking.

## Implementation Notes

- The API simulates a network delay (`Task.Delay`) to make async behavior visible
- The client shows a "Loading products..." message while fetching data
- Search filters products by name or category
- All state mutations happen on the render thread, avoiding race conditions

