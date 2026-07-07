using Miko.Common;
using Miko.Components;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Ionic.Components;
using Miko.Styling;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

public class IonCardTests : IonicComponentTestBase
{
    private static readonly RenderFragment Body = builder => builder.AddContent(0, "Card body");
    private static RenderFragment Text(string value) => builder => builder.AddContent(0, value);

    [Fact]
    public void IonCard_RendersPlainCardDom()
    {
        var cut = Context.Render<IonCard>(p => p.Add(nameof(IonCard.ChildContent), Body));

        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-card");
        cut.GetTextContent().ShouldContain("Card body");
    }

    [Fact]
    public void IonCard_RendersButtonNative_WhenButton()
    {
        var cut = Context.Render<IonCard>(p =>
        {
            p.Add(nameof(IonCard.Button), true);
            p.Add(nameof(IonCard.ChildContent), Body);
        });

        cut.Root.Class.ShouldContain("ion-activatable");
        cut.Root.Children[0].TagName.ShouldBe("button");
        cut.Root.Children[0].Class.ShouldBe("card-native");
    }

    [Fact]
    public void IonCard_RendersAnchorNative_WhenHref()
    {
        var cut = Context.Render<IonCard>(p =>
        {
            p.Add(nameof(IonCard.Href), "/details");
            p.Add(nameof(IonCard.Target), "_blank");
            p.Add(nameof(IonCard.ChildContent), Body);
        });

        var anchor = cut.Root.Children[0].ShouldBeOfType<AnchorElement>();
        anchor.Class.ShouldBe("card-native");
        anchor.Href.ShouldBe("/details");
        anchor.Target.ShouldBe("_blank");
    }

    [Fact]
    public void IonCard_InvokesOnClick_WhenClickable()
    {
        var clicked = false;
        var cut = Context.Render<IonCard>(p =>
        {
            p.Add(nameof(IonCard.Button), true);
            p.Add(nameof(IonCard.OnClick), EventCallback.Factory.Create(this, () => clicked = true));
        });

        cut.Root.Children[0].OnClick!.Invoke(new MouseEventArgs { Target = cut.Root.Children[0] });

        clicked.ShouldBeTrue();
    }

    [Fact]
    public void IonCard_DoesNotInvokeOnClick_WhenDisabled()
    {
        var clicked = false;
        var cut = Context.Render<IonCard>(p =>
        {
            p.Add(nameof(IonCard.Button), true);
            p.Add(nameof(IonCard.Disabled), true);
            p.Add(nameof(IonCard.OnClick), EventCallback.Factory.Create(this, () => clicked = true));
        });

        cut.Root.Children[0].OnClick!.Invoke(new MouseEventArgs { Target = cut.Root.Children[0] });

        clicked.ShouldBeFalse();
        cut.Root.Class.ShouldContain("card-disabled");
    }

    [Fact]
    public void IonCard_DefaultStyle_UsesMdCardSurface()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonCard>();
        var style = cut.GetComputedStyle(cut.Root)!;

