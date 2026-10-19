# Using the Engine Directly

You do not need Razor to use Miko. You can build the DOM, define a stylesheet, and render
a frame entirely in code — useful for headless render-to-image scenarios, tests, or
embedding the engine in your own host.

## Render a frame to a PNG

The example below builds a small tree and exports it as a PNG:

```csharp
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Miko.Styling.Selectors;
using Miko.Common;
using SkiaSharp;

// 1. Define a stylesheet
var styleSheet = new StyleSheet();
styleSheet.AddRule(new ClassSelector("container"), new Style
{
    Display = Display.Flex,
    FlexDirection = FlexDirection.Column,
    Padding = new Padding(Length.Px(20)),
    BackgroundColor = new Color(245, 245, 245)
});

// 2. Build the DOM tree
var root = new DivElement { Class = "container" };
root.AddChild(new H1Element { TextContent = "Hello Miko" });
root.AddChild(new ParagraphElement { TextContent = "A lightweight rendering engine." });

// 3. Render to a Skia canvas
using var surface = SKSurface.Create(new SKImageInfo(800, 600));
var canvas = surface.Canvas;
canvas.Clear(SKColors.White);

var engine = new MikoEngineBuilder().Build();
engine.Initialize(root, new List<StyleSheet> { styleSheet }, canvas, 800, 600);
engine.Render(canvas);

// 4. Export the frame
using var image = surface.Snapshot();
using var data = image.Encode(SKEncodedImageFormat.Png, 100);
using var stream = File.OpenWrite("output.png");
data.SaveTo(stream);
```

## Building the DOM

Elements live in `Miko.Core.DomElements`. Construct them and assemble a tree with
`AddChild` (which also marks the parent dirty), or via the `Children` initializer:

```csharp
var root = new DivElement
{
    Class = "container",
    Children =
    {
        new H1Element { TextContent = "Hello Miko" },
        new DivElement
        {
            Style = new Style { Display = Display.Flex },
            Children =
            {
                new ButtonElement { TextContent = "Button 1" },
                new ButtonElement { TextContent = "Button 2" }
            }
        }
    }
};
```

Each markup tag has an element class — the full list is in [Elements](../guide/elements.md).
In code every public property can be set, including those markup cannot reach
(`InputElement.Placeholder`, `Min` / `Max`, `OptionElement.Value`, `TdElement.ColSpan`, …), and
`CanvasElement` paints custom content through its `Paint` callback.

Members every element shares:

| Member | Purpose |
| --- | --- |
| `Id`, `Class`, `Style`, `TextContent` | Identity, classes, inline `Style`, and text (a facade over the child text nodes). |
| `Children`, `Parent`, `AddChild`, `RemoveChild` | The tree. Writes through `Children` (including collection initializers) are tracked like `AddChild`. |
| `FindById`, `FindByClass`, `FindByTagName`, `HasClass` | Lookups in the subtree. |
| `State`, `SetState`, `ClearState`, `HasState`, `IsDisabled` | `ElementState` flags (`Hover`, `Active`, `Focus`, `Disabled`); `IsDisabled` also checks ancestors. |
| `OnClick`, `OnInput`, … , `AddEventListener<T>`, `RemoveEventListener<T>` | Event handlers — see [Events](../guide/events.md#on-dom-elements). |

## Incremental updates

For interactive scenes, mutate the DOM, mark what changed, and repaint only the dirty
regions:

```csharp
element.TextContent = "Updated text";
engine.InvalidateElement(element);
engine.Update(canvas);            // incremental render of dirty regions only
```

## The `MikoEngine` API

| Method | Purpose |
| --- | --- |
| `Initialize(root, styleSheets, canvas, width, height)` | Set up the engine with a DOM root, stylesheets, target canvas, and viewport size. |
| `Render(canvas)` | Full render of the whole tree. |
| `Update(canvas)` | Incremental render — repaints only dirty regions. |
| `InvalidateElement(element)` | Mark an element dirty so the next `Update` repaints it. |

See the [Pipeline Overview](./overview.md) for what happens inside each call, and the
[Bootstrap console example](../examples.md#bootstrap) for a complete render-to-image program.
