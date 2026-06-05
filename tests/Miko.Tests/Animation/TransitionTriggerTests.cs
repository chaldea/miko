using Miko.Animation;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Animation;

public class TransitionTriggerTests : IDisposable
{
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;

    public TransitionTriggerTests()
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

    [Fact]
    public void Transition_ShouldTrigger_WhenComputedStyleChanges()
    {
        var styleSheet = new StyleSheet();
        styleSheet.Add(new CssObject
        {
            [".box"] = new CssObject
            {
                Width = Length.Px(100),
                Height = Length.Px(50),
                MaxHeight = Length.Px(0),
                Transitions = [Transition.For(x => x.MaxHeight).Duration(0.5f).Linear()]
            },
            [".box.open"] = new CssObject
            {
                MaxHeight = Length.Px(200)
            }
        });

        var root = new DivElement();
        root.Style = new Style { Width = Length.Px(500), Height = Length.Px(500) };

        var box = new DivElement { Class = "box" };
        root.AddChild(box);

        var engine = new MikoEngine();
        engine.Initialize(root, [styleSheet], _canvas, 600, 600);

        // 初始状态：MaxHeight = 0
        engine.AnimationManager.HasActiveAnimations.ShouldBeFalse();

        // 改变 class 触发 transition
        box.Class = "box open";
        engine.Render(_canvas);

        engine.AnimationManager.HasActiveAnimations.ShouldBeTrue();
    }

    [Fact]
    public void Transition_ShouldInterpolate_MaxHeight()
    {
        var styleSheet = new StyleSheet();
        styleSheet.Add(new CssObject
        {
            [".box"] = new CssObject
            {
                Width = Length.Px(100),
                Height = Length.Px(50),
                MaxHeight = Length.Px(0),
                Transitions = [Transition.For(x => x.MaxHeight).Duration(1f).Linear()]
            },
            [".box.open"] = new CssObject
            {
                MaxHeight = Length.Px(200)
            }
        });

        var root = new DivElement();
        root.Style = new Style { Width = Length.Px(500), Height = Length.Px(500) };

        var box = new DivElement { Class = "box" };
        root.AddChild(box);

        var engine = new MikoEngine();
        engine.Initialize(root, [styleSheet], _canvas, 600, 600);

        // 触发 transition
        box.Class = "box open";
        engine.Render(_canvas);

        // 模拟 0.5 秒后（50% 进度，线性插值）
        engine.AnimationManager.Update(0.5f);

        // MaxHeight 应该在中间值附近
        box.Style.ShouldNotBeNull();
        box.Style!.MaxHeight!.Value.Value.ShouldBe(100f, 1f);
    }

    [Fact]
    public void Transition_ShouldComplete_AfterDuration()
    {
        var styleSheet = new StyleSheet();
        styleSheet.Add(new CssObject
        {
            [".box"] = new CssObject
            {
                Width = Length.Px(100),
                Height = Length.Px(50),
                MaxHeight = Length.Px(0),
                Transitions = [Transition.For(x => x.MaxHeight).Duration(0.5f).Linear()]
            },
            [".box.open"] = new CssObject
            {
                MaxHeight = Length.Px(200)
            }
        });

        var root = new DivElement();
        root.Style = new Style { Width = Length.Px(500), Height = Length.Px(500) };

        var box = new DivElement { Class = "box" };
        root.AddChild(box);

        var engine = new MikoEngine();
        engine.Initialize(root, [styleSheet], _canvas, 600, 600);

        box.Class = "box open";
        engine.Render(_canvas);

        // 完成 transition
        engine.AnimationManager.Update(0.6f);

        engine.AnimationManager.HasActiveAnimations.ShouldBeFalse();
        box.Style!.MaxHeight!.Value.Value.ShouldBe(200f, 1f);
    }

    [Fact]
    public void Transition_Padding_ShouldExpandToAllSides()
    {
        var styleSheet = new StyleSheet();
        styleSheet.Add(new CssObject
        {
            [".box"] = new CssObject
            {
                Width = Length.Px(100),
                Height = Length.Px(50),
                Padding = new Padding(0, 16),
                Transitions = [Transition.For(x => x.Padding).Duration(1f).Linear()]
            },
            [".box.open"] = new CssObject
            {
                Padding = 16
            }
        });

        var root = new DivElement();
        root.Style = new Style { Width = Length.Px(500), Height = Length.Px(500) };

        var box = new DivElement { Class = "box" };
        root.AddChild(box);

        var engine = new MikoEngine();
        engine.Initialize(root, [styleSheet], _canvas, 600, 600);

        box.Class = "box open";
        engine.Render(_canvas);

        // PaddingTop 从 0 到 16，应该触发 transition
        engine.AnimationManager.HasActiveAnimations.ShouldBeTrue();

        engine.AnimationManager.Update(0.5f);

        // PaddingTop 应该在中间值
        box.Style!.PaddingTop!.Value.Value.ShouldBe(8f, 1f);
    }