        style.MarginTop.ShouldBe(Length.Px(10));
        style.BorderTopLeftRadius.Value.ShouldBe(4f);
        style.BoxShadow.ShouldNotBeNull();
        style.BoxShadow!.Count.ShouldBe(3);
    }

    [Fact]
    public void IonCard_IosStyle_UsesLargerMarginAndRadius()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonCard>();
        var style = cut.GetComputedStyle(cut.Root)!;

        style.MarginTop.ShouldBe(Length.Px(24));
        style.BorderTopLeftRadius.Value.ShouldBe(8f);
    }

    [Fact]
    public void IonCardHeader_RendersAndStyles()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonCardHeader>(p => p.AddChildContent(Text("Header")));

        cut.Root.Class.ShouldBe("md ion-card-header ion-inherit-color");
        cut.GetComputedStyle(cut.Root)!.PaddingTop.ShouldBe(Length.Px(16));
    }

    [Fact]
    public void IonCardHeader_StampsTranslucentAndColor()
    {
        var cut = Context.Render<IonCardHeader>(p =>
        {
            p.Add(nameof(IonCardHeader.Translucent), true);
            p.Add(nameof(IonCardHeader.Color), "primary");
        });

        cut.Root.Class.ShouldContain("card-header-translucent");
        cut.Root.Class.ShouldContain("ion-color-primary");
    }

    [Fact]
    public void IonCardContent_RendersModeSpecificClassAndPadding()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonCardContent>(p => p.AddChildContent(Text("Content")));
        var style = cut.GetComputedStyle(cut.Root)!;

        cut.Root.Class.ShouldBe("md ion-card-content card-content-md");
        style.PaddingTop.ShouldBe(Length.Px(13));
        style.PaddingLeft.ShouldBe(Length.Px(16));
    }

    [Fact]
    public void IonCardTitle_RendersWithCorrectClasses()
    {
        var cut = Context.Render<IonCardTitle>(p => p.AddChildContent(Text("Card Title")));

        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-card-title ion-inherit-color");
        cut.GetTextContent().ShouldBe("Card Title");
    }

    [Fact]
    public void IonCardTitle_AppliesColorClass()
    {
        var cut = Context.Render<IonCardTitle>(p =>
        {
            p.Add(nameof(IonCardTitle.Color), "primary");
            p.AddChildContent(Text("Colored Title"));
        });

        cut.Root.Class.ShouldContain("ion-color");
        cut.Root.Class.ShouldContain("ion-color-primary");
    }

    [Fact]
    public void IonCardSubtitle_RendersWithCorrectClasses()
    {
        var cut = Context.Render<IonCardSubtitle>(p => p.AddChildContent(Text("Card Subtitle")));

        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("md ion-card-subtitle ion-inherit-color");
        cut.GetTextContent().ShouldBe("Card Subtitle");
    }

    [Fact]
    public void IonCardSubtitle_AppliesColorClass()
    {
        var cut = Context.Render<IonCardSubtitle>(p =>
        {
            p.Add(nameof(IonCardSubtitle.Color), "danger");
            p.AddChildContent(Text("Colored Subtitle"));
        });

        cut.Root.Class.ShouldContain("ion-color");
        cut.Root.Class.ShouldContain("ion-color-danger");
    }

    [Fact]
    public void IonCardTitle_IosMode_StampsIosClass()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = Context.Render<IonCardTitle>(p => p.AddChildContent(Text("iOS Title")));

        cut.Root.Class.ShouldContain("ios");
        cut.Root.Class.ShouldContain("ion-card-title");
    }

    [Fact]
    public void IonCardSubtitle_IosMode_StampsIosClass()
    {
        UsePlatform(Miko.Platform.HostPlatform.Ios);

        var cut = Context.Render<IonCardSubtitle>(p => p.AddChildContent(Text("iOS Subtitle")));

        cut.Root.Class.ShouldContain("ios");
        cut.Root.Class.ShouldContain("ion-card-subtitle");
    }

    [Fact]
    public void IonCard_InFlexContainer_HasCorrectDisplay()
    {
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var cut = Context.Render<IonCard>(p => p.AddChildContent(Text("Content")));
        var style = cut.GetComputedStyle(cut.Root)!;

        // Card 应该是 block 元素
        style.Display.ShouldBe(Display.Block);

        // Width 为 100% 以防止在Flex容器中收缩到0
        style.Width.ShouldBe(Length.Percent(100));
    }

    [Fact]
    public void IonCard_InFlexContainer_RendersWithNonZeroWidth()
    {
        // 模拟DebugDemo的实际场景：根容器600px → Flex容器max-width:400px → IonCard
        Context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());

        var containerStyle = new StyleSheet();
        containerStyle.Add(new CssObject
        {
            [".root"] = new()
            {
                Display = Display.Block,
                Width = Length.Px(600),
                Height = Length.Px(600),
            },
            [".flex-container"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Height = Length.Px(200),
                MaxWidth = Length.Px(400),
                Margin = new Margin(0, Length.Auto),
            }
        });
        Context.AddStyleSheet(containerStyle);

        // 创建嵌套结构：root > flex-container > IonCard（带内容）
        var root = new DivElement { Class = "root" };
        var flexContainer = new DivElement { Class = "flex-container" };

        // 创建一个简单的card div来测试
        var cardDiv = new DivElement
        {
            Class = "md ion-card",
            Children =
            {
                new DivElement
                {
                    Class = "md ion-card-header",
                    Children =
                    {
                        new DivElement
                        {
                            Class = "md ion-card-title ion-inherit-color",
                            TextContent = "Title"
                        },
                        new DivElement
                        {
                            Class = "md ion-card-subtitle ion-inherit-color",
                            TextContent = "Subtitle"
                        }
                    }
                },
                new DivElement
                {
                    Class = "md ion-card-content",
                    TextContent = "Content text here"
                }
            }
        };

        flexContainer.AddChild(cardDiv);
        root.AddChild(flexContainer);

        // 渲染整个结构
        var cut = Context.RenderElement(root);

        var container = cut.Root.Children[0];
        var card = container.Children[0];

        // 查找Flex容器和Card的LayoutBox
        var containerBox = cut.FindLayoutBox(container);
        var cardBox = cut.FindLayoutBox(card);

        containerBox.ShouldNotBeNull("Container LayoutBox should exist");
        cardBox.ShouldNotBeNull("Card LayoutBox should exist");

        // 验证Flex容器被max-width约束到400px
        containerBox.BoxModel.Content.Width.ShouldBeLessThanOrEqualTo(400f,
            $"Flex container should be constrained by max-width: 400px, but got {containerBox.BoxModel.Content.Width}px");

        // 验证IonCard的宽度不为0且不超过容器宽度
        cardBox.BoxModel.Content.Width.ShouldBeGreaterThan(0f,
            $"IonCard width should not be 0 in flex container, but got {cardBox.BoxModel.Content.Width}px");
        cardBox.BoxModel.Content.Width.ShouldBeLessThanOrEqualTo(containerBox.BoxModel.Content.Width,
            $"IonCard width should not exceed container width, but card is {cardBox.BoxModel.Content.Width}px and container is {containerBox.BoxModel.Content.Width}px");
    }
}
