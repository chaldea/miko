using Microsoft.Extensions.DependencyInjection;
using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Events;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Testing;
using Shouldly;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// ion-toolbar.md §2: an <c>ion-button</c> inside an <c>ion-toolbar</c> must pick up Ionic's
/// toolbar treatment. button.tsx resolves three things from the button's ancestors at render:
/// <list type="bullet">
///   <item><description><c>'in-toolbar': hostContext('ion-toolbar', this.el)</c> — pulls the label /
///   border / solid fill from the toolbar's own color (<c>button.scss</c>'s
///   <c>:host(.in-toolbar…)</c> block).</description></item>
///   <item><description><c>'in-buttons': hostContext('ion-buttons', this.el)</c> — applies the
///   denser toolbar metrics from <c>buttons.{md,ios}.scss</c>'s
///   <c>::slotted(*) ion-button</c> block.</description></item>
///   <item><description><c>fill = inToolbar ? 'clear' : 'solid'</c> — the default fill flips to
///   clear for buttons slotted through an <c>ion-buttons</c> group.</description></item>
/// </list>
/// Miko's <c>IonButton</c> stamps these from a <c>ToolbarContext</c> cascaded by
/// <c>IonToolbar</c> / <c>IonButtons</c>; these tests pin both the stamping and the resulting styles.
/// </summary>
public class IonButtonInToolbarTests : IDisposable
{
    private readonly PlatformInfo _platform = new(HostPlatform.Android);
    private readonly TestContext _context;

    public IonButtonInToolbarTests()
    {
        _context = new TestContext();
        _context.Services.AddSingleton<IPlatformInfo>(_platform);
        _context.AddStyleSheet(IonicStyleSheetFactory.CreateAllModes());
        _context.ViewportWidth = 400f;
        _context.ViewportHeight = 300f;
    }

    public void Dispose() => _context.Dispose();

    private void UsePlatform(HostPlatform platform) => _platform.Platform = platform;

    // <IonButton [Fill]>label</IonButton>
    private static RenderFragment TextButton(string? fill = null, string? color = null, string? shape = null) => b =>
    {
        b.OpenComponent<IonButton>(0);
        if (fill is not null) b.AddComponentParameter(1, nameof(IonButton.Fill), fill);
        if (color is not null) b.AddComponentParameter(2, nameof(IonButton.Color), color);
        if (shape is not null) b.AddComponentParameter(3, nameof(IonButton.Shape), shape);
        b.AddComponentParameter(4, nameof(IonButton.ChildContent), (RenderFragment)(c => c.AddContent(0, "Edit")));
        b.CloseComponent();
    };

    // <IonButton><IconOnly><IonIcon slot="icon-only" /></IconOnly></IonButton>
    private static RenderFragment IconOnlyButton(string? fill = null) => b =>
    {
        b.OpenComponent<IonButton>(0);
        if (fill is not null) b.AddComponentParameter(1, nameof(IonButton.Fill), fill);
        b.AddComponentParameter(2, nameof(IonButton.IconOnly), (RenderFragment)(c =>
        {
            c.OpenComponent<IonIcon>(0);
            c.AddComponentParameter(1, nameof(IonIcon.Slot), "icon-only");
            c.AddComponentParameter(2, nameof(IonIcon.Icon), Ionicons.Search);
            c.CloseComponent();
        }));
        b.CloseComponent();
    };

    // <IonButtons slot="…">{inner}</IonButtons>
    private static RenderFragment ButtonsGroup(RenderFragment inner, string slot = "start") => b =>
    {
        b.OpenComponent<IonButtons>(0);
        b.AddComponentParameter(1, nameof(IonButtons.Slot), slot);
        b.AddComponentParameter(2, nameof(IonButtons.ChildContent), inner);
        b.CloseComponent();
    };

    // <IonToolbar><Start><IonButtons>{button}</IonButtons></Start></IonToolbar>
    private ComponentUnderTest RenderButtonInButtons(RenderFragment button) =>
        _context.Render<IonToolbar>(p => p.Add(nameof(IonToolbar.Start), ButtonsGroup(button)));

