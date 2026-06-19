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

var engine = new MikoEngine();
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

Available element types include containers (`DivElement`, `SpanElement`,
`ParagraphElement`), headings (`H1Element`–`H6Element`), interactive elements
(`ButtonElement`, `InputElement`, `SelectElement`), media (`ImageElement`), lists
(`UlElement` / `OlElement` / `LiElement`), and tables (`TableElement` and friends).

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

See the [Pipeline Overview](/engine/overview) for what happens inside each call, and the
[Bootstrap console example](/examples#bootstrap) for a complete render-to-image program.