    [Fact]
    public void Transition_ShouldNotRetrigger_WhileActive()
    {
        var styleSheet = new StyleSheet();
        styleSheet.Add(new CssObject
        {
            [".box"] = new CssObject
            {
                Width = Length.Px(100),
                Height = Length.Px(50),
                MaxHeight = Length.Px(0),
                Transitions = [Transition.For(x => x.MaxHeight).Duration(1f).Linear()]
            },
            [".box.open"] = new CssObject
            {
                MaxHeight = Length.Px(200)
            }
        });

        var root = new DivElement();
        root.Style = new Style { Width = Length.Px(500), Height = Length.Px(500) };

        var box = new DivElement { Class = "box" };
        root.AddChild(box);

        var engine = new MikoEngine();
        engine.Initialize(root, [styleSheet], _canvas, 600, 600);

        box.Class = "box open";
        engine.Render(_canvas);

        // 模拟 0.3 秒
        engine.AnimationManager.Update(0.3f);
        float valueAt03 = box.Style!.MaxHeight!.Value.Value;

        // 再次 Render 不应重新触发 transition
        engine.Render(_canvas);
        engine.AnimationManager.Update(0.1f);

        // 值应该继续从 0.3s 的位置前进，而不是重新从 0 开始
        float valueAt04 = box.Style!.MaxHeight!.Value.Value;
        valueAt04.ShouldBeGreaterThan(valueAt03);
    }

    [Fact]
    public void Transition_ShouldNotRetrigger_AfterCompletion()
    {
        var styleSheet = new StyleSheet();
        styleSheet.Add(new CssObject
        {
            [".box"] = new CssObject
            {
                Width = Length.Px(100),
                Height = Length.Px(50),
                MaxHeight = Length.Px(0),
                Transitions = [Transition.For(x => x.MaxHeight).Duration(0.5f).Ease()]
            },
            [".box.open"] = new CssObject
            {
                MaxHeight = Length.Px(300)
            }
        });

        var root = new DivElement();
        root.Style = new Style { Width = Length.Px(500), Height = Length.Px(500) };

        var box = new DivElement { Class = "box" };
        root.AddChild(box);

        var engine = new MikoEngine();
        engine.Initialize(root, [styleSheet], _canvas, 600, 600);

        // 触发 transition
        box.Class = "box open";
        engine.Render(_canvas);
        engine.AnimationManager.HasActiveAnimations.ShouldBeTrue();

        // 完成 transition（模拟 MikoApp 的帧循环：先 Update 再 Render）
        engine.AnimationManager.Update(0.6f);
        engine.AnimationManager.HasActiveAnimations.ShouldBeFalse();

        // 下一帧 Render 不应触发新的 transition
        engine.Render(_canvas);
        engine.AnimationManager.HasActiveAnimations.ShouldBeFalse();
    }

    [Fact]
    public void Transition_FirstFrame_ShouldRenderStartValue_NotTargetValue()
    {
        // 验证触发 transition 的首帧渲染起始值，不会闪烁到目标值
        var styleSheet = new StyleSheet();
        styleSheet.Add(new CssObject
        {
            [".box"] = new CssObject
            {
                Width = Length.Px(100),
                Height = Length.Px(50),
                MaxHeight = Length.Px(300),
                BackgroundColor = Color.FromHex("f3f3f3"),
                Transitions = [Transition.For(x => x.MaxHeight).Duration(0.5f).Ease()]
            },
            [".box.closed"] = new CssObject
            {
                MaxHeight = Length.Px(0)
            }
        });

        var root = new DivElement();
        root.Style = new Style { Width = Length.Px(500), Height = Length.Px(500) };

        var box = new DivElement { Class = "box" };
        root.AddChild(box);

        var engine = new MikoEngine();
        engine.Initialize(root, [styleSheet], _canvas, 600, 600);

        // 切换到 closed 状态，触发 300 -> 0 的 transition
        box.Class = "box closed";
        engine.Render(_canvas);

        // transition 应该已触发
        engine.AnimationManager.HasActiveAnimations.ShouldBeTrue();

        // 首帧 inline style 应该是起始值 300，不是目标值 0
        box.Style.ShouldNotBeNull();
        box.Style!.MaxHeight!.Value.Value.ShouldBe(300f, 1f);
    }
}
