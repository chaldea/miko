using Miko.Common;
using Miko.Components.Virtualization;
using Miko.Core;
using Miko.Hosting;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;
using SkiaSharp;

namespace Miko.Razor.Tests;

/// <summary>
/// ISSUE-151 through the real compiler: <c>Virtualize</c> written the ways Blazor allows — TItem inferred
/// from <c>Items</c> with the template between the tags and <c>Context</c>, the template in an
/// <c>&lt;ItemContent&gt;</c> element, an <c>ItemsProvider</c> with a <c>&lt;Placeholder&gt;</c> — needs
/// <c>RenderFragment&lt;TValue&gt;</c>, and the name-based parameter path (type-inferred generic components)
/// must find <c>Virtualize.ChildContent</c>, which hides <c>ComponentBase.ChildContent</c>.
/// </summary>
public class RazorVirtualizeTests : IDisposable
{
    private const int W = 900;
    private const int H = 300;

    private readonly SKSurface _surface = SKSurface.Create(new SKImageInfo(W, H));

    public void Dispose() => _surface.Dispose();

    private (MikoEngine Engine, VirtualizeFixture Page, Element Root) Start()
    {
        var sheet = new StyleSheet();
        sheet.AddRule(new ClassSelector("lists"), new Style { Display = Display.Flex });
        sheet.AddRule(new ClassSelector("scroller"), new Style
        {
            Width = Length.Px(300),
            Height = Length.Px(H),
            OverflowY = Overflow.Auto,
        });
        sheet.AddRule(new ClassSelector("person"), new Style { Height = Length.Px(30) });
        sheet.AddRule(new ClassSelector("item-content"), new Style { Height = Length.Px(30) });
        sheet.AddRule(new ClassSelector("loaded"), new Style { Height = Length.Px(30) });
        sheet.AddRule(new ClassSelector("placeholder"), new Style { Height = Length.Px(30) });

        var page = new VirtualizeFixture();
        var root = page.Build();
        var engine = new MikoEngineBuilder().Build();
        engine.Initialize(root, new List<StyleSheet> { sheet }, _surface.Canvas, W, H);
        return (engine, page, root);
    }

    [Fact]
    public void Inferred_item_type_with_the_template_between_the_tags_renders_the_visible_items()
    {
        var (_, _, root) = Start();

        var people = root.FindByClass("person");
        people.Count.ShouldBeInRange(10, 14, "300px / 30px rows plus the overscan, out of 1000");
        people[0].TextContent.ShouldBe("Person 0");
    }

    [Fact]
    public void ItemContent_element_with_a_named_context_renders_the_visible_items()
    {
        var (_, _, root) = Start();

        var items = root.FindByClass("item-content");
        items.Count.ShouldBeInRange(10, 12);
        items[0].TextContent.ShouldBe("Person 0");
        root.FindByClass("empty").ShouldBeEmpty();
    }

    [Fact]
    public void EmptyContent_is_shown_when_there_are_no_items()
    {
        var page = new VirtualizeFixture { People = new() };
        var host = page.Build();

        host.FindByClass("empty").ShouldHaveSingleItem("only the explicit list declares <EmptyContent>");
    }

    [Fact]
    public void ItemsProvider_shows_placeholders_until_the_items_arrive()
    {
        var (engine, page, root) = Start();

        var pending = page.PendingLoad.ShouldNotBeNull("the provider was asked for the first screenful");
        root.FindByClass("loaded").ShouldBeEmpty();

        pending.SetResult(new ItemsProviderResult<VirtualizeFixture.Person>(
            Enumerable.Range(0, 20).Select(i => new VirtualizeFixture.Person(i, $"Loaded {i}")), 500));
        engine.Render(_surface.Canvas);

        var loaded = root.FindByClass("loaded");
        loaded.Count.ShouldBeGreaterThanOrEqualTo(10);
        loaded[0].TextContent.ShouldBe("Loaded 0");
    }
}
