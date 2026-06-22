using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Platform.Resources;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Core;

/// <summary>
/// 图片加载生命周期：用 fake 加载器验证引擎发起加载、写入位图与内禀尺寸、
/// 通过跨线程失效驱动重排，并在元素离开树时回收加载记录——不依赖真实网络/磁盘。
/// </summary>
public class ImageLoadLifecycleTests : IDisposable
{
    private readonly SKBitmap _bitmap = new(800, 600);
    private readonly SKCanvas _canvas;

    public ImageLoadLifecycleTests() => _canvas = new SKCanvas(_bitmap);

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    private static List<StyleSheet> ImageSheet() => new()
    {
        new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new() { Selector = new TagSelector("div"), Style = new Style { Display = Display.Block } },
                new() { Selector = new TagSelector("img"), Style = new Style { Display = Display.Block } },
            }
        }
    };

    [Fact]
    public void Initialize_WithImage_RequestsLoadOnce()
    {
        var loader = new FakeImageLoader();
        var engine = new MikoEngine { ImageLoader = loader };
        var img = new ImageElement { Source = "https://x/a.png" };
        var root = new DivElement { Children = { img } };

        engine.Initialize(root, ImageSheet(), _canvas, 800, 600);
        // 多次渲染不应重复请求（按元素身份去重）。
        engine.Render(_canvas);
        engine.Render(_canvas);

        loader.Calls.ShouldBe(1);
        loader.Last.Raw.ShouldBe("https://x/a.png");
    }

    [Fact]
    public void ImageWithoutSource_DoesNotLoad()
    {
        var loader = new FakeImageLoader();
        var engine = new MikoEngine { ImageLoader = loader };
        var root = new DivElement { Children = { new ImageElement() } };

        engine.Initialize(root, ImageSheet(), _canvas, 800, 600);

        loader.Calls.ShouldBe(0);
    }

    [Fact]
    public void LoadCompletion_SetsBitmapAndIntrinsicSize_AndRelayoutUsesIt()
    {
        var loader = new FakeImageLoader();
        var engine = new MikoEngine { ImageLoader = loader };
        var img = new ImageElement { Source = "https://x/a.png" };
        var root = new DivElement { Children = { img } };

        engine.Initialize(root, ImageSheet(), _canvas, 800, 600);

        // 加载完成（模拟解码线程）：返回 80×40 位图。
        loader.Complete(new SKBitmap(80, 40));

        // 跨线程失效在下一次 Render 时排空并重排。
        engine.Render(_canvas);

        img.Bitmap.ShouldNotBeNull();
        img.IntrinsicWidth.ShouldBe(80);
        img.IntrinsicHeight.ShouldBe(40);
        FindBox(engine.GetCurrentLayout()!, img)!.BoxModel.Content.Width.ShouldBe(80);
    }

    [Fact]
    public void ImageRemovedFromTree_DropsTracking_AndAllowsReload()
    {
        var loader = new FakeImageLoader();
        var engine = new MikoEngine { ImageLoader = loader };
        var img = new ImageElement { Source = "https://x/a.png" };
        var root = new DivElement { Children = { img } };

        engine.Initialize(root, ImageSheet(), _canvas, 800, 600);
        loader.Calls.ShouldBe(1);

        // 移除后重新渲染：加载记录应被回收。
        root.Children.Clear();
        engine.Render(_canvas);

        // 重新加入同一元素：因记录已回收，可再次发起加载。
        loader.Reset();
        root.AddChild(img);
        engine.Render(_canvas);

        loader.Calls.ShouldBe(1);
    }

    private static LayoutBox? FindBox(LayoutBox box, Element element)
    {
        if (box.Element == element) return box;
        foreach (var child in box.Children)
        {
            var found = FindBox(child, element);
            if (found != null) return found;
        }
        return null;
    }

    // ---- fakes -------------------------------------------------------------

    private sealed class FakeImageLoader : IImageLoader
    {
        private TaskCompletionSource<SKBitmap?> _tcs = new();
        public int Calls { get; private set; }
        public MediaSource Last { get; private set; }

        public Task<SKBitmap?> LoadAsync(MediaSource source, CancellationToken ct = default)
        {
            Calls++;
            Last = source;
            return _tcs.Task;
        }

        public void Complete(SKBitmap? bitmap) => _tcs.TrySetResult(bitmap);

        public void Reset()
        {
            Calls = 0;
            _tcs = new TaskCompletionSource<SKBitmap?>();
        }
    }
}
