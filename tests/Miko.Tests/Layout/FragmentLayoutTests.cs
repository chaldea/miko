using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// ISSUE-066 问题1：FragmentElement 在布局中是透明的（等价 CSS display:contents）。
/// 当一个多根页面被放进 Layout 的 body 时，承载多根的 FragmentElement 不应在布局树中
/// 产生任何包裹盒——其子节点的盒子直接成为父盒的子节点，使父元素的样式（如 flex）
/// 直接作用于页面的真实顶层元素。
/// </summary>
public class FragmentLayoutTests
{
    private readonly LayoutEngine _layoutEngine = new();

    private static List<StyleSheet> BlockForAll() => new()
    {
        new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new StyleRule { Selector = new TagSelector("div"), Style = new Style { Display = Display.Block } },
                new StyleRule { Selector = new TagSelector("fragment"), Style = new Style { Display = Display.Block } },
            }
        }
    };

    [Fact]
    public void ChildFragment_IsTransparent_ChildrenSpliceIntoParentBox()
    {
        // <div class="root"> <fragment> <div/><div/> </fragment> </div>
        // 期望布局树：root 盒下直接挂两个 div 盒，中间没有 fragment 盒。
        var a = new DivElement();
        var b = new DivElement();
        var fragment = new FragmentElement();
        fragment.AddChild(a);
        fragment.AddChild(b);
        var root = new DivElement { Class = "root" };
        root.AddChild(fragment);

        var layoutRoot = _layoutEngine.Layout(root, BlockForAll(), 800, 600);

        layoutRoot.Element.ShouldBe(root);
        layoutRoot.Children.Count.ShouldBe(2);
        layoutRoot.Children[0].Element.ShouldBe(a);
        layoutRoot.Children[1].Element.ShouldBe(b);
        // 布局树中不存在任何 FragmentElement 盒子。
        layoutRoot.Children.ShouldNotContain(box => box.Element.GetType() == typeof(FragmentElement));
    }

    [Fact]
    public void NestedFragments_AreFlattenedTransparently()
    {
        // 片段套片段也应被完全摊平。
        var leaf1 = new DivElement();
        var leaf2 = new DivElement();
        var inner = new FragmentElement();
        inner.AddChild(leaf1);
        inner.AddChild(leaf2);
        var outer = new FragmentElement();
        outer.AddChild(inner);
        var root = new DivElement();
        root.AddChild(outer);

        var layoutRoot = _layoutEngine.Layout(root, BlockForAll(), 800, 600);

        layoutRoot.Children.Count.ShouldBe(2);
        layoutRoot.Children[0].Element.ShouldBe(leaf1);
        layoutRoot.Children[1].Element.ShouldBe(leaf2);
    }

    [Fact]
    public void RootFragment_ActsAsPermittedWrapperBlock()
    {
        // 无 Layout 的多根页面：FragmentElement 是布局根。此时它充当 issue 所允许的
        // “自动创建的根包裹”，按普通块盒填满视口，其子节点在其内布局。
        var a = new DivElement();
        var b = new DivElement();
        var rootFragment = new FragmentElement();
        rootFragment.AddChild(a);
        rootFragment.AddChild(b);

        var layoutRoot = _layoutEngine.Layout(rootFragment, BlockForAll(), 800, 600);

        layoutRoot.Element.ShouldBe(rootFragment);
        layoutRoot.BoxModel.Content.Width.ShouldBe(800);
        layoutRoot.Children.Count.ShouldBe(2);
        layoutRoot.Children[0].Element.ShouldBe(a);
        layoutRoot.Children[1].Element.ShouldBe(b);
    }

    [Fact]
    public void FlexOnRoot_AppliesDirectlyToPageItems_AcrossTransparentFragment()
    {
        // ISSUE-066 问题1 的核心症状：.root 上的 flex 必须直接作用于页面的顶层元素。
        // 旧实现把多根页面包进不透明 div，flex 父级变成那个 div，导致 .root 的 flex 失效。
        // 现在 fragment 透明，.root 的两个 flex item 应横向并排（各 100px 宽，紧邻排列）。
        var item1 = new DivElement { Class = "item" };
        var item2 = new DivElement { Class = "item" };
        var fragment = new FragmentElement();
        fragment.AddChild(item1);
        fragment.AddChild(item2);
        var root = new DivElement { Class = "root" };
        root.AddChild(fragment);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new ClassSelector("root"),
                        Style = new Style { Display = Display.Flex, FlexDirection = FlexDirection.Row }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("item"),
                        Style = new Style { Display = Display.Block, Width = Length.Px(100), Height = Length.Px(60) }
                    },
                }
            }
        };

        var layoutRoot = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // 两个 item 是 .root 的直接 flex 子项，横向并排：第二个紧接第一个右侧。
        layoutRoot.Children.Count.ShouldBe(2);
        var box1 = layoutRoot.Children[0];
        var box2 = layoutRoot.Children[1];
        box1.Element.ShouldBe(item1);
        box2.Element.ShouldBe(item2);
        box1.BoxModel.Content.Width.ShouldBe(100);
        box2.BoxModel.Content.Left.ShouldBe(box1.BoxModel.Content.Right);
    }
}
