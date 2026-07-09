# Events

Miko uses a DOM-style event model with capture and bubbling phases. Handle events with Razor `@on...` directives or directly on `Element`s.

## In Razor

```razor
<button @onclick="Increment">Clicked @_count times</button>
@code {
    private int _count;
    private void Increment() => _count++;
}
```

After a synchronous handler runs the component re-renders automatically. Async handlers (`async Task`) are awaited and re-render on completion — see [razor.md](razor.md#lifecycle).

Common directives: `@onclick`, `@onmousedown`, `@onmouseup`, `@onmouseenter`, `@onmouseleave`, `@onfocus`, `@onblur`, `@onchange`, `@oninput`, `@onscroll`, `@onkeydown`.

## On DOM elements (direct API)

Handlers use `MikoEventHandler<T>` — a delegate taking a **single** event-args argument:

```csharp
using Miko.Core.DomElements;
using Miko.Events;

var button = new ButtonElement { TextContent = "Click me" };

// Convenience property
button.OnClick = args => Console.WriteLine($"({args.X}, {args.Y})");

// Generic listener (participates in capture/bubble)
button.AddEventListener<MouseEventArgs>("click", args => { /* ... */ });
```

Convenience handler properties on every `Element`:

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

`MouseEventArgs` exposes pointer coordinates `X`, `Y` (among others). All args derive from `MikoEventArgs`.

## Capture & bubbling

1. **Capture** — root → target.
2. **Target** — the target element.
3. **Bubble** — target → root.

A listener on an ancestor sees descendant events as they bubble up.

## Global key handling

```csharp
builder.AddGlobalKeyHandler(key =>
{
    if (key == MikoKey.Escape) { /* handle */ return true; }  // true = consumed
    return false;
});
```

`MikoKey` / `MikoKeyModifiers` are the platform-agnostic input enums (`Miko.Platform`).
