using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Ionic.Components;
using Miko.Testing;
using Shouldly;
using Miko.Platform;
using Miko.Styling;
using System.Text;
using System.Globalization;

namespace Miko.Ionic.Tests;

public class IonicThemeRegistryTests
{
    [Theory]
    [InlineData(IonicMode.Md)]
    [InlineData(IonicMode.Ios)]
    public void Registry_CanScopeEveryIonicComponentFamily(IonicMode mode)
    {
        var registry = new IonicStyleRegistry();
        var theme = IonicTheme.Create(mode);
        foreach (var type in typeof(IonButton).Assembly.GetTypes().Where(type =>
            !type.IsAbstract && type.IsSubclassOf(typeof(IonicComponentBase))))
            registry.Register(type, mode, theme).ShouldNotBeNull(type.Name);
    }

    [Fact]
    public void Registry_IgnoresUnrelatedTokensButIncludesCrossComponentDependencies()
    {
        var registry = new IonicStyleRegistry();
        var theme = IonicTheme.CreateMd();
        var scope = registry.Register(typeof(IonButton), IonicMode.Md, theme);
        var count = registry.StyleSheet.Rules.Count;
        theme.Card.Color = Color.Red;
        registry.Register(typeof(IonButton), IonicMode.Md, theme).ShouldBe(scope);
        registry.StyleSheet.Rules.Count.ShouldBe(count);

        theme.Toolbar.ButtonMinHeight += 1;
        registry.Register(typeof(IonButton), IonicMode.Md, theme).ShouldNotBe(scope);
    }

    [Fact]
    public void Registry_DoesNotInjectIosModalRulesInMdMode()
    {
        var registry = new IonicStyleRegistry();
        var globalCount = registry.StyleSheet.Rules.Count;
        var scope = registry.Register(typeof(IonModal), IonicMode.Md, IonicTheme.CreateMd());
        var modal = new DivElement { Class = $"ion-modal ios modal-sheet {scope}" };
        var wrapper = new DivElement { Class = "modal-wrapper" };
        modal.AddChild(wrapper);
        registry.StyleSheet.Rules.Skip(globalCount).Any(rule => rule.Selector.Matches(wrapper)).ShouldBeFalse();
    }

    [Fact]
    public void PartialTokens_KeepExplicitZeroTransparentAndCollectionValues()
    {
        var shadow = new BoxShadow(1, 2, 3, 4, Color.Red);
        var theme = new IonicTheme
        {
            Button = new ButtonToken { BorderRadius = 0, SolidBackground = Color.Transparent, SolidBoxShadow = { shadow } },
        };
        using var context = CreateContext(theme);
        var cut = context.Render<IonButton>();
        var native = cut.FindByClass("button-native").Single();
        var style = cut.GetComputedStyle(native)!;
        style.BackgroundColor.ShouldBe(Color.Transparent);
        style.BorderTopLeftRadius.ShouldBe(Length.Px(0));
        style.BoxShadow!.Value.Value.ShouldHaveSingleItem().ShouldBe(shadow);
        theme.Button.SolidBoxShadow.Clear();
        style.BoxShadow!.Value.Value.ShouldHaveSingleItem().ShouldBe(shadow);
    }