    private static Element Button(ComponentUnderTest cut) => cut.Root.FindByClass("ion-button").First();

    private static Element Native(ComponentUnderTest cut) =>
        cut.Root.FindByClass("button-native").First();

    private static Element Icon(ComponentUnderTest cut) => cut.Root.FindByClass("ion-icon").First();

    // --- Class stamping -----------------------------------------------------------------------

    [Fact]
    public void ButtonInsideToolbarButtons_GetsInToolbarAndInButtons()
    {
        var cut = RenderButtonInButtons(TextButton());

        var button = Button(cut);
        button.HasClass("in-toolbar").ShouldBeTrue();
        button.HasClass("in-buttons").ShouldBeTrue();
    }

    [Fact]
    public void ButtonInToolbarDefaultSlot_GetsInToolbarButNotInButtons()
    {
        // Ionic's in-buttons keys off closest('ion-buttons'); a button dropped straight into the
        // toolbar's default slot is in-toolbar only, so it keeps its standalone metrics.
        var cut = _context.Render<IonToolbar>(p => p.AddChildContent(TextButton()));

        var button = Button(cut);
        button.HasClass("in-toolbar").ShouldBeTrue();
        button.HasClass("in-buttons").ShouldBeFalse();
    }

    [Fact]
    public void StandaloneButton_HasNeitherToolbarClass()
    {
        var cut = _context.Render<IonButton>(p => p.AddChildContent(b => b.AddContent(0, "Edit")));

        var button = cut.Root;
        button.HasClass("in-toolbar").ShouldBeFalse();
        button.HasClass("in-buttons").ShouldBeFalse();
    }

    // --- Default fill flips to clear ------------------------------------------------------------

    [Fact]
    public void ButtonInToolbarButtons_DefaultsToClearFill()
    {
        var cut = RenderButtonInButtons(TextButton());

        var button = Button(cut);
        button.HasClass("button-clear").ShouldBeTrue();
        button.HasClass("button-solid").ShouldBeFalse();
    }

    [Fact]
    public void ButtonInToolbarButtons_KeepsExplicitFill()
    {
        var cut = RenderButtonInButtons(TextButton(fill: "solid"));

        var button = Button(cut);
        button.HasClass("button-solid").ShouldBeTrue();
        button.HasClass("button-clear").ShouldBeFalse();
    }

    [Fact]
    public void StandaloneButton_StillDefaultsToSolidFill()
    {
        var cut = _context.Render<IonButton>(p => p.AddChildContent(b => b.AddContent(0, "Edit")));

        cut.Root.HasClass("button-solid").ShouldBeTrue();
    }

    // --- button.scss :host(.in-toolbar) color rules ----------------------------------------------

    [Fact]
    public void MdClearButtonInToolbar_TakesToolbarColorForItsLabel()
    {
        var t = IonicTheme.CreateMd();
        var cut = RenderButtonInButtons(TextButton());

        // Standalone the label would be the primary ButtonTextColor; in a toolbar it becomes the
        // toolbar's own color (#424242 in md).
        cut.GetComputedStyle(Native(cut))!.Color.ShouldBe(t.ToolbarColor);
        t.ToolbarColor.ShouldNotBe(t.ButtonTextColor);
    }

    [Fact]
    public void IosClearButtonInToolbar_TakesIosToolbarColor()
    {
        UsePlatform(HostPlatform.Ios);
        var t = IonicTheme.CreateIos();

        var cut = RenderButtonInButtons(TextButton());

        cut.GetComputedStyle(Native(cut))!.Color.ShouldBe(t.ToolbarColor);
    }

    [Fact]
    public void SolidButtonInToolbar_InvertsToToolbarColorFill()
    {
        var t = IonicTheme.CreateMd();
        var cut = RenderButtonInButtons(TextButton(fill: "solid"));

        // :host(.button-solid.in-toolbar…) .button-native — fill = toolbar color, label = toolbar bg.
        var style = cut.GetComputedStyle(Native(cut))!;
        style.BackgroundColor.ShouldBe(t.ToolbarColor);
        style.Color.ShouldBe(t.ToolbarBackground);
    }

