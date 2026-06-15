using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Core;

/// <summary>
/// Tests for <see cref="MikoEngine.SetSafeAreaInsets"/>: a change relays out the tree so the
/// root content is inset into the safe area, repeated identical values are a no-op, and the
/// root background color is exposed so the host can fill the system-bar bands to match.
/// </summary>
public class SafeAreaInsetsEngineTests : IDisposable
{
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;

    public SafeAreaInsetsEngineTests()
    {
        _bitmap = new SKBitmap(390, 844);
        _canvas = new SKCanvas(_bitmap);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    private static StyleSheet RootSheet(Color bg) => new()
    {
        Rules = new List<StyleRule>
        {
            new()
            {
                Selector = new ClassSelector("root"),
                Style = new Style
                {
                    Display = Display.Block,
                    Width = Length.Percent(100),
                    Height = Length.Percent(100),
                    BackgroundColor = bg,
                },
            },
        }
    };

    [Fact]
    public void SetSafeAreaInsets_RelaysOutRootIntoSafeArea()
    {
        var root = new DivElement { Class = "root" };
        var engine = new MikoEngine();
        engine.Initialize(root, new List<StyleSheet> { RootSheet(new Color(255, 255, 255)) }, _canvas, 390, 844);

        engine.SetSafeAreaInsets(new SafeAreaInsets(0, 48, 0, 24));
        engine.Render(_canvas);

        var layout = engine.GetCurrentLayout();
        layout.ShouldNotBeNull();
        layout!.BoxModel.Content.Top.ShouldBe(48f);
        layout.BoxModel.Content.Height.ShouldBe(844f - 48f - 24f);
    }

    [Fact]
    public void SetSafeAreaInsets_ExposedViaProperty()
    {
        var root = new DivElement { Class = "root" };
        var engine = new MikoEngine();
        engine.Initialize(root, new List<StyleSheet> { RootSheet(new Color(255, 255, 255)) }, _canvas, 390, 844);

        var insets = new SafeAreaInsets(12, 48, 8, 24);
        engine.SetSafeAreaInsets(insets);

        engine.SafeAreaInsets.ShouldBe(insets);
    }

    [Fact]
    public void SetSafeAreaInsets_SameValue_DoesNotRelayout()
    {
        var root = new DivElement { Class = "root" };
        var engine = new MikoEngine();
        engine.Initialize(root, new List<StyleSheet> { RootSheet(new Color(255, 255, 255)) }, _canvas, 390, 844);

        engine.SetSafeAreaInsets(new SafeAreaInsets(0, 48, 0, 24));
        engine.Render(_canvas);                  // apply relayout + clear dirty regions
        var before = engine.GetCurrentLayout();

        // Setting the identical value must not invalidate the root; Update only relays out when
        // dirty regions exist, so the layout-tree reference stays the same.
        engine.SetSafeAreaInsets(new SafeAreaInsets(0, 48, 0, 24));
        engine.Update(_canvas);
        engine.GetCurrentLayout().ShouldBeSameAs(before);
    }

    [Fact]
    public void SetSafeAreaInsets_DifferentValue_Relayouts()
    {
        var root = new DivElement { Class = "root" };
        var engine = new MikoEngine();
        engine.Initialize(root, new List<StyleSheet> { RootSheet(new Color(255, 255, 255)) }, _canvas, 390, 844);

        engine.SetSafeAreaInsets(new SafeAreaInsets(0, 48, 0, 24));
        engine.Render(_canvas);
        var before = engine.GetCurrentLayout();

        // A changed value invalidates the root, so the next Update produces a fresh layout tree.
        engine.SetSafeAreaInsets(new SafeAreaInsets(0, 60, 0, 30));
        engine.Update(_canvas);
        engine.GetCurrentLayout().ShouldNotBeSameAs(before);
        engine.GetCurrentLayout()!.BoxModel.Content.Top.ShouldBe(60f);
    }

    [Fact]
    public void GetRootBackgroundColor_ReturnsResolvedColor()
    {
        var root = new DivElement { Class = "root" };
        var engine = new MikoEngine();
        var bg = new Color(18, 52, 86);
        engine.Initialize(root, new List<StyleSheet> { RootSheet(bg) }, _canvas, 390, 844);

        var result = engine.GetRootBackgroundColor();
        result.ShouldNotBeNull();
        result!.Value.R.ShouldBe((byte)18);
        result.Value.G.ShouldBe((byte)52);
        result.Value.B.ShouldBe((byte)86);
    }

    [Fact]
    public void GetRootBackgroundColor_TransparentRoot_ReturnsNull()
    {
        var root = new DivElement { Class = "root" };
        var engine = new MikoEngine();
        // No background color rule -> root defaults to transparent.
        engine.Initialize(root, new List<StyleSheet>(), _canvas, 390, 844);

        engine.GetRootBackgroundColor().ShouldBeNull();
    }
}