    [Fact]
    public void Registry_ShadowValuesAndCultureDoNotBreakValueBasedReuse()
    {
        var registry = new IonicStyleRegistry();
        var theme = IonicTheme.CreateMd();
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var scope = registry.Register(typeof(IonCard), IonicMode.Md, theme);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            registry.Register(typeof(IonCard), IonicMode.Md, IonicTheme.CreateMd()).ShouldBe(scope);
            theme.Card.BoxShadow.Add(new BoxShadow(1, 2, 3, 4, Color.Red));
            registry.Register(typeof(IonCard), IonicMode.Md, theme).ShouldNotBe(scope);
        }
        finally { CultureInfo.CurrentCulture = culture; }
    }

    [Fact]
    public void AddIonic_BindsJsonAndComposesConfigureCallbacksOnce()
    {
        using var json = new MemoryStream(Encoding.UTF8.GetBytes("""
            { "Ionic": { "Platform": "Ios", "Theme": { "Button": {
              "SolidBackground": "#0e7490", "SolidColor": "#ffffff", "FontSize": 21,
              "LetterSpacing": "0.25em", "SolidBoxShadow": []
            } } } }
            """));
        var configuration = new ConfigurationBuilder().AddJsonStream(json).Build();
        var builder = MikoAppBuilder.CreateDefault();
        builder.Services.Configure<IonicOptions>(configuration.GetSection("Ionic"));
        var calls = 0;
        builder.AddIonic(options => { calls++; options.Theme.Button.MinHeight = 57; });
        calls.ShouldBe(0);
        using var provider = builder.Services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<IonicOptions>>().Value;
        calls.ShouldBe(1);
        options.Platform.ShouldBe(HostPlatform.Ios);
        options.Theme.Button.FontSize.ShouldBe(21);
        options.Theme.Button.SolidBackground.ShouldBe(Color.FromHex("0e7490"));
        options.Theme.Button.LetterSpacing.ShouldBe(Length.Em(.25f));
        provider.GetRequiredService<IOptions<MikoAppOptions>>().Value.StyleSheets
            .ShouldContain(provider.GetRequiredService<IonicStyleRegistry>().StyleSheet);

        using var context = CreateContext(builder);
        var cut = context.Render<IonButton>();
        cut.Root.HasClass("ios").ShouldBeTrue();
        var native = cut.FindByClass("button-native").Single();
        cut.GetComputedStyle(native)!.BackgroundColor.ShouldBe(Color.FromHex("0e7490"));
        cut.GetComputedStyle(native)!.MinHeight.ShouldBe(Length.Px(57));
    }

    [Fact]
    public void Registry_OnlyLoadsTheActiveModeAndPreservesPlatformDefaults()
    {
        var builder = MikoAppBuilder.CreateDefault().AddIonic();
        var platform = new PlatformInfo(HostPlatform.Ios);
        builder.Services.AddSingleton<IPlatformInfo>(platform);
        using var context = CreateContext(builder);
        var ios = context.Render<IonButton>();
        ios.GetComputedStyle(ios.FindByClass("button-native").Single())!.MinHeight
            .ShouldBe(IonicTheme.CreateIos().Button.MinHeight);
        var count = context.StyleSheets[0].Rules.Count;
        platform.Platform = HostPlatform.Android;
        var md = context.Render<IonButton>();
        md.GetComputedStyle(md.FindByClass("button-native").Single())!.MinHeight
            .ShouldBe(IonicTheme.CreateMd().Button.MinHeight);
        GetThemeScope(md.Root).ShouldNotBe(GetThemeScope(ios.Root));
        context.StyleSheets[0].Rules.Count.ShouldBeGreaterThan(count);
        context.Render<IonButton>();
        var afterMd = context.StyleSheets[0].Rules.Count;
        platform.Platform = HostPlatform.Ios;
        context.Render<IonButton>();
        context.StyleSheets[0].Rules.Count.ShouldBe(afterMd);
    }

    [Fact]
    public void ThemeSwitch_RestoresExistingRulesAndKeepsScopesOnAttachedRerenders()
    {
        using var context = CreateContext();
        using var services = context.Services.BuildServiceProvider();
        var switcher = new ThemeSwitcher();
        Element root;
        using (ComponentServiceScope.Push(services)) root = switcher.Build();
        var parent = new DivElement();
        parent.AddChild(root);
        var initial = context.RenderElement(parent);
        var lightScope = GetThemeScope(initial.FindByClass("ion-button").Single());
        switcher.SetDark(true);
        var dark = context.RenderElement(parent);
        dark.GetComputedStyle(dark.FindByClass("button-native").Single())!.BackgroundColor.ShouldBe(Color.Black);
        var count = context.StyleSheets[0].Rules.Count;
        switcher.SetDark(false);
        var light = context.RenderElement(parent);
        GetThemeScope(light.FindByClass("ion-button").Single()).ShouldBe(lightScope);
        context.StyleSheets[0].Rules.Count.ShouldBe(count);

        var button = new RefreshableButton();
        using (ComponentServiceScope.Push(services)) root = button.Build();
        var scope = GetThemeScope(root);
        button.Refresh();
        GetThemeScope(root).ShouldBe(scope);
    }

    [Fact]
    public void LazyStyles_PreserveItemLayoutAndNestedListBorderThemes()
    {
        using var context = CreateContext();
        var standalone = context.Render<IonItem>();
        standalone.GetComputedStyle(standalone.FindByClass("item-native").Single())!.Display.ShouldBe(Display.Flex);
        var cut = context.Render<ThemeContextFixture>();
        var native = cut.FindByClass("item-native").Single();
        cut.GetComputedStyle(native)!.BorderBottomColor.ShouldBe(Color.Red);
        var icon = cut.FindByClass("ion-icon").Single();
        cut.GetComputedStyle(icon)!.FontSize.ShouldBe(IonicTheme.CreateMd().Tab.ButtonIconSize);
        var innerContent = cut.FindByClass("inner-content").Single();
        cut.GetComputedStyle(innerContent.FindByClass("background-content").Single())!.BackgroundColor.ShouldBe(Color.Blue);
    }

    private sealed class RefreshableButton : IonButton
    {
        public void Refresh() => StateHasChanged();
    }

    private sealed class ThemeSwitcher : ComponentBase
    {
        private IonicTheme? _theme;
        public void SetDark(bool value)
        {
            _theme = value ? new IonicTheme { Button = new ButtonToken { SolidBackground = Color.Black, SolidColor = Color.White } } : null;
            StateHasChanged();
        }
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var __c1 = builder.OpenComponent<ConfigProvider>();
            __c1.IonicTheme = _theme;
            __c1.ChildContent = (RenderFragment)(child =>
            {
                child.OpenComponent<IonButton>();
                child.CloseComponent();
            });
            builder.CloseComponent();
        }
    }

    [Fact]
    public void Registry_StartsWithGlobalRulesOnly()
    {
        var registry = new IonicStyleRegistry();
        var global = IonicStyleSheetFactory.CreateGlobal();

        registry.StyleSheet.Rules.Count.ShouldBe(global.Rules.Count);
        registry.StyleSheet.PseudoElementRules.Count.ShouldBe(global.PseudoElementRules.Count);
    }

    [Fact]
    public void Registry_ReusesTheRuleSetForEquivalentComponentThemes()
    {
        var registry = new IonicStyleRegistry();
        var theme = IonicTheme.CreateMd();
        var initialRuleCount = registry.StyleSheet.Rules.Count;

        var firstScope = registry.Register(typeof(IonButton), IonicMode.Md, theme);
        var countAfterFirstRegistration = registry.StyleSheet.Rules.Count;
        var secondScope = registry.Register(typeof(IonButton), IonicMode.Md, IonicTheme.CreateMd());

        firstScope.ShouldNotBeNullOrWhiteSpace();
        secondScope.ShouldBe(firstScope);
        countAfterFirstRegistration.ShouldBeGreaterThan(initialRuleCount);
        registry.StyleSheet.Rules.Count.ShouldBe(countAfterFirstRegistration);
    }

    [Fact]
    public void ConfigProvider_CombinesNestedPartialThemesAndIsolatesTheirRules()
    {
        using var context = CreateContext();
        var cut = context.Render<ThemeFixture>();
        var buttons = cut.FindByClass("ion-button");
        buttons.Count.ShouldBe(3);

        var defaultNative = buttons[0].FindByClass("button-native").ShouldHaveSingleItem();
        var outerNative = buttons[1].FindByClass("button-native").ShouldHaveSingleItem();
        var innerNative = buttons[2].FindByClass("button-native").ShouldHaveSingleItem();

        cut.GetComputedStyle(defaultNative)!.BackgroundColor.ShouldBe(IonicTheme.CreateMd().Button.SolidBackground);
        cut.GetComputedStyle(outerNative)!.BackgroundColor.ShouldBe(Color.FromHex("b42318"));
        cut.GetComputedStyle(innerNative)!.BackgroundColor.ShouldBe(Color.FromHex("b42318"));
        cut.GetComputedStyle(innerNative)!.Color.ShouldBe(Color.FromHex("fef3c7"));

        GetThemeScope(buttons[0]).ShouldNotBe(GetThemeScope(buttons[1]));
        GetThemeScope(buttons[1]).ShouldNotBe(GetThemeScope(buttons[2]));
    }

    [Fact]
    public void StartupOptions_ApplyThemeOverridesToTheFirstComponentRuleSet()
    {
        var theme = new IonicTheme
        {
            Button = new ButtonToken { Color = Color.FromHex("0e7490") },
        };

        using var context = CreateContext(theme);
        var cut = context.Render<IonButton>(parameters => parameters.Add(nameof(IonButton.ChildContent),
            (RenderFragment)(builder => builder.AddContent("Themed"))));
        var native = cut.Root.FindByClass("button-native").ShouldHaveSingleItem();

        cut.GetComputedStyle(native)!.BackgroundColor.ShouldBe(Color.FromHex("0e7490"));
    }

    /// <summary>
    /// Every component resolving the same (app theme, mode, cascading theme) must share one
    /// resolved theme instance rather than build its own (ISSUE-145).
    ///
    /// <para>Resolving means constructing a complete mode theme from scratch and copying the
    /// specified values over it — ~38 KB and ~15 µs each. Doing that per component instance made
    /// it 84% of the build stage's allocation on a real page, and the build stage is what a route
    /// navigation pays. Sharing is only sound because nothing writes to a resolved theme, so this
    /// test also pins the reference-equality that the style-key memo depends on.</para>
    /// </summary>
    [Fact]
    public void ResolvedThemes_AreSharedAcrossComponentsOfTheSameConfiguration()
    {
        IonicComponentBase.InvalidateResolvedThemes();
        using var context = CreateContext();
        var cut = context.Render<ThemeSharingFixture>();

        var scopes = cut.Root.FindByClass("ion-button")
            .Select(GetThemeScope)
            .Distinct()
            .ToArray();

        // Identical configuration ⇒ identical resolved theme ⇒ one style key ⇒ one scope class.
        scopes.ShouldHaveSingleItem();
    }

    /// <summary>
    /// Mutating a theme a caller holds must still change the style key it produces. The memo
    /// added in ISSUE-145 is opt-in for exactly this reason: it applies only to the resolved
    /// instances the component base caches, never to a theme the application owns.
    /// </summary>
    [Fact]
    public void MutatingACallerHeldTheme_StillChangesItsStyleKey()
    {
        var registry = new IonicStyleRegistry();
        var theme = IonicTheme.CreateMd();

        var first = registry.Register(typeof(IonButton), IonicMode.Md, theme);
        first.ShouldNotBeNull();

        theme.Button.SolidBackground = Color.FromHex("123456");

        registry.Register(typeof(IonButton), IonicMode.Md, theme).ShouldNotBe(first);
    }

    /// <summary>Three sibling buttons under one configuration — see the sharing test above.</summary>
    private sealed class ThemeSharingFixture : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<DivElement>();
            for (var i = 0; i < 3; i++)
            {
                builder.OpenComponent<IonButton>();
                builder.CloseComponent();
            }
            builder.CloseElement();
        }
    }

    private static TestContext CreateContext(IonicTheme? startupTheme = null)
    {
        var builder = MikoAppBuilder.CreateDefault().AddIonic(options => options.Theme = startupTheme ?? new IonicTheme());
        return CreateContext(builder);
    }

    private static TestContext CreateContext(MikoAppBuilder builder)
    {
        var context = new TestContext();
        foreach (var descriptor in builder.Services)
            context.Services.Add(descriptor);
        var registry = (IonicStyleRegistry)builder.Services.Single(descriptor =>
            descriptor.ServiceType == typeof(IonicStyleRegistry)).ImplementationInstance!;
        context.AddStyleSheet(registry.StyleSheet);
        return context;
    }

    private static string GetThemeScope(Element element) => (element.Class ?? string.Empty)
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Single(value => value.StartsWith("ion-theme-", StringComparison.Ordinal));

    private sealed class ThemeFixture : ComponentBase
    {
        private static readonly IonicTheme OuterTheme = new()
        {
            Button = new ButtonToken { Color = Color.FromHex("b42318") },
        };

        private static readonly IonicTheme InnerTheme = new()
        {
            Button = new ButtonToken { SolidColor = Color.FromHex("fef3c7") },
        };

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            AddButton(builder, 0, "Default");

            var __c2 = builder.OpenComponent<ConfigProvider>();
            __c2.IonicTheme = OuterTheme;
            __c2.ChildContent = (RenderFragment)(outer =>
            {
                AddButton(outer, 0, "Outer");

                var __c4 = outer.OpenComponent<ConfigProvider>();
                __c4.IonicTheme = InnerTheme;
                __c4.ChildContent = (RenderFragment)(inner =>
                {
                    AddButton(inner, 0, "Inner");
                });
                outer.CloseComponent();
            });
            builder.CloseComponent();
        }

        private static void AddButton(RenderTreeBuilder builder, int sequence, string text)
        {
            builder.OpenComponent<IonButton>(sequence);
            builder.AddComponentParameter(sequence + 1, nameof(IonButton.ChildContent),
                (RenderFragment)(content => content.AddContent(0, text)));
            builder.CloseComponent();
        }
    }
}