    [Fact]
    public void OutlineButtonInToolbar_TakesToolbarColorForItsBorder()
    {
        var t = IonicTheme.CreateMd();
        var cut = RenderButtonInButtons(TextButton(fill: "outline"));

        var style = cut.GetComputedStyle(Native(cut))!;
        style.BorderTopColor.ShouldBe(t.ToolbarColor);
        style.Color.ShouldBe(t.ToolbarColor);
    }

    [Fact]
    public void ColoredButtonInToolbar_KeepsItsOwnPalette()
    {
        var t = IonicTheme.CreateMd();
        // Ionic guards every in-toolbar color rule with :not(.ion-color) — an explicitly colored
        // button must not be repainted by the toolbar.
        var cut = RenderButtonInButtons(TextButton(fill: "solid", color: "danger"));

        cut.GetComputedStyle(Native(cut))!.BackgroundColor.ShouldBe(t.Danger);
    }

    // --- buttons.{md,ios}.scss ::slotted(*) ion-button metrics ------------------------------------

    [Fact]
    public void MdButtonInToolbarButtons_UsesToolbarMetrics()
    {
        var t = IonicTheme.CreateMd();
        var cut = RenderButtonInButtons(TextButton());

        var host = cut.GetComputedStyle(Button(cut))!;
        host.MinHeight.ShouldBe(t.ToolbarButtonMinHeight);          // 32px, not the standalone 36px
        host.MarginLeft.Value.ShouldBe(t.ToolbarButtonMarginX);
        host.MarginRight.Value.ShouldBe(t.ToolbarButtonMarginX);

        var native = cut.GetComputedStyle(Native(cut))!;
        native.MinHeight.ShouldBe(t.ToolbarButtonMinHeight);        // mirrors the host min-height
        native.PaddingTop.ShouldBe(t.ToolbarButtonPaddingTop);      // 3px
        native.PaddingBottom.ShouldBe(t.ToolbarButtonPaddingBottom);
        native.PaddingLeft.ShouldBe(t.ToolbarButtonPaddingStart);   // 8px (md), not the 1.1em default
        native.PaddingRight.ShouldBe(t.ToolbarButtonPaddingEnd);
        native.BorderTopLeftRadius.Value.ShouldBe(t.ToolbarButtonBorderRadius); // 2px
    }

    [Fact]
    public void IosButtonInToolbarButtons_UsesIosToolbarMetrics()
    {
        UsePlatform(HostPlatform.Ios);
        var t = IonicTheme.CreateIos();

        var cut = RenderButtonInButtons(TextButton());

        var native = cut.GetComputedStyle(Native(cut))!;
        native.PaddingLeft.ShouldBe(t.ToolbarButtonPaddingStart);   // 5px on iOS
        native.PaddingRight.ShouldBe(t.ToolbarButtonPaddingEnd);
        native.BorderTopLeftRadius.Value.ShouldBe(t.ToolbarButtonBorderRadius); // 4px on iOS
        t.ToolbarButtonBorderRadius.ShouldNotBe(IonicTheme.CreateMd().ToolbarButtonBorderRadius);
    }

    [Fact]
    public void RoundButtonInToolbarButtons_KeepsItsPillRadius()
    {
        var t = IonicTheme.CreateMd();
        // `::slotted(*) ion-button:not(.button-round)` — the squarer toolbar radius skips pills.
        var cut = RenderButtonInButtons(TextButton(shape: "round"));

        cut.GetComputedStyle(Native(cut))!.BorderTopLeftRadius.Value
            .ShouldBe(t.ButtonRoundBorderRadius);
    }

    [Fact]
    public void ButtonInToolbarDefaultSlot_KeepsStandaloneMetrics()
    {
        var t = IonicTheme.CreateMd();
        var cut = _context.Render<IonToolbar>(p => p.AddChildContent(TextButton()));

        // in-toolbar without in-buttons: recolored, but not re-metered.
        var host = cut.GetComputedStyle(Button(cut))!;
        host.MinHeight.ShouldBe(t.ButtonMinHeight);
        cut.GetComputedStyle(Native(cut))!.PaddingLeft.ShouldBe(t.ButtonPaddingStart);
    }

