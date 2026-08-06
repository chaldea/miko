using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// A flex container whose main size is indefinite (shrink-to-fit) ends up exactly as big as its
/// content, so there is never free space for <c>justify-content</c> to distribute. The placeholder
/// main size used while laying the line out — which <c>min-width</c> / <c>min-height</c> can raise
/// above zero — must not be mistaken for a real basis when the content is bigger than it: the
/// resulting negative free space would push every item outside the box the container is about to
/// grow into.
/// <para>
/// <c>row-reverse</c> / <c>column-reverse</c> hit this even at the default alignment, because they
/// mirror <c>flex-start</c> into <c>flex-end</c>. That is how Ionic's <c>ion-fab-list</c> threw its
/// buttons clean out of the list box for <c>side="start"</c> (row-reverse) and <c>side="top"</c>
/// (column-reverse) — issues/ion-fab.md problem 4.
/// </para>
/// </summary>
public class FlexShrinkToFitJustifyTests
{
    private const float ViewportW = 800f;
    private const float ViewportH = 600f;

    private readonly LayoutEngine _layoutEngine = new();

    private static StyleSheet Sheet(params StyleRule[] rules) => new() { Rules = rules.ToList() };

    private static StyleRule Rule(string cls, Style style) => new()
    {
        Selector = new ClassSelector(cls),
        Style = style,
    };

