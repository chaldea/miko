using Miko.Core;
using Miko.Fonts;
using Miko.Layout;
using Miko.Platform;
using Shouldly;

namespace Miko.Docs.Demo.Tests;

public sealed class ButtonExamplesTests
{
    [Fact]
    public void Should_build_and_layout_all_nine_documented_patterns()
    {
        var app = App.CreateContext();
        var root = app.Controller.BuildRoot();

        var cards = new List<Element>();
        CollectByClass(root, "demo-example-card", cards);
        cards.Count.ShouldBe(9);
        var layout = new LayoutEngine().Layout(root, app.Options.StyleSheets, 1280, 900);
        var shell = root.FindByClass("gallery-shell").Single();
        FindBox(layout, shell).ComputedStyle.Display.ShouldBe(Miko.Common.Display.Flex);
    }

    [Fact]
    public void Should_register_the_bundled_browser_font()
    {
        var app = App.CreateContext();
        app.RegisterFonts();
        FontManager.Instance.IsFontRegistered("Inter").ShouldBeTrue();
    }

    [Fact]
    public void Should_rebuild_buttons_in_the_selected_mode()
    {
        var app = App.CreateContext();
        var platform = (PlatformInfo)app.Services.GetService(typeof(PlatformInfo))!;

        platform.Platform = HostPlatform.Ios;
        var iosButtons = app.Controller.BuildRoot().FindByClass("ion-button");
        iosButtons.ShouldNotBeEmpty();
        iosButtons[0].HasClass("ios").ShouldBeTrue();

        platform.Platform = HostPlatform.Android;
        var mdButtons = app.Controller.BuildRoot().FindByClass("ion-button");
        mdButtons.ShouldNotBeEmpty();
        mdButtons[0].HasClass("md").ShouldBeTrue();
    }

    private static LayoutBox FindBox(LayoutBox box, Element element)
    {
        if (ReferenceEquals(box.Element, element)) return box;
        foreach (var child in box.Children)
        {
            var match = FindBoxOrNull(child, element);
            if (match != null) return match;
        }
        throw new Xunit.Sdk.XunitException($"No layout box found for {element.TagName}.{element.Class}");
    }

    private static LayoutBox? FindBoxOrNull(LayoutBox box, Element element)
    {
        if (ReferenceEquals(box.Element, element)) return box;
        foreach (var child in box.Children)
        {
            var match = FindBoxOrNull(child, element);
            if (match != null) return match;
        }
        return null;
    }

    private static void CollectByClass(Element element, string className, List<Element> results)
    {
        if (element.HasClass(className)) results.Add(element);
        foreach (var child in element.Children) CollectByClass(child, className, results);
    }
}
