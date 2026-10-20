using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Shouldly;

namespace Miko.Razor.Tests;

/// <summary>
/// ISSUE-153 problem 4: <c>@ref</c> compiled into a literal <c>"@ref"</c> attribute because the
/// compiler's ref support is switched on by the <see cref="ElementReference"/> type, which did not
/// exist. Every captured field stayed null, so component methods such as
/// <c>IonModal.DismissAsync</c> could not be called from markup.
/// </summary>
public class RazorRefCaptureTests
{
    private static bool IsInside(Element element, Element root)
    {
        for (var node = element; node is not null; node = node.Parent)
            if (ReferenceEquals(node, root)) return true;
        return false;
    }

    [Fact]
    public void ElementRefs_CaptureTheBuiltElements_InEachFieldStyle()
    {
        var page = new RefCaptureFixture();
        var root = page.Build();

        page.Root.ShouldBeSameAs(root);
        page.Canvas.ShouldBeSameAs(root.FindByClass("ref-canvas").Single());
        page.Span.ShouldBeSameAs(root.FindByClass("ref-span").Single());
        page.Button.Element.ShouldBeSameAs(root.FindByClass("ref-button").Single());
    }

    [Fact]
    public void ComponentRef_CapturesTheComponentInstance()
    {
        var page = new RefCaptureFixture();
        page.Build();

        page.Child.ShouldNotBeNull();
        page.Child!.Label.ShouldBe("child");
    }

    [Fact]
    public void Refs_FollowTheLiveElements_AfterARerender()
    {
        // No parent element: StateHasChanged moves the new content into the surviving root and
        // discards the newly built root, so a ref on the root must be re-pointed.
        var page = new RefCaptureFixture();
        var root = page.Build();

        page.Refresh();

        page.Root.ShouldBeSameAs(root);
        IsInside(page.Canvas!, root).ShouldBeTrue("the canvas ref must point into the live tree");
        page.Span!.TextContent.ShouldBe("1");
        IsInside(page.Span, root).ShouldBeTrue();
    }

    [Fact]
    public void Refs_FollowTheLiveElements_WhenRerenderedUnderAParent()
    {
        var page = new RefCaptureFixture();
        var host = new DivElement { Children = { page.Build() } };

        page.Refresh();

        page.Root.ShouldBeSameAs(host.Children.Single());
        IsInside(page.Canvas!, host).ShouldBeTrue();
        page.Span!.TextContent.ShouldBe("1");
    }
}
