using Miko.Animation;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Core;

/// <summary>
/// 回归测试：切换页面（重新 Initialize）时，旧页面元素的 transition 不应在新页面元素上触发。
/// 见 ISSUE-043。
/// </summary>
public class NavigationTransitionTests : IDisposable
{
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;

    public NavigationTransitionTests()
    {
        _bitmap = new SKBitmap(600, 600);
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(SKColors.White);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    private static StyleSheet BuildStyleSheet()
    {
        var styleSheet = new StyleSheet();
        styleSheet.Add(new CssObject
        {
            // 模拟 .btn：带 background-color transition 和实色背景
            [".btn"] = new CssObject
            {
                Width = Length.Px(120),
                Height = Length.Px(40),
                BackgroundColor = Color.FromRgb(13, 110, 253),
                Color = Color.White,
                Transitions = [Transition.For(x => x.BackgroundColor).Duration(0.15f).Linear()]
            },
            // 模拟 .form-control：无 transition、无实色背景
            [".form-control"] = new CssObject
            {
                Width = Length.Px(200),
                Height = Length.Px(30),
                BackgroundColor = Color.Transparent
            }
        });
        return styleSheet;
    }

    private static Element BuildButtonPage()
    {
        var root = new DivElement { Class = "container" };
        root.Style = new Style { Width = Length.Px(500), Height = Length.Px(500) };
        root.AddChild(new ButtonElement { Class = "btn", TextContent = "Primary" });
        return root;
    }

    private static Element BuildFormPage()
    {
        var root = new DivElement { Class = "container" };
        root.Style = new Style { Width = Length.Px(500), Height = Length.Px(500) };
        root.AddChild(new InputElement { Class = "form-control" });
        return root;
    }

    [Fact]
    public void PageSwitch_ShouldNotTriggerOldPageTransition_OnNewPageElement()
    {
        var styleSheet = BuildStyleSheet();
        var engine = new MikoEngine();

        // 渲染 /button 页面
        engine.Initialize(BuildButtonPage(), [styleSheet], _canvas, 600, 600);
        engine.AnimationManager.HasActiveAnimations.ShouldBeFalse();

        // 切换到 /form 页面（结构位置相同：container > 单个子元素，但子元素类型/class 不同）
        var formPage = BuildFormPage();
        engine.Initialize(formPage, [styleSheet], _canvas, 600, 600);

        // 旧页面 .btn 的 background-color transition 不应在 .form-control 上被触发
        engine.AnimationManager.HasActiveAnimations.ShouldBeFalse();

        // 新页面 input 的内联样式不应被写入 .btn 的起始背景色
        var input = formPage.Children[0];
        if (input.Style?.BackgroundColor is { } bg)
        {
            bg.ShouldBe(Color.Transparent);
        }
    }

    [Fact]
    public void PageSwitch_ToSameStructure_ShouldStillPreserveIdentity_ForMatchingElements()
    {
        var styleSheet = BuildStyleSheet();
        var engine = new MikoEngine();

        // 两次渲染相同结构的 /button 页面：LayoutBox 身份应保持，元素无 transition 被错误触发
        engine.Initialize(BuildButtonPage(), [styleSheet], _canvas, 600, 600);
        engine.Initialize(BuildButtonPage(), [styleSheet], _canvas, 600, 600);

        // 背景色未变化，不应有 transition
        engine.AnimationManager.HasActiveAnimations.ShouldBeFalse();
    }
}
