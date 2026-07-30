using Microsoft.Extensions.DependencyInjection;
using Miko.Common;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// Stylesheet assertions for <see cref="IonIcon"/> — the ported <c>icon.css</c> rules:
/// the 1em × 1em inline-block host box, the font-size-driven <c>icon-small</c>/<c>icon-large</c>
/// sizes, and the <c>ion-color-*</c> tint (CSS <c>fill: currentColor</c>).
/// </summary>
public class IonIconStyleTests
{
    private static TestContext ContextFor(HostPlatform platform)
    {
        var ctx = new TestContext();
        ctx.Services.AddSingleton<IPlatformInfo>(new PlatformInfo(platform));
        ctx.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        ctx.ViewportWidth = 300f;
        ctx.ViewportHeight = 200f;
        return ctx;
    }

    [Theory]
    [InlineData(HostPlatform.Android)] // md
    [InlineData(HostPlatform.Ios)]
    public void IonIcon_HostBox_IsInlineBlock1em(HostPlatform platform)
    {
        using var ctx = ContextFor(platform);
        var cut = ctx.Render<IonIcon>(p => p.Add(nameof(IonIcon.Icon), "triangle"));
        var computed = cut.GetComputedStyle(cut.Root)!;

        computed.Display.ShouldBe(Display.InlineBlock);
        computed.Width.Unit.ShouldBe(LengthUnit.Em);
        computed.Width.Value.ShouldBe(1f);
        computed.Height.Unit.ShouldBe(LengthUnit.Em);
        computed.Height.Value.ShouldBe(1f);
    }

    [Fact]
    public void IonIcon_Small_SetsFontSize()
    {
        // :host(.icon-small) { font-size: 1.125rem } — computed styles resolve rem to px
        // (18px at the 16px root), and the 1em box follows.
        using var ctx = ContextFor(HostPlatform.Android);
        var cut = ctx.Render<IonIcon>(p => p
            .Add(nameof(IonIcon.Icon), "triangle")
            .Add(nameof(IonIcon.Size), "small"));
        var computed = cut.GetComputedStyle(cut.Root)!;

        computed.FontSize.Unit.ShouldBe(LengthUnit.Px);
        computed.FontSize.Value.ShouldBe(Length.RootFontSize * 1.125f, 0.01f);

        var box = cut.GetBoxModel(cut.Root)!.BorderBox;
        box.Width.ShouldBe(Length.RootFontSize * 1.125f, 0.01f);
        box.Height.ShouldBe(Length.RootFontSize * 1.125f, 0.01f);
    }

    [Fact]
    public void IonIcon_Large_SetsFontSize()
    {
        // :host(.icon-large) { font-size: 2rem } — computed styles resolve rem to px
        // (32px at the 16px root), and the 1em box follows.
        using var ctx = ContextFor(HostPlatform.Android);
        var cut = ctx.Render<IonIcon>(p => p
            .Add(nameof(IonIcon.Icon), "triangle")
            .Add(nameof(IonIcon.Size), "large"));
        var computed = cut.GetComputedStyle(cut.Root)!;

        computed.FontSize.Unit.ShouldBe(LengthUnit.Px);
        computed.FontSize.Value.ShouldBe(Length.RootFontSize * 2f, 0.01f);

        var box = cut.GetBoxModel(cut.Root)!.BorderBox;
        box.Width.ShouldBe(Length.RootFontSize * 2f, 0.01f);
        box.Height.ShouldBe(Length.RootFontSize * 2f, 0.01f);
    }

    [Fact]
    public void IonIcon_Color_TintsViaTextColor()
    {
        // :host(.ion-color-primary) — the glyph is tinted with the element color
        // (fill: currentColor), so the rule maps to the theme's primary base.
        using var ctx = ContextFor(HostPlatform.Android);
        var cut = ctx.Render<IonIcon>(p => p
            .Add(nameof(IonIcon.Icon), "triangle")
            .Add(nameof(IonIcon.Color), "primary"));
        var computed = cut.GetComputedStyle(cut.Root)!;

        computed.Color.ShouldBe(IonicTheme.CreateMd().Primary);
    }

    [Fact]
    public void IonIcon_UserClass_OverridesHostBoxSize()
    {
        // ISSUE-107：后加载的应用样式表中的单类规则必须能覆盖组件宿主盒样式
        //（.ion-icon.{mode} 的 1em × 1em）——对应浏览器中外层文档规则恒胜于 :host
        // shadow 规则的语义（CSS Scoping）；ionic 样式表位于较低的级联层（-1）。
        using var ctx = ContextFor(HostPlatform.Android);
        var userSheet = new Miko.Styling.StyleSheet();
        userSheet.Add(new Miko.Styling.CssObject
        {
            [".component-icon"] = new()
            {
                Width = Length.Px(50),
                Height = Length.Px(50),
                BackgroundColor = Color.FromHex("0054e9"),
            }
        });
        ctx.AddStyleSheet(userSheet);

        var cut = ctx.Render<IonIcon>(p => p
            .Add(nameof(IonIcon.Icon), "triangle")
            .Add(nameof(IonIcon.Class), "component-icon"));
        var computed = cut.GetComputedStyle(cut.Root)!;

        computed.Width.Value.ShouldBe(50f);
        computed.Height.Value.ShouldBe(50f);
        computed.BackgroundColor.ShouldBe(Color.FromHex("0054e9"));

        var box = cut.GetBoxModel(cut.Root)!.BorderBox;
        box.Width.ShouldBe(50f, 0.01f);
        box.Height.ShouldBe(50f, 0.01f);
    }

    [Fact]
    public void IonIcon_TabButtonIcon_SizedByFontSize()
    {
        // .ion-tab-button .ion-icon — the tab button drives the icon size via font-size
        // (Ionic's ::slotted(ion-icon) { font-size: $tab-button-icon-size }).
        using var ctx = ContextFor(HostPlatform.Android);
        var cut = ctx.Render<IonTabButton>(p => p
            .AddChildContent(builder =>
            {
                builder.OpenComponent<IonIcon>(0);
                builder.AddComponentParameter(1, nameof(IonIcon.Icon), "triangle");
                builder.CloseComponent();
            }));

        var icon = cut.Root.FindByClass("ion-icon").First();
        var computed = cut.GetComputedStyle(icon)!;

        computed.FontSize.Unit.ShouldBe(LengthUnit.Px);
        computed.FontSize.Value.ShouldBe(IonicTheme.CreateMd().TabButtonIconSize);

        var box = cut.GetBoxModel(icon)!.BorderBox;
        box.Width.ShouldBe(IonicTheme.CreateMd().TabButtonIconSize, 0.01f);
        box.Height.ShouldBe(IonicTheme.CreateMd().TabButtonIconSize, 0.01f);
    }
}
