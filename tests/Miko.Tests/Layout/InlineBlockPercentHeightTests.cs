using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// inline-block 容器的确定高度应作为子元素百分比高度的解析基准。
/// <para>
/// InlineLayout 在算出自身 contentHeight 之前只用确定宽度重排过一次行内流（高度传 null），
/// 之后再没有把算出的高度下传，因此 inline-block 内的 height:100% 永远退化为 auto——
/// 而 BlockLayout / FlexLayout 早已通过 childAvailableHeight 下传确定高度。表现为 Ionic
/// ion-toggle 的 .toggle-wrapper（height: inherit → 100%）在 inline-block 宿主内塌缩成
/// 内容高度。
/// </para>
/// <para>
/// 与 ISSUE-078 的边界保持一致：只有外部定型或自身显式高度算确定基准，min-height 抬升
/// 出来的 auto 高度不算（该约定由 <see cref="InlineBlockMinSizeTests"/> 覆盖）。
/// </para>
/// </summary>
public class InlineBlockPercentHeightTests
{
    private readonly LayoutEngine _layoutEngine = new();

    private static LayoutBox Find(LayoutBox box, string className)
    {
        if (box.Element.Class == className) return box;
        foreach (var child in box.Children)
        {
            var found = Find(child, className);
            if (found != null) return found;
        }
        return null!;
    }

    /// <summary>
    /// .host(display 可变, height:120) > .inner(flex, height:100%) > 文本。
    /// </summary>
    private LayoutBox LayoutTree(Display hostDisplay, Length hostHeight, Length innerHeight)
    {
        var inner = new DivElement { Class = "inner", TextContent = "x" };
        var host = new DivElement { Class = "host", Children = { inner } };
        var root = new DivElement { Class = "root", Children = { host } };

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new()
                    {
                        Selector = new ClassSelector("host"),
                        Style = new Style { Display = hostDisplay, Height = hostHeight },
                    },
                    new()
                    {
                        Selector = new ClassSelector("inner"),
                        Style = new Style { Display = Display.Flex, Height = innerHeight },
                    },
                }
            }
        };

        return _layoutEngine.Layout(root, styleSheets, 800, 600);
    }

    /// <summary>
    /// 核心回归：inline-block 宿主有确定高度时，子元素 height:100% 解析到该高度。
    /// 修复前 inner 退化为一行文本高度。
    /// </summary>
    [Fact]
    public void InlineBlock_DefiniteHeight_ResolvesChildPercentHeight()
    {
        var layoutRoot = LayoutTree(Display.InlineBlock, Length.Px(120), Length.Percent(100));

        Find(layoutRoot, "host").BoxModel.Content.Height.ShouldBe(120f);
        Find(layoutRoot, "inner").BoxModel.Content.Height.ShouldBe(120f,
            "inline-block 的确定高度应作为子元素 height:100% 的解析基准");
    }

    /// <summary>
    /// inline-block 与 block / flex 的行为应一致——后两者本就正确，此处锁定三者不再分叉。
    /// </summary>
    [Theory]
    [InlineData(Display.InlineBlock)]
    [InlineData(Display.Block)]
    [InlineData(Display.Flex)]
    public void DefiniteHeight_ResolvesChildPercentHeight_AcrossDisplayTypes(Display hostDisplay)
    {
        var layoutRoot = LayoutTree(hostDisplay, Length.Px(120), Length.Percent(100));

        Find(layoutRoot, "inner").BoxModel.Content.Height.ShouldBe(120f);
    }

    /// <summary>百分比高度（50%）同样按确定基准折算，而非只处理 100%。</summary>
    [Fact]
    public void InlineBlock_DefiniteHeight_ResolvesFractionalPercentHeight()
    {
        var layoutRoot = LayoutTree(Display.InlineBlock, Length.Px(120), Length.Percent(50));

        Find(layoutRoot, "inner").BoxModel.Content.Height.ShouldBe(60f);
    }

    /// <summary>
    /// 宿主高度为 auto 时不构成确定包含块，子元素 height:100% 仍退化为内容高度
    /// （CSS 规范行为，见 ISSUE-077 / ISSUE-078）。
    /// </summary>
    [Fact]
    public void InlineBlock_AutoHeight_LeavesChildPercentHeightIndefinite()
    {
        var layoutRoot = LayoutTree(Display.InlineBlock, Length.Auto, Length.Percent(100));

        var host = Find(layoutRoot, "host").BoxModel.Content.Height;
        var inner = Find(layoutRoot, "inner").BoxModel.Content.Height;

        // 两者都是内容高度（一行文本），而不是 0 塌缩。
        inner.ShouldBeGreaterThan(0f);
        inner.ShouldBe(host);
    }

    /// <summary>没有百分比高度子元素时行为不变（额外重排被跳过）。</summary>
    [Fact]
    public void InlineBlock_DefiniteHeight_DoesNotAffectAutoHeightChild()
    {
        var layoutRoot = LayoutTree(Display.InlineBlock, Length.Px(120), Length.Auto);

        var inner = Find(layoutRoot, "inner").BoxModel.Content.Height;

        inner.ShouldBeGreaterThan(0f);
        inner.ShouldBeLessThan(120f);
    }
}
