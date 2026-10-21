using Miko.Core;
using Miko.Core.DomElements;
using Shouldly;

namespace Miko.Razor.Tests;

/// <summary>
/// ISSUE-154 BUG-01 / FEAT-01 through the real compiler: <c>@key</c> and call-site identity reach
/// <c>RenderTreeBuilder.OpenComponent</c>, so a parent re-render keeps the child instances whose
/// identity is unchanged.
/// </summary>
public class RazorComponentIdentityTests
{
    /// <summary>Label → instance number of every rendered child.</summary>
    private static Dictionary<string, int> Instances(Element root)
        => root.FindByClass("keyed-child")
            .Select(e => e.TextContent!.Split(':'))
            .ToDictionary(parts => parts[0].StartsWith("trailing", StringComparison.Ordinal) ? "trailing" : parts[0],
                          parts => int.Parse(parts[1]));

    [Fact]
    public void Keyed_items_keep_their_instances_when_reordered_removed_and_inserted()
    {
        var page = new KeyedReuseFixture();
        var host = new DivElement { Children = { page.Build() } };
        var before = Instances(host);

        page.Items = new() { "c", "a", "d" };
        page.Refresh();
        var after = Instances(host);

        after["a"].ShouldBe(before["a"]);
        after["c"].ShouldBe(before["c"]);
        after.ShouldNotContainKey("b");
        after["d"].ShouldNotBeOneOf(before.Values.ToArray());
        host.FindByClass("keyed-child").Select(e => e.TextContent!.Split(':')[0])
            .ShouldBe(new[] { "c", "a", "d", "trailing-1" });
    }

    [Fact]
    public void A_component_after_a_conditional_block_keeps_its_instance_when_the_block_appears()
    {
        var page = new KeyedReuseFixture();
        var host = new DivElement { Children = { page.Build() } };
        var before = Instances(host);

        page.ShowLeading = true;
        page.Refresh();
        var after = Instances(host);

        after["trailing"].ShouldBe(before["trailing"]);
        after["a"].ShouldBe(before["a"]);
        after["leading"].ShouldNotBeOneOf(before.Values.ToArray());
    }

    [Fact]
    public void Focus_follows_a_keyed_element_when_the_list_is_reordered()
    {
        var page = new KeyedReuseFixture();
        var host = new DivElement { Children = { page.Build() } };
        host.FindById("b")!.SetState(ElementState.Focus);

        // b moves from the second place to the first: pairing by position would hand its focus to c.
        page.Items = new() { "b", "c", "a" };
        page.Refresh();

        host.FindById("b")!.HasState(ElementState.Focus).ShouldBeTrue();
        host.FindById("a")!.HasState(ElementState.Focus).ShouldBeFalse();
        host.FindById("c")!.HasState(ElementState.Focus).ShouldBeFalse();
    }

    [Fact]
    public void Duplicate_keys_are_reported()
    {
        var page = new KeyedReuseFixture();
        _ = new DivElement { Children = { page.Build() } };

        page.Items = new() { "a", "a" };

        Should.Throw<InvalidOperationException>(() => page.Refresh());
    }
}
