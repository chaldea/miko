using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Ionic.Components;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonListHeaderTests : IonicComponentTestBase
{
    private static readonly RenderFragment Text = builder => builder.AddContent(0, "Header");

    private ComponentUnderTest RenderHeader(
        Action<ComponentParameterBuilder<IonListHeader>>? configure = null,
        RenderFragment? content = null,
        bool withStyles = false)
    {
        if (withStyles)
        {
            Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        }

        return Context.Render<IonListHeader>(parameters =>
        {
            parameters.Add(nameof(IonListHeader.ChildContent), content ?? Text);
            configure?.Invoke(parameters);
        });
    }

    private static Element Inner(ComponentUnderTest cut) =>
        cut.FindByClass("list-header-inner").ShouldHaveSingleItem();

    [Fact]
    public void IonListHeader_RendersInnerWrapperAndContent()
    {
        var cut = RenderHeader();

        cut.Root.Class.ShouldBe("md ion-list-header");
        cut.Root.Children.ShouldHaveSingleItem().ShouldBe(Inner(cut));
        cut.GetTextContent().ShouldContain("Header");
    }

    [Theory]
    [InlineData("full")]
    [InlineData("inset")]
    [InlineData("none")]
    public void IonListHeader_StampsLinesClass(string lines)
    {
        var cut = RenderHeader(parameters =>
            parameters.Add(nameof(IonListHeader.Lines), lines));

        cut.Root.ShouldHaveClass($"list-header-lines-{lines}");
    }

    [Theory]
    [InlineData("full", 1f, 0f)]
    [InlineData("inset", 0f, 1f)]
    [InlineData("none", 0f, 0f)]
    public void IonListHeader_LinesControlHostAndInnerBorders(
        string lines, float hostBorder, float innerBorder)
    {
        var cut = RenderHeader(
            parameters => parameters.Add(nameof(IonListHeader.Lines), lines),
            withStyles: true);

        cut.GetComputedStyle(cut.Root)!.BorderBottomWidth.Value.ShouldBe(hostBorder);
        cut.GetComputedStyle(Inner(cut))!.BorderBottomWidth.Value.ShouldBe(innerBorder);
    }

    [Fact]
    public void IonListHeader_ColorUsesPaletteFillAndContrast()
    {
        var cut = RenderHeader(
            parameters => parameters.Add(nameof(IonListHeader.Color), "danger"),
            withStyles: true);

        cut.Root.ShouldHaveClass("ion-color");
        cut.Root.ShouldHaveClass("ion-color-danger");
        var style = cut.GetComputedStyle(cut.Root)!;
        style.BackgroundColor.ShouldBe(IonicTheme.CreateMd().Danger);
        style.Color.ShouldBe(Color.White);
    }

    [Fact]
    public void IonListHeader_DirectLabelUsesFlexibleSlotSizing()
    {
        RenderFragment content = builder =>
        {
            builder.OpenComponent<IonLabel>(0);
            builder.AddComponentParameter(1, nameof(IonLabel.ChildContent), Text);
            builder.CloseComponent();
            builder.OpenComponent<IonButton>(2);
            builder.AddComponentParameter(3, nameof(IonButton.ChildContent),
                (RenderFragment)(button => button.AddContent(0, "+")));
            builder.CloseComponent();
        };

        var cut = RenderHeader(content: content, withStyles: true);
        var labelStyle = cut.GetComputedStyle(cut.FindByClass("ion-label").ShouldHaveSingleItem())!;

        labelStyle.FlexGrow.ShouldBe(1f);
        labelStyle.FlexShrink.ShouldBe(1f);
        labelStyle.FlexBasis.ShouldBe(Length.Auto);
    }
}