    [Fact]
    public void StandaloneButton_KeepsStandaloneMetrics()
    {
        var t = IonicTheme.CreateMd();
        var cut = _context.Render<IonButton>(p => p.AddChildContent(b => b.AddContent(0, "Edit")));

        cut.GetComputedStyle(cut.Root)!.MinHeight.ShouldBe(t.ButtonMinHeight);
    }

    // --- Toolbar button icons ---------------------------------------------------------------------

    [Fact]
    public void MdIconOnlyClearButtonInToolbar_BecomesCircularTapTarget()
    {
        var t = IonicTheme.CreateMd();
        // buttons.md.scss `::slotted(*) .button-has-icon-only.button-clear` — 3rem circle.
        var cut = RenderButtonInButtons(IconOnlyButton());

        var button = Button(cut);
        button.HasClass("button-has-icon-only").ShouldBeTrue();
        button.HasClass("button-clear").ShouldBeTrue();

        var host = cut.GetComputedStyle(button)!;
        host.Width.Value.ShouldBe(t.ToolbarButtonIconOnlyClearSize);   // 48px
        host.Height.Value.ShouldBe(t.ToolbarButtonIconOnlyClearSize);

        var native = cut.GetComputedStyle(Native(cut))!;
        native.PaddingLeft.ShouldBe(t.ToolbarButtonIconOnlyClearPadding);
        native.BorderTopLeftRadius.ShouldBe(Length.Percent(50));
    }

    [Fact]
    public void IosIconOnlyClearButtonInToolbar_HasNoCircularTreatment()
    {
        UsePlatform(HostPlatform.Ios);
        var t = IonicTheme.CreateIos();
        // iOS's buttons.ios.scss has no `.button-has-icon-only.button-clear` rule.
        t.ToolbarButtonIconOnlyClearSize.ShouldBe(0f);

        var cut = RenderButtonInButtons(IconOnlyButton());

        cut.GetComputedStyle(Native(cut))!.BorderTopLeftRadius
            .ShouldNotBe(Length.Percent(50));
    }

    [Fact]
    public void IconOnlyIconInToolbar_SizesAgainstTheToolbarIconFontSize()
    {
        var t = IonicTheme.CreateMd();
        var cut = RenderButtonInButtons(IconOnlyButton());

        // `::slotted(*) ion-icon[slot="icon-only"] { font-size: 1.8em }` with the base icon rule's
        // width/height: 1em turning that into the icon box — instead of the standalone px box.
        // ComputedStyle resolves em against the inherited (button) font size.
        var style = cut.GetComputedStyle(Icon(cut))!;
        style.FontSize.Value.ShouldBe(t.ButtonFontSize * t.ToolbarButtonIconOnlyFontSize, 0.01f);

        var box = cut.GetBoxModel(Icon(cut))!;
        box.Content.Width.ShouldBe(t.ButtonFontSize * t.ToolbarButtonIconOnlyFontSize, 0.5f);
        box.Content.Height.ShouldBe(t.ButtonFontSize * t.ToolbarButtonIconOnlyFontSize, 0.5f);
    }

    [Fact]
    public void StartIconInToolbarButton_UsesToolbarIconSizeAndGap()
    {
        var t = IonicTheme.CreateMd();
        var cut = RenderButtonInButtons(b =>
        {
            b.OpenComponent<IonButton>(0);
            b.AddComponentParameter(1, nameof(IonButton.Start), (RenderFragment)(c =>
            {
                c.OpenComponent<IonIcon>(0);
                c.AddComponentParameter(1, nameof(IonIcon.Slot), "start");
                c.AddComponentParameter(2, nameof(IonIcon.Icon), Ionicons.Search);
                c.CloseComponent();
            }));
            b.AddComponentParameter(2, nameof(IonButton.ChildContent),
                (RenderFragment)(c => c.AddContent(0, "Contact")));
            b.CloseComponent();
        });

        // 1.4em (md) rather than the standalone button's 1.35em, with a .3em trailing gap.
        var style = cut.GetComputedStyle(Icon(cut))!;
        style.FontSize.Value.ShouldBe(t.ButtonFontSize * t.ToolbarButtonIconFontSize, 0.01f);
        // Margins keep their em unit in ComputedStyle (resolved later, against the icon's own
        // font size) — unlike font-size, which is resolved during the cascade.
        style.MarginRight.ShouldBe(Length.Em(0.3f));
        style.MarginLeft.Value.ShouldBe(0f);
    }

