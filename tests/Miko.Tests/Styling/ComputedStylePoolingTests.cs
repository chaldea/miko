using Miko.Animation;
using Miko.Common;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;
using SkiaSharp;
using System.Reflection;

namespace Miko.Tests.Styling;

/// <summary>
/// <see cref="ComputedStyle"/> 的实例池化（ISSUE-132）。
///
/// <para>单个 <c>ComputedStyle</c> 实测 <b>9336 字节</b>——它继承 <see cref="Style"/>（约 137 个
/// 属性槽）又遮蔽了约 133 个已解析属性。而每帧每个元素都要新建一个：87 个元素的 Ionic 页面
/// 一帧就是 812 KB，冲垮 Gen0 预算后被提升进 Gen2，拖动滑块时 G2 于是单调上涨。</para>
///
/// <para>这些对象逐帧抛弃，因此改为池化复用。<b>池化的全部风险都在「复位是否干净」</b>：
/// 少复位一个属性，就是上一帧的值悄悄渗进这一帧、且只在特定元素顺序下才显形。
/// 本文件因此以「复位后的实例必须与全新实例逐属性相等」为核心断言。</para>
/// </summary>
public class ComputedStylePoolingTests
{
    private static readonly MethodInfo RentMethod =
        typeof(ComputedStyle).GetMethod("Rent", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ReturnMethod =
        typeof(ComputedStyle).GetMethod("Return", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static ComputedStyle Rent() => (ComputedStyle)RentMethod.Invoke(null, null)!;
    private static void Return(ComputedStyle style) => ReturnMethod.Invoke(null, new object[] { style });

    /// <summary>
    /// 复位后的实例必须与「全新构造」的实例在<b>每一个</b>公开属性上相等。
    ///
    /// <para>这是池化正确性的总断言，用反射遍历<b>全部</b>属性而不是列举几个——正因为
    /// 手工清单必然与属性声明脱节，才要让测试自己去发现新增的属性。</para>
    /// </summary>
    [Fact]
    public void RecycledInstance_ShouldBeIndistinguishableFromFresh()
    {
        var dirty = Rent();

        // 尽量把各类存储都写脏：枚举、逻辑尺寸门面、物理边门面、颜色、长度、
        // 以及带副作用的 setter（ZIndex 会顺带置 HasZIndex）。
        dirty.Display = Display.Flex;
        dirty.WritingMode = WritingMode.VerticalRl;
        dirty.Direction = Direction.Rtl;
        dirty.Width = Length.Px(123);
        dirty.Height = Length.Px(45);
        dirty.MinWidth = Length.Px(7);
        dirty.MaxHeight = Length.Px(300);
        dirty.FlexGrow = 3f;
        dirty.FlexShrink = 7f;
        dirty.Opacity = 0.3f;
        dirty.MarginTop = Length.Px(9);
        dirty.PaddingLeft = Length.Px(8);
        dirty.Top = Length.Px(3);
        dirty.Color = Color.Red;
        dirty.BackgroundColor = Color.Blue;
        dirty.BorderTopColor = Color.Green;
        dirty.BorderTopWidth = Length.Px(4);
        dirty.FontSize = Length.Px(99);
        dirty.ZIndex = 42;                       // 副作用：HasZIndex = true
        dirty.OverflowX = Overflow.Scroll;
        dirty.Position = Position.Absolute;
        dirty.Vars = new Dictionary<string, VarValue>();
        dirty.Transitions.Add(new Transition { Property = "Opacity" });
        dirty.Animations.Add(new KeyframeAnimation { Name = "spin" });

        Return(dirty);
        var recycled = Rent();
        ReferenceEquals(recycled, dirty).ShouldBeTrue("池应复用同一实例，否则本用例没测到复位");

        var fresh = new ComputedStyle();
        var mismatches = new List<string>();

        foreach (var prop in typeof(ComputedStyle).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead) continue;

            object? a, b;
            try
            {
                a = prop.GetValue(recycled);
                b = prop.GetValue(fresh);
            }
            catch
            {
                continue;   // 索引器等取不到值的成员
            }

            // Transform / Transitions / Animations 是引用类型，Equals 按引用比较，
            // 两个实例天然不等；它们的「内容为空」由下面的专门用例断言。
            if (prop.Name is "Transform" or "Transitions" or "Animations") continue;

            if (!Equals(a, b))
                mismatches.Add($"{prop.Name}: recycled={a} fresh={b}");
        }

        mismatches.ShouldBeEmpty(
            "复位不干净会让上一帧的值渗进下一帧：\n" + string.Join("\n", mismatches));
    }

    /// <summary>
    /// <c>HasZIndex</c> 是 <c>ZIndex</c> setter 的副作用产物，且为私有 setter，
    /// 生成的复位代码看不见它——复位时必须显式收尾，否则每个定位元素都会谎称
    /// 「显式声明过 z-index」而错误地建立层叠上下文。
    /// </summary>
    [Fact]
    public void RecycledInstance_ShouldClearHasZIndex()
    {
        var dirty = Rent();
        dirty.ZIndex = 5;
        dirty.HasZIndex.ShouldBeTrue();

        Return(dirty);
        var recycled = Rent();

        recycled.HasZIndex.ShouldBeFalse("z-index:auto 与 z-index:0 的区分不能跨帧串味");
        recycled.ZIndex.ShouldBe(0);
    }

    /// <summary>
    /// <c>Transitions</c> / <c>Animations</c> 是懒初始化列表。若复位写成
    /// <c>Transitions = pristine.Transitions</c>，会触发模板的懒初始化并把<b>同一个</b>
    /// 列表实例分发给池中所有对象——两个元素的过渡定义就会互相污染。
    /// </summary>
    [Fact]
    public void RecycledInstances_ShouldNotShareLazyLists()
    {
        var first = Rent();
        first.Transitions.Add(new Transition { Property = "Opacity" });
        first.Animations.Add(new KeyframeAnimation { Name = "fade" });
        Return(first);

        var second = Rent();
        second.Transitions.ShouldBeEmpty("回收后的实例不得带着上一个使用者的过渡定义");
        second.Animations.ShouldBeEmpty();

        // 模板自身也不能被污染：它是所有复位的取值来源。
        new ComputedStyle().Transitions.ShouldBeEmpty();
        new ComputedStyle().Animations.ShouldBeEmpty();
    }

    /// <summary>
    /// 同一个实例可能被两个元素的布局盒引用（引擎的 <c>MapElementIdentityRecursive</c> 与
    /// 组件的 <c>TransferLayoutBox</c> 会跨代搬运布局盒）。若两边都归还，池里会出现同一实例
    /// 两份，随后被两个元素同时取用而相互覆写——必须挡住重复入池。
    /// </summary>
    [Fact]
    public void DoubleReturn_ShouldNotPutTheSameInstanceInThePoolTwice()
    {
        var style = Rent();
        Return(style);
        Return(style);

        var a = Rent();
        var b = Rent();

        ReferenceEquals(a, b).ShouldBeFalse("同一实例被取出两次会让两个元素共用一份计算样式");
    }

    /// <summary>
    /// 端到端：连续多帧重排后，池化不得改变任何布局结果。
    /// 这条是对「复位干净」的行为级复核——逐属性比较之外，再从渲染结果确认一遍。
    /// </summary>
    [Fact]
    public void RepeatedLayouts_ShouldStayCorrectWithPooling()
    {
        var sheet = new StyleSheet
        {
            Rules =
            {
                new StyleRule
                {
                    Selector = new ClassSelector("row"),
                    Style = new Style { Display = Display.Block, Height = Length.Px(30) }
                },
                new StyleRule
                {
                    Selector = new ClassSelector("tall"),
                    Style = new Style { Height = Length.Px(90) }
                },
            }
        };

        var page = new DivElement { Class = "page" };
        for (int i = 0; i < 10; i++)
            page.AddChild(new DivElement { Class = "row", TextContent = $"row {i}" });

        var engine = new MikoEngineBuilder().Build();
        using var surface = SKSurface.Create(new SKImageInfo(400, 800));
        engine.Initialize(page, new List<StyleSheet> { sheet }, surface.Canvas, 400, 800);
        engine.Render(surface.Canvas);

        // 反复在两种 class 之间切换：每帧都会回收上一帧的整批计算样式再重新取用。
        for (int frame = 0; frame < 30; frame++)
        {
            var target = page.Children[frame % 10];
            target.Class = frame % 2 == 0 ? "row tall" : "row";
            engine.InvalidateElement(target);
            engine.Render(surface.Canvas);
        }

        // 终态必须与「对同一棵终态树从零做一次全量布局」逐盒一致。
        var expectedTree = new DivElement { Class = "page" };
        for (int i = 0; i < 10; i++)
            expectedTree.AddChild(new DivElement { Class = "row", TextContent = $"row {i}" });
        expectedTree.Children[29 % 10].Class = "row";      // 最后一帧 frame=29 为奇数
        for (int i = 0; i < 10; i++)
        {
            // 复原前 29 帧留下的 class 状态。
            for (int frame = 0; frame < 30; frame++)
                if (frame % 10 == i) expectedTree.Children[i].Class = frame % 2 == 0 ? "row tall" : "row";
        }

        var expected = new LayoutEngine().Layout(expectedTree, new List<StyleSheet> { sheet }, 400, 800);
        var actual = engine.GetCurrentLayout()!;

        actual.Children.Count.ShouldBe(expected.Children.Count);
        for (int i = 0; i < actual.Children.Count; i++)
        {
            actual.Children[i].BoxModel.BorderBox.Height
                .ShouldBe(expected.Children[i].BoxModel.BorderBox.Height, 0.01,
                    $"row {i} height diverged after pooled relayouts");
            actual.Children[i].BoxModel.BorderBox.Y
                .ShouldBe(expected.Children[i].BoxModel.BorderBox.Y, 0.01,
                    $"row {i} position diverged after pooled relayouts");
        }
    }
}
