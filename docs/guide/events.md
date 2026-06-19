# Events

Miko dispatches input with a **DOM-style event model**, including a capture phase and a
bubbling phase. You can handle events either with Razor `@on...` directives or directly on
DOM elements.

## In Razor components

In `.razor` markup, attach handlers with the familiar `@onclick` (and similar)
directives:

```razor
<button @onclick="Increment">Clicked @_count times</button>

@code {
    private int _count;
    private void Increment() => _count++;
}
```

After an event handler runs, the component re-renders automatically — you do not need to
call `StateHasChanged()` yourself for synchronous handlers.

## On DOM elements

When building the DOM directly (see [Using the Engine Directly](/engine/direct-api)),
elements expose convenience handler properties and a generic `AddEventListener`.

Handlers use the `MikoEventHandler<T>` delegate, which takes a **single** event-args
argument:

```csharp
using Miko.Core.DomElements;
using Miko.Events;

var button = new ButtonElement { TextContent = "Click me" };

// Convenience property
button.OnClick = args =>
{
    Console.WriteLine($"Clicked at ({args.X}, {args.Y})");
};

// Generic listener (capture/bubbling participating)
button.AddEventListener<MouseEventArgs>("click", args =>
{
    // custom handling
});
```

Convenience handler properties on `Element` include:

| Property | Event args |
| --- | --- |
| `OnClick` | `MouseEventArgs` |
| `OnMouseEnter` / `OnMouseLeave` | `MouseEventArgs` |
| `OnMouseDown` / `OnMouseUp` | `MouseEventArgs` |
| `OnFocus` / `OnBlur` | `FocusEventArgs` |
| `OnChange` | `ChangeEventArgs` |
| `OnInput` | `InputEventArgs` |
| `OnScroll` | `ScrollEventArgs` |
| `OnKeyDown` | `KeyboardEventArgs` |

`MouseEventArgs` exposes the pointer coordinates (`X`, `Y`), among other fields.

## Capture and bubbling

Events propagate through the element tree like in a browser:

1. **Capture phase** — from the root down toward the target.
2. **Target phase** — the target element itself.
3. **Bubbling phase** — from the target back up toward the root.

A listener registered on an ancestor therefore sees events that originate on its
descendants as they bubble up.

## Global key handling

For app-wide shortcuts, register a global key handler on the app builder. Return `true`
to mark the key as handled:

```csharp
builder.AddGlobalKeyHandler(key =>
{
    if (key == MikoKey.Escape)
    {
        // handle escape
        return true;
    }
    return false;
});
```

## Async handlers

Event handlers may be `async`. See [Async & Lifecycle](/guide/async) for how Miko awaits
them and re-renders when the task completes.