    // --- Survives a re-render -------------------------------------------------------------------

    [Fact]
    public void ButtonInToolbar_KeepsToolbarClasses_AfterItReRendersItself()
    {
        var clicked = false;
        var cut = _context.Render<IonToolbar>(p => p.Add(nameof(IonToolbar.Start), ButtonsGroup(b =>
        {
            b.OpenComponent<IonButton>(0);
            b.AddComponentParameter(1, nameof(IonButton.OnClick),
                EventCallback.Factory.Create(this, () => clicked = true));
            b.AddComponentParameter(2, nameof(IonButton.ChildContent),
                (RenderFragment)(c => c.AddContent(0, "Edit")));
            b.CloseComponent();
        })));

        var native = cut.Root.FindByClass("button-native").First();
        native.OnClick!.Invoke(new MouseEventArgs { Target = native });
        clicked.ShouldBeTrue();

        // Clicking makes the button re-render itself, which rebuilds its root from its own
        // OnParametersSet — the toolbar's post-pass is not on that path. The toolbar classes must
        // survive anyway, or the button visibly snaps back to its standalone look on first tap.
        var button = cut.Root.FindByClass("ion-button").First();
        button.HasClass("in-toolbar").ShouldBeTrue();
        button.HasClass("in-buttons").ShouldBeTrue();
        button.HasClass("button-clear").ShouldBeTrue();
        button.HasClass("button-solid").ShouldBeFalse();
    }

    [Fact]
    public void ButtonInToolbar_KeepsToolbarStyles_AfterItReRendersItself()
    {
        var t = IonicTheme.CreateMd();
        var cut = _context.Render<IonToolbar>(p => p.Add(nameof(IonToolbar.Start), ButtonsGroup(b =>
        {
            b.OpenComponent<IonButton>(0);
            b.AddComponentParameter(1, nameof(IonButton.OnClick),
                EventCallback.Factory.Create(this, () => { }));
            b.AddComponentParameter(2, nameof(IonButton.ChildContent),
                (RenderFragment)(c => c.AddContent(0, "Edit")));
            b.CloseComponent();
        })));

        var native = cut.Root.FindByClass("button-native").First();
        native.OnClick!.Invoke(new MouseEventArgs { Target = native });

        // The visible symptom: without the classes the button reverts to the standalone 36px
        // metrics and the primary-blue label.
        // Re-lay-out the mutated tree so the assertions read post-click computed styles.
        var after = _context.RenderElement(cut.Root);
        var host = after.GetComputedStyle(after.Root.FindByClass("ion-button").First())!;
        host.MinHeight.ShouldBe(t.ToolbarButtonMinHeight);
        after.GetComputedStyle(after.Root.FindByClass("button-native").First())!
            .Color.ShouldBe(t.ToolbarColor);
    }

    [Fact]
    public void StandaloneStartIcon_KeepsTheStandaloneIconSize()
    {
        var cut = _context.Render<IonButton>(p =>
        {
            p.Add(nameof(IonButton.Start), (RenderFragment)(c =>
            {
                c.OpenComponent<IonIcon>(0);
                c.AddComponentParameter(1, nameof(IonIcon.Slot), "start");
                c.AddComponentParameter(2, nameof(IonIcon.Icon), Ionicons.Search);
                c.CloseComponent();
            }));
            p.AddChildContent(c => c.AddContent(0, "Contact"));
        });

        var t = IonicTheme.CreateMd();
        cut.GetComputedStyle(Icon(cut))!.FontSize.Value.ShouldBe(t.ButtonFontSize * 1.35f, 0.01f);
    }
}
