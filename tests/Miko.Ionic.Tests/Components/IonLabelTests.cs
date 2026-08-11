using Miko.Common;
using Miko.Components;
using Miko.Ionic.Components;
using Miko.Platform;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonLabelTests : IonicComponentTestBase
{
    [Fact]
    public void IonLabel_RendersWithCorrectClass()
    {
        // Act
        var cut = Context.Render<IonLabel>();

        // Assert - DOM structure
        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-label");
    }

    [Fact]
    public void IonLabel_HasCorrectDOMStructure()
    {
        // Act
        var cut = Context.Render<IonLabel>();

        // Assert - DOM structure is the component contract
        var elements = cut.GetAllElements();
        elements.Count.ShouldBe(1); // Only the root div
        elements[0].TagName.ShouldBe("div");
        elements[0].Class.ShouldBe("md ion-label");
    }

    [Fact]
    public void IonLabel_WithColor_StampsIonColorClasses()
    {
        // Ionic's createColorClasses stamps both the `ion-color` marker and the `ion-color-*`
        // palette class (label.tsx @Prop color).
        var cut = Context.Render<IonLabel>(parameters =>
            parameters.Add(nameof(IonLabel.Color), "primary"));

        cut.Root.Class.ShouldBe("md ion-label ion-color ion-color-primary");
    }

    [Fact]
    public void IonLabel_WithoutColor_StampsNoColorClasses()
    {
        var cut = Context.Render<IonLabel>();

        cut.Root.ShouldNotHaveClass("ion-color");
    }

    [Theory]
    [InlineData("fixed", "label-fixed")]
    [InlineData("stacked", "label-stacked")]
    [InlineData("floating", "label-floating")]
    public void IonLabel_WithPosition_StampsLabelPositionClass(string position, string expected)
    {
        // label.tsx: class={`label-${position}`} when position !== undefined.
        var cut = Context.Render<IonLabel>(parameters =>
            parameters.Add(nameof(IonLabel.Position), position));

        cut.Root.ShouldHaveClass(expected);
    }

    [Fact]
    public void IonLabel_WithoutPosition_StampsNoLabelPositionClass()
    {
        var cut = Context.Render<IonLabel>();

        cut.Root.ShouldNotHaveClass("label-");
    }

    [Theory]
    [InlineData("primary")]
    [InlineData("secondary")]
    [InlineData("tertiary")]
    [InlineData("success")]
    [InlineData("warning")]
    [InlineData("danger")]
    [InlineData("light")]
    [InlineData("medium")]
    [InlineData("dark")]
    public void IonLabel_Color_RecolorsTextToPaletteBase_Md(string colorName)
    {
        // label.scss :host(.ion-color) { color: current-color(base) }. Before this was ported the
        // ion-color-* class matched no rule, so Color was a silent no-op.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonLabel>(parameters =>
            parameters.Add(nameof(IonLabel.Color), colorName));

        var style = cut.GetComputedStyle(cut.Root);
        style.ShouldNotBeNull();
        style.Color.ShouldBe(PaletteBase(IonicTheme.CreateMd(), colorName));
    }

    [Fact]
    public void IonLabel_Color_RecolorsTextToPaletteBase_Ios()
    {
        // The port ships both mode stylesheets, so the ios-scoped rule must resolve too.
        UsePlatform(HostPlatform.Ios);
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonLabel>(parameters =>
            parameters.Add(nameof(IonLabel.Color), "danger"));

        cut.Root.ShouldHaveClass("ios");
        var style = cut.GetComputedStyle(cut.Root);
        style.ShouldNotBeNull();
        style.Color.ShouldBe(IonicTheme.CreateIos().Danger);
    }

    [Fact]
    public void IonLabel_InColoredItem_FollowsItemPaletteColor()
    {
        // label.tsx stamps `in-item-color` from hostContext('ion-item.ion-color'); this port
        // expresses the same ancestor test as a descendant selector. Built with the real component
        // nesting (IonList > IonItem > IonLabel) rather than flat divs, since the rule depends on
        // the actual ancestor chain.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonList>(parameters =>
            parameters.Add(nameof(IonList.ChildContent), (RenderFragment)(listBuilder =>
            {
                listBuilder.OpenComponent<IonItem>(0);
                listBuilder.AddAttribute(1, nameof(IonItem.Color), "primary");
                listBuilder.AddAttribute(2, nameof(IonItem.ChildContent), (RenderFragment)(itemBuilder =>
                {
                    itemBuilder.OpenComponent<IonLabel>(0);
                    itemBuilder.AddAttribute(1, nameof(IonLabel.ChildContent),
                        (RenderFragment)(b => b.AddContent(0, "Colored Item")));
                    itemBuilder.CloseComponent();
                }));
                listBuilder.CloseComponent();
            })));

        var label = cut.FindByClass("ion-label").Single();
        var style = cut.GetComputedStyle(label);
        style.ShouldNotBeNull();
        style.Color.ShouldBe(IonicTheme.CreateMd().Primary);
    }

    [Fact]
    public void IonLabel_OwnColor_BeatsItemColor()
    {
        // An explicit color on the label is one class more specific than the in-item rule, so it
        // wins over the item's palette color.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonList>(parameters =>
            parameters.Add(nameof(IonList.ChildContent), (RenderFragment)(listBuilder =>
            {
                listBuilder.OpenComponent<IonItem>(0);
                listBuilder.AddAttribute(1, nameof(IonItem.Color), "primary");
                listBuilder.AddAttribute(2, nameof(IonItem.ChildContent), (RenderFragment)(itemBuilder =>
                {
                    itemBuilder.OpenComponent<IonLabel>(0);
                    itemBuilder.AddAttribute(1, nameof(IonLabel.Color), "danger");
                    itemBuilder.AddAttribute(2, nameof(IonLabel.ChildContent),
                        (RenderFragment)(b => b.AddContent(0, "Colored Item")));
                    itemBuilder.CloseComponent();
                }));
                listBuilder.CloseComponent();
            })));

        var label = cut.FindByClass("ion-label").Single();
        var style = cut.GetComputedStyle(label);
        style.ShouldNotBeNull();
        style.Color.ShouldBe(IonicTheme.CreateMd().Danger);
    }

    [Fact]
    public void IonLabel_PositionFixed_UsesFixedWidthTrack()
    {
        // label.scss :host(.label-fixed): flex 0 0 100px; width/min-width 100px; max-width 200px.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonLabel>(parameters =>
        {
            parameters.Add(nameof(IonLabel.Position), "fixed");
            parameters.Add(nameof(IonLabel.ChildContent),
                (RenderFragment)(b => b.AddContent(0, "A very long label that would otherwise stretch")));
        });

        var box = cut.GetBoxModel(cut.Root);
        box.ShouldNotBeNull();
        box.BorderBox.Width.ShouldBe(100f, 0.5f);
    }

    [Fact]
    public void IonLabel_TextWrap_LoosensLineHeight()
    {
        // label.md.scss / label.ios.scss :host(.ion-text-wrap): line-height 1.5 both modes.
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonLabel>(parameters =>
        {
            parameters.Add(nameof(IonLabel.Class), "ion-text-wrap");
            parameters.Add(nameof(IonLabel.ChildContent), (RenderFragment)(b => b.AddContent(0, "Wrapped")));
        });

        var style = cut.GetComputedStyle(cut.Root);
        style.ShouldNotBeNull();
        style.LineHeight.ShouldBe(Length.Em(1.5f));
    }

    [Fact]
    public void IonLabel_StackedPosition_UsesModeMargin()
    {
        // label.ios.scss gives a stacked label a 4px bottom margin; label.md.scss zeroes it.
        UsePlatform(HostPlatform.Ios);
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonLabel>(parameters =>
            parameters.Add(nameof(IonLabel.Position), "stacked"));

        var style = cut.GetComputedStyle(cut.Root);
        style.ShouldNotBeNull();
        style.MarginBottom.ShouldBe(Length.Px(4));
    }

    private static Color PaletteBase(IonicTheme t, string name) => name switch
    {
        "primary" => t.Primary,
        "secondary" => t.Secondary,
        "tertiary" => t.Tertiary,
        "success" => t.Success,
        "warning" => t.Warning,
        "danger" => t.Danger,
        "light" => t.Light,
        "medium" => t.Medium,
        "dark" => t.Dark,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown palette color"),
    };
}
