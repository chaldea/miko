using Miko.Common;
using Miko.Components;
using Miko.Ionic.Components;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonGridTests : IonicComponentTestBase
{
    private static RenderFragment Text(string value) => builder => builder.AddContent(0, value);

    [Fact]
    public void IonGrid_RendersDomAndChildRows()
    {
        var cut = Context.Render<IonGrid>(p => p.AddChildContent(builder =>
        {
            builder.OpenComponent<IonRow>(0);
            builder.CloseComponent();
        }));

        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-grid");
        cut.FindByClass("ion-row").ShouldHaveSingleItem();
    }

    [Fact]
    public void IonGrid_StampsFixedClass()
    {
        var cut = Context.Render<IonGrid>(p => p.Add(nameof(IonGrid.Fixed), true));

        cut.Root.Class.ShouldBe("md ion-grid grid-fixed");
    }

    [Fact]
    public void IonRow_RendersFlexRowClass()
    {
        var cut = Context.Render<IonRow>();

        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-row");
    }

    [Fact]
    public void IonCol_RendersDefaultClass()
    {
        var cut = Context.Render<IonCol>(p => p.AddChildContent(Text("A")));

        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-col");
        cut.GetTextContent().ShouldBe("A");
    }

    [Fact]
    public void IonCol_SizeAppliesInlineFlexBasis()
    {
        var cut = Context.Render<IonCol>(p => p.Add(nameof(IonCol.Size), "6"));

        cut.Root.Style.ShouldNotBeNull();
        cut.Root.Style!.FlexGrow.ShouldBe(0f);
        cut.Root.Style.FlexBasis.ShouldBe(Length.Percent(50));
        cut.Root.Style.Width.ShouldBe(Length.Percent(50));
        cut.Root.Style.MaxWidth.ShouldBe(Length.Percent(50));
    }

    [Fact]
    public void IonCol_OffsetPushPullApplyInlinePosition()
    {
        var cut = Context.Render<IonCol>(p =>
        {
            p.Add(nameof(IonCol.Offset), "3");
            p.Add(nameof(IonCol.Push), "2");
            p.Add(nameof(IonCol.Pull), "1");
        });

        cut.Root.Style!.MarginLeft.ShouldBe(Length.Percent(25));
        cut.Root.Style.Left.ShouldBe(Length.Percent(16.666668f));
        cut.Root.Style.Right.ShouldBe(Length.Percent(8.333334f));
    }

    [Fact]
    public void GridStyles_ApplyLayoutRules()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var grid = Context.Render<IonGrid>(p => p.Add(nameof(IonGrid.Fixed), true));
        var gridStyle = grid.GetComputedStyle(grid.Root)!;

        gridStyle.Display.ShouldBe(Display.Block);
        gridStyle.PaddingTop.ShouldBe(Length.Px(5));
        gridStyle.MaxWidth.ShouldBe(Length.Px(1140));

        var row = Context.Render<IonRow>();
        row.GetComputedStyle(row.Root)!.FlexWrap.ShouldBe(FlexWrap.Wrap);

        var col = Context.Render<IonCol>();
        col.GetComputedStyle(col.Root)!.FlexGrow.ShouldBe(1f);
    }
}