    private static LayoutBox? FindByClass(LayoutBox box, string cls)
    {
        if (box.Element.Class == cls) return box;
        foreach (var child in box.Children)
        {
            var found = FindByClass(child, cls);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>
    /// A positioned, shrink-to-fit flex container (the ion-fab-list shape: absolute, one inset per
    /// axis so it never gets inset-sized, plus the min-width/min-height that raise the placeholder)
    /// holding <paramref name="count"/> fixed 40×40 items.
    /// </summary>
    private LayoutBox LayoutList(FlexDirection direction, int count)
    {
        var list = new DivElement { Class = "list" };
        for (int i = 0; i < count; i++)
            list.AddChild(new DivElement { Class = "item" });
        var host = new DivElement { Class = "host" };
        host.AddChild(list);

        var sheet = Sheet(
            Rule("host", new Style
            {
                Position = Position.Relative,
                Width = Length.Px(400),
                Height = Length.Px(300),
            }),
            Rule("list", new Style
            {
                Display = Display.Flex,
                Position = Position.Absolute,
                Top = Length.Px(0),
                Left = Length.Px(0),
                FlexDirection = direction,
                AlignItems = AlignItems.Center,
                // Ionic sizes the list so it can stay centred on a small fab button; these raise the
                // placeholder main size above zero, which is what made the bug reachable.
                MinWidth = Length.Px(56),
                MinHeight = Length.Px(56),
            }),
            Rule("item", new Style { Width = Length.Px(40), Height = Length.Px(40) }));

        return _layoutEngine.Layout(host, new List<StyleSheet> { sheet }, ViewportW, ViewportH);
    }

    private static List<LayoutBox> Items(LayoutBox listBox)
    {
        var items = new List<LayoutBox>();
        Walk(listBox);
        return items;

        void Walk(LayoutBox box)
        {
            foreach (var child in box.Children)
            {
                if (child.Element.Class == "item") items.Add(child);
                else Walk(child);
            }
        }
    }

    private static void ShouldContainAllItems(LayoutBox listBox)
    {
        var list = listBox.BoxModel.Content;
        foreach (var item in Items(listBox))
        {
            var b = item.BoxModel.BorderBox;
            b.Left.ShouldBeGreaterThanOrEqualTo(list.Left - 0.5f);
            b.Right.ShouldBeLessThanOrEqualTo(list.Right + 0.5f);
            b.Top.ShouldBeGreaterThanOrEqualTo(list.Top - 0.5f);
            b.Bottom.ShouldBeLessThanOrEqualTo(list.Bottom + 0.5f);
        }
    }

    [Theory]
    [InlineData(FlexDirection.Row)]
    [InlineData(FlexDirection.RowReverse)]
    [InlineData(FlexDirection.Column)]
    [InlineData(FlexDirection.ColumnReverse)]
    public void ShrinkToFitContainer_KeepsEveryItemInsideItsBox(FlexDirection direction)
    {
        var root = LayoutList(direction, 4);

        ShouldContainAllItems(FindByClass(root, "list")!);
    }

    [Fact]
    public void RowReverse_PacksItemsFromTheRightEdgeInward()
    {
        var root = LayoutList(FlexDirection.RowReverse, 3);
        var list = FindByClass(root, "list")!;
        var items = Items(list);

        // The container is exactly as wide as the three 40px items.
        list.BoxModel.Content.Width.ShouldBe(120f, 0.5f);
        // Reverse order: the FIRST DOM item is rightmost.
        items[0].BoxModel.BorderBox.Left.ShouldBe(list.BoxModel.Content.Left + 80f, 0.5f);
        items[1].BoxModel.BorderBox.Left.ShouldBe(list.BoxModel.Content.Left + 40f, 0.5f);
        items[2].BoxModel.BorderBox.Left.ShouldBe(list.BoxModel.Content.Left, 0.5f);
    }

    [Fact]
    public void ColumnReverse_PacksItemsFromTheBottomEdgeUpward()
    {
        var root = LayoutList(FlexDirection.ColumnReverse, 3);
        var list = FindByClass(root, "list")!;
        var items = Items(list);

        list.BoxModel.Content.Height.ShouldBe(120f, 0.5f);
        items[0].BoxModel.BorderBox.Top.ShouldBe(list.BoxModel.Content.Top + 80f, 0.5f);
        items[1].BoxModel.BorderBox.Top.ShouldBe(list.BoxModel.Content.Top + 40f, 0.5f);
        items[2].BoxModel.BorderBox.Top.ShouldBe(list.BoxModel.Content.Top, 0.5f);
    }

    [Fact]
    public void Row_StillPacksItemsFromTheLeftEdge()
    {
        // The non-reversed direction must be untouched by the fix.
        var root = LayoutList(FlexDirection.Row, 3);
        var list = FindByClass(root, "list")!;
        var items = Items(list);

        items[0].BoxModel.BorderBox.Left.ShouldBe(list.BoxModel.Content.Left, 0.5f);
        items[2].BoxModel.BorderBox.Left.ShouldBe(list.BoxModel.Content.Left + 80f, 0.5f);
    }

    [Fact]
    public void MinSizeLargerThanContent_StillCentersWithJustifyContent()
    {
        // The complementary contract: when the min-size placeholder EXCEEDS the content there IS
        // real free space, and justify-content must still use it. (FlexLayoutTests covers the block
        // case; this pins the same behaviour for a shrink-to-fit out-of-flow container.)
        var item = new DivElement { Class = "item" };
        var list = new DivElement { Class = "list" };
        list.AddChild(item);
        var host = new DivElement { Class = "host" };
        host.AddChild(list);

        var sheet = Sheet(
            Rule("host", new Style { Position = Position.Relative, Width = Length.Px(400), Height = Length.Px(300) }),
            Rule("list", new Style
            {
                Display = Display.Flex,
                Position = Position.Absolute,
                Top = Length.Px(0),
                Left = Length.Px(0),
                FlexDirection = FlexDirection.Row,
                JustifyContent = JustifyContent.Center,
                MinWidth = Length.Px(100),
            }),
            Rule("item", new Style { Width = Length.Px(40), Height = Length.Px(40) }));

        var root = _layoutEngine.Layout(host, new List<StyleSheet> { sheet }, ViewportW, ViewportH);
        var listBox = FindByClass(root, "list")!;

        listBox.BoxModel.Content.Width.ShouldBe(100f, 0.5f);
        // (100 - 40) / 2 = 30 of leading free space.
        Items(listBox)[0].BoxModel.BorderBox.Left.ShouldBe(listBox.BoxModel.Content.Left + 30f, 0.5f);
    }
}
