using Miko.Common;
using Miko.Components;
using Miko.Ionic.Components;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Tests for <c>ion-list</c>. Covers the DOM contract (host classes, children), the
/// <c>lines</c> propagation (list-*-lines-* classes retarget only <c>.item-lines-default</c>
/// items, so an item's own <c>lines</c> wins — list.md.scss), and the <c>inset</c> list
/// (margin/radius + the last item's divider removal done by the <c>Build()</c> post-pass).
/// </summary>
public class IonListTests : IonicComponentTestBase
{
    // Renders `count` items; pass lines values to set item-level lines (null = unset).
    private static RenderFragment Items(params string?[] itemLines) => builder =>
    {
        int seq = 0;
        foreach (var lines in itemLines)
        {
            builder.OpenComponent<IonItem>(seq++);
            if (lines is not null)
            {
                builder.AddComponentParameter(seq++, nameof(IonItem.Lines), lines);
            }
            builder.AddComponentParameter(seq++, nameof(IonItem.ChildContent),
                (RenderFragment)(b => b.AddContent(0, "Item")));
            builder.CloseComponent();
        }
    };

    private ComponentUnderTest RenderList(Action<ComponentParameterBuilder<IonList>> configure)
        => Context.Render<IonList>(configure);

    private ComponentUnderTest RenderListWithStyles(Action<ComponentParameterBuilder<IonList>> configure)
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        return RenderList(configure);
    }

    private static float NativeBorderWidth(ComponentUnderTest cut, Miko.Core.Element item)
        => cut.GetComputedStyle(item.FindByClass("item-native").Single())!.BorderBottomWidth.Value;

    private static float InnerBorderWidth(ComponentUnderTest cut, Miko.Core.Element item)
        => cut.GetComputedStyle(item.FindByClass("item-inner").Single())!.BorderBottomWidth.Value;

    // ---- DOM contract ------------------------------------------------------

    [Fact]
    public void IonList_RendersHostWithListClasses_AndChildren()
    {
        var cut = RenderList(p => p.Add(nameof(IonList.ChildContent), Items(null, null)));

        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-list list-md");
        cut.FindByClass("ion-item").Count.ShouldBe(2);
        cut.GetTextContent().ShouldContain("Item");
    }

    [Fact]
    public void IonList_UsesIosClasses_OnIosPlatform()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = RenderList(p => p.Add(nameof(IonList.ChildContent), Items((string?)null)));

        cut.Root.Class.ShouldBe("ios ion-list list-ios");
    }

    // ---- lines classes -----------------------------------------------------

    [Fact]
    public void IonList_StampsNoLinesClass_ByDefault()
    {
        var cut = RenderList(p => p.Add(nameof(IonList.ChildContent), Items((string?)null)));

        cut.Root.ShouldNotHaveClass("list-lines-none");
        cut.Root.ShouldNotHaveClass("list-md-lines-none");
        cut.Root.ShouldNotHaveClass("list-lines-full");
    }

    [Theory]
    [InlineData("full", "list-lines-full", "list-md-lines-full")]
    [InlineData("inset", "list-lines-inset", "list-md-lines-inset")]
    [InlineData("none", "list-lines-none", "list-md-lines-none")]
    public void IonList_StampsLinesClasses_WhenLinesProvided(string lines, string linesClass, string modeLinesClass)
    {
        var cut = RenderList(p =>
        {
            p.Add(nameof(IonList.Lines), lines);
            p.Add(nameof(IonList.ChildContent), Items((string?)null));
        });

        cut.Root.ShouldHaveClass(linesClass);
        cut.Root.ShouldHaveClass(modeLinesClass);
    }

    [Fact]
    public void IonList_StampsIosLinesClass_OnIosPlatform()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = RenderList(p =>
        {
            p.Add(nameof(IonList.Lines), "none");
            p.Add(nameof(IonList.ChildContent), Items((string?)null));
        });

        cut.Root.ShouldHaveClass("list-lines-none");
        cut.Root.ShouldHaveClass("list-ios-lines-none");
    }

    // ---- lines styles ------------------------------------------------------

    [Fact]
    public void IonList_LinesNone_RemovesItemDivider()
    {
        var cut = RenderListWithStyles(p =>
        {
            p.Add(nameof(IonList.Lines), "none");
            p.Add(nameof(IonList.ChildContent), Items((string?)null));
        });

        NativeBorderWidth(cut, cut.FindByClass("ion-item").Single()).ShouldBe(0f);
    }

    [Fact]
    public void IonList_LinesFull_KeepsFullWidthItemDivider()
    {
        var cut = RenderListWithStyles(p =>
        {
            p.Add(nameof(IonList.Lines), "full");
            p.Add(nameof(IonList.ChildContent), Items((string?)null));
        });

        var item = cut.FindByClass("ion-item").Single();
        NativeBorderWidth(cut, item).ShouldBe(1f);
        InnerBorderWidth(cut, item).ShouldBe(0f);
    }

    [Fact]
    public void IonList_LinesInset_MovesItemDividerToItemInner()
    {
        var cut = RenderListWithStyles(p =>
        {
            p.Add(nameof(IonList.Lines), "inset");
            p.Add(nameof(IonList.ChildContent), Items((string?)null));
        });

        var item = cut.FindByClass("ion-item").Single();
        NativeBorderWidth(cut, item).ShouldBe(0f);
        InnerBorderWidth(cut, item).ShouldBe(1f);
    }

    [Fact]
    public void IonList_ItemLines_TakePriorityOverListLines_Full()
    {
        // list.md.scss targets only .item-lines-default — an item with its own lines keeps it.
        var cut = RenderListWithStyles(p =>
        {
            p.Add(nameof(IonList.Lines), "none");
            p.Add(nameof(IonList.ChildContent), Items("full"));
        });

        var item = cut.FindByClass("ion-item").Single();
        item.ShouldHaveClass("item-lines-full");
        NativeBorderWidth(cut, item).ShouldBe(1f);
    }

    [Fact]
    public void IonList_ItemLines_TakePriorityOverListLines_Inset()
    {
        var cut = RenderListWithStyles(p =>
        {
            p.Add(nameof(IonList.Lines), "none");
            p.Add(nameof(IonList.ChildContent), Items("inset"));
        });

        var item = cut.FindByClass("ion-item").Single();
        NativeBorderWidth(cut, item).ShouldBe(0f);
        InnerBorderWidth(cut, item).ShouldBe(1f);
    }

    [Fact]
    public void IonList_LinesNone_AppliesPerItem_OnlyToDefaultItems()
    {
        // First item follows the list (no divider); the second keeps its own full line.
        var cut = RenderListWithStyles(p =>
        {
            p.Add(nameof(IonList.Lines), "none");
            p.Add(nameof(IonList.ChildContent), Items(null, "full"));
        });

        var items = cut.FindByClass("ion-item");
        NativeBorderWidth(cut, items[0]).ShouldBe(0f);
        NativeBorderWidth(cut, items[1]).ShouldBe(1f);
    }

    // ---- inset -------------------------------------------------------------

    [Fact]
    public void IonList_Inset_StampsInsetClass_AndAppliesMarginAndRadius()
    {
        var cut = RenderListWithStyles(p =>
        {
            p.Add(nameof(IonList.Inset), true);
            p.Add(nameof(IonList.ChildContent), Items((string?)null));
        });

        cut.Root.ShouldHaveClass("list-inset");
        var style = cut.GetComputedStyle(cut.Root);
        style.ShouldNotBeNull();
        style.MarginLeft.ShouldBe(Length.Px(16));
        style.MarginTop.ShouldBe(Length.Px(16));
        style.BorderTopLeftRadius.ShouldBe(Length.Px(2)); // $list-inset-md-border-radius
    }

    [Fact]
    public void IonList_Inset_UsesIosRadius_OnIosPlatform()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = RenderListWithStyles(p =>
        {
            p.Add(nameof(IonList.Inset), true);
            p.Add(nameof(IonList.ChildContent), Items((string?)null));
        });

        var style = cut.GetComputedStyle(cut.Root);
        style.ShouldNotBeNull();
        style.BorderTopLeftRadius.ShouldBe(Length.Px(10)); // $list-inset-ios-border-radius
    }

    [Fact]
    public void IonList_Inset_RemovesDivider_FromLastItemOnly()
    {
        var cut = RenderListWithStyles(p =>
        {
            p.Add(nameof(IonList.Inset), true);
            p.Add(nameof(IonList.ChildContent), Items(null, null, null));
        });

        var items = cut.FindByClass("ion-item");
        items.Count.ShouldBe(3);

        // Ionic's .list-inset ion-item:last-of-type override — the Build() post-pass stamps the
        // last item; the others keep their divider.
        items[0].ShouldNotHaveClass("item-last-in-list");
        items[1].ShouldNotHaveClass("item-last-in-list");
        items[2].ShouldHaveClass("item-last-in-list");

        NativeBorderWidth(cut, items[0]).ShouldBe(1f);
        NativeBorderWidth(cut, items[2]).ShouldBe(0f);
        InnerBorderWidth(cut, items[2]).ShouldBe(0f);
    }

    [Fact]
    public void IonList_NotInset_LeavesLastItemDividerIntact()
    {
        var cut = RenderListWithStyles(p =>
            p.Add(nameof(IonList.ChildContent), Items(null, null)));

        var items = cut.FindByClass("ion-item");
        items[^1].ShouldNotHaveClass("item-last-in-list");
        NativeBorderWidth(cut, items[^1]).ShouldBe(1f);
    }
}
