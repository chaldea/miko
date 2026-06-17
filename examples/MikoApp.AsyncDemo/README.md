# MikoApp.AsyncDemo

This example demonstrates async/await support in Miko components, including:

- **Async lifecycle methods** (`OnInitializedAsync`)
- **Async event handlers** (button click with `async Task`)
- **Real HTTP communication** between a Miko client and ASP.NET Core API
- **Loading states** that don't block the render thread

## Structure

- **MikoApp.AsyncDemo.Api** — ASP.NET Core minimal API serving product data
- **MikoApp.AsyncDemo** — Shared Miko Razor components (product search UI)
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

The desktop window will open showing the product search interface.

## Features Demonstrated

### Async Lifecycle

The `Home.razor` component loads initial product data in `OnInitializedAsync()`:

```csharp
protected override async Task OnInitializedAsync()
{
    await LoadProductsAsync(null);
}
```

The component renders immediately with a loading indicator, then re-renders automatically when the async method completes.

### Async Event Handlers

The search button uses an async event handler:

```razor
<Button Variant="primary" OnClick="SearchAsync">Search</Button>

@code {
    private async Task SearchAsync()
    {
        await LoadProductsAsync(_searchQuery);
    }
}
```

When the button is clicked, the handler runs without blocking the render thread. UI animations and other interactions continue smoothly during the HTTP request.

### Thread Safety

All async continuations (`await` resumptions) automatically marshal back to the render thread via `MikoSynchronizationContext`. You can safely call `StateHasChanged()` or mutate component state from async methods without explicit locking.

## Implementation Notes

- The API simulates a 500ms network delay to make async behavior visible
- The client shows a "Loading products..." message while fetching data
- Search filters products by name or category
- All state mutations happen on the render thread, avoiding race conditions
