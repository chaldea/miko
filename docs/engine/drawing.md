# Custom Drawing

Miko draws with SkiaSharp internally, but the rendering backend never appears in Miko's
public API. Components, platform hosts and apps draw through the abstractions in
`Miko.Graphics`. The backend is sealed inside the `Miko` package: its types cannot even be
named outside it, so it can be replaced later without breaking the code built on top.

| Type | What it is |
| --- | --- |
| `IDrawingContext` | A drawing target: state (save / restore, transforms, clips) and primitives (rectangles, circles, arcs, lines, text, images). |
| `IGraphicsFactory` | Creates resources: laid-out text (`TextBlock`), decoded images (`Image`), offscreen surfaces (`RenderSurface`). Registered in the container; also `MikoEngine.Graphics` and `IDrawingContext.Graphics`. |
| `TextBlock` | One line of text laid out with the engine's own pipeline (font fallback, bidi, shaping), so mixed scripts render correctly. |
| `Image` | A bitmap, an SVG (drawn as vectors at any size) or a GPU texture such as a video frame. |
| `RenderSurface` | A block of pixels to draw into: offscreen, GPU or a window framebuffer. `Context`, `EncodePng()`, `GetPixel()`. |
| `GraphicsDevice` | A GPU context, created by platform hosts from native handles. `GraphicsDevice.Software` renders on the CPU. |

## Painting a `<canvas>`

`CanvasElement.Paint` receives the drawing context, clipped to the element's content box,
and the content box itself. Draw inside that rectangle. Create resources once, then reuse
them across frames:

```csharp
using Miko.Common;
using Miko.Core.DomElements;
using Miko.Graphics;

TextBlock? title = null;
var canvas = new CanvasElement
{
    Paint = (context, box) =>
    {
        context.FillRoundRect(box, 8, Color.FromHex("#202428"));
        context.FillCircle(box.X + 20, box.Y + box.Height / 2, 8, Color.FromHex("#38cfa1"));

        title ??= context.Graphics.CreateText("Live 直播", new TextStyle("Arial", 16));
        float baseline = box.Y + (box.Height + title.Ascent - title.Descent) / 2;
        context.DrawText(title, box.X + 36, baseline, Color.White);
    }
};
```

A paint callback only draws. To change what is drawn, update your state and call
`MikoEngine.RequestRepaint()`. A paint-only change does not trigger a layout.

- **Coordinates** are logical pixels. The host has already applied the device density;
  `context.DeviceScale` reports it, which helps when choosing a bitmap resolution.
- **Text**: `CreateText` returns a cached layout for the same text and style. Draw it at
  a baseline; `Ascent`, `Descent` and `Width` describe its box. `StrokeText` draws the
  glyph outlines, for example as a contrasting border behind the fill.
- **Images**: `DecodeImage` accepts PNG, JPEG, WebP, GIF (first frame) and SVG. SVGs stay
  vectors. `DrawImage(image, rect, tint: color)` paints a single-color glyph in `color`,
  like `fill: currentColor`. Release the images you decode once you stop using them.
- The context belongs to the engine. Do not keep it after the callback returns.

## Rendering without a window

Render into a `RenderSurface` and read the result back, as tests and image exporters do:

```csharp
var engine = new MikoEngineBuilder().Build();
using var surface = engine.Graphics.CreateSurface(800, 600);
surface.Context.Clear(Color.White);

engine.Initialize(root, styleSheets, surface.Context, 800, 600);
engine.Render(surface.Context);

File.WriteAllBytes("frame.png", surface.EncodePng());
var pixel = surface.GetPixel(10, 10);
```

The engine only renders into contexts that Miko created (`RenderSurface.Context`, a
`WindowRenderTarget` surface, a `GraphicsDevice` surface). Your own `IDrawingContext`
implementation can still record a component's drawing calls in a test, but you cannot
pass it to `MikoEngine.Render`.

## Platform hosts and video backends

The hosts in `Miko.Windowing`, `Miko.Android`, `Miko.iOS` and `Miko.Simulator` create a
`GraphicsDevice` from native handles and give it to the engine:

| Host | Device | Frame surface |
| --- | --- | --- |
| Desktop / Simulator (OpenGL) | `GraphicsDevice.CreateOpenGl(getProcAddress)` | `device.CreateWindowRenderTarget().Acquire(width, height, framebuffer)` |
| Android | `GraphicsDevice.CreateOpenGl()` (EGL) | same as above |
| macOS / iOS (Metal) | `GraphicsDevice.CreateMetal(device, queue)` | `device.CreateMetalSurface(drawableTexture, width, height)` |

Set `MikoEngine.GraphicsDevice` to the device. A video backend's `IVideoFrameSource`
receives it in `AcquireCurrentFrame(GraphicsDevice device)` and returns an `Image`:
`device.WrapGlTexture` / `device.WrapMetalTexture` wrap decoder textures without copying,
`device.CreateImage` uploads CPU pixels, and `Nv12FrameComposer.Compose` converts NV12
planes on the GPU. The simplest option is to derive from `VideoFrameSourceBase` and push
`VideoFrameBuffer`s, which handles all of this for you.

An `IImageLoader` returns `Task<Image?>`; a custom loader decodes with
`IGraphicsFactory.DecodeImage`.
