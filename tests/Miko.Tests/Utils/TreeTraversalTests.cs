using Miko.Core.DomElements;
using Miko.Utils;
using Shouldly;

namespace Miko.Tests.Utils;

public class TreeTraversalTests
{
    [Fact]
    public void PreOrder_ShouldVisitRootFirst()
    {
        var root = new DivElement { Id = "root" };
        var child1 = new DivElement { Id = "child1" };
        var child2 = new DivElement { Id = "child2" };
        root.AddChild(child1);
        root.AddChild(child2);

        var visited = new List<string>();
        TreeTraversal.PreOrder(root, e => visited.Add(e.Id!));

        visited[0].ShouldBe("root");
        visited.ShouldContain("child1");
        visited.ShouldContain("child2");
    }

    [Fact]
    public void PostOrder_ShouldVisitRootLast()
    {
        var root = new DivElement { Id = "root" };
        var child1 = new DivElement { Id = "child1" };
        var child2 = new DivElement { Id = "child2" };
        root.AddChild(child1);
        root.AddChild(child2);

        var visited = new List<string>();
        TreeTraversal.PostOrder(root, e => visited.Add(e.Id!));

        visited[visited.Count - 1].ShouldBe("root");
        visited.ShouldContain("child1");
        visited.ShouldContain("child2");
    }

    [Fact]
    public void LevelOrder_ShouldVisitByLevel()
    {
        var root = new DivElement { Id = "root" };
        var child1 = new DivElement { Id = "child1" };
        var child2 = new DivElement { Id = "child2" };
        var grandchild = new DivElement { Id = "grandchild" };

        root.AddChild(child1);
        root.AddChild(child2);
        child1.AddChild(grandchild);

        var visited = new List<(string id, int level)>();
        TreeTraversal.LevelOrder(root, (e, level) => visited.Add((e.Id!, level)));

        visited[0].ShouldBe(("root", 0));
        visited[1].ShouldBe(("child1", 1));
        visited[2].ShouldBe(("child2", 1));
        visited[3].ShouldBe(("grandchild", 2));
    }

    [Fact]
    public void FindFirst_ShouldReturnFirstMatch()
    {
        var root = new DivElement { Class = "container" };
        var child1 = new SpanElement { Class = "item" };
        var child2 = new SpanElement { Class = "item" };
        root.AddChild(child1);
        root.AddChild(child2);

        var found = TreeTraversal.FindFirst(root, e => e.Class == "item");

        found.ShouldBe(child1);
    }

    [Fact]
    public void FindFirst_WithNoMatch_ShouldReturnNull()
    {
        var root = new DivElement { Class = "container" };

        var found = TreeTraversal.FindFirst(root, e => e.Class == "nonexistent");

        found.ShouldBeNull();
    }

    [Fact]
    public void FindAll_ShouldReturnAllMatches()
    {
        var root = new DivElement();
        var span1 = new SpanElement();
        var span2 = new SpanElement();
        var div = new DivElement();

        root.AddChild(span1);
        root.AddChild(span2);
        root.AddChild(div);

        var found = TreeTraversal.FindAll(root, e => e.TagName == "span");

        found.Count.ShouldBe(2);
        found.ShouldContain(span1);
        found.ShouldContain(span2);
    }

    [Fact]
    public void GetDepth_ForRootElement_ShouldReturnZero()
    {
        var root = new DivElement();

        var depth = TreeTraversal.GetDepth(root);

        depth.ShouldBe(0);
    }

    [Fact]
    public void GetDepth_ForNestedElement_ShouldReturnCorrectDepth()
    {
        var root = new DivElement();
        var child = new DivElement();
        var grandchild = new DivElement();

        root.AddChild(child);
        child.AddChild(grandchild);

        TreeTraversal.GetDepth(child).ShouldBe(1);
        TreeTraversal.GetDepth(grandchild).ShouldBe(2);
    }

    [Fact]
    public void GetHeight_ForLeafElement_ShouldReturnZero()
    {
        var element = new DivElement();

        var height = TreeTraversal.GetHeight(element);

        height.ShouldBe(0);
    }

    [Fact]
    public void GetHeight_ForTreeWithChildren_ShouldReturnMaxDepth()
    {
        var root = new DivElement();
        var child1 = new DivElement();
        var child2 = new DivElement();
        var grandchild = new DivElement();

        root.AddChild(child1);
        root.AddChild(child2);
        child1.AddChild(grandchild);

        var height = TreeTraversal.GetHeight(root);

        height.ShouldBe(2);
    }
}
