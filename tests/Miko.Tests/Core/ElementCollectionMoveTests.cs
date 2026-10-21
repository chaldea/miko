using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Core;

/// <summary>
/// ISSUE-154 BUG-03：一个元素任何时刻只属于一个父节点、在其子集合中只出现一次。
///
/// <para>此前 <c>Children.Add</c>/<c>Insert</c>/索引器/<c>AddRange</c> 只改写子节点的 <c>Parent</c>，
/// 旧父节点的集合里仍留着它——同一元素同时挂在两棵树上，旧引擎也不知道自己的树变了；重复的节点、
/// null 与祖先环都会被收下。现在所有入口统一为 DOM 的移动语义，非法操作在改动任何东西之前被拒绝。</para>
/// </summary>
public class ElementCollectionMoveTests
{
    [Fact]
    public void Add_should_move_an_element_from_its_previous_parent()
    {
        var oldParent = new DivElement();
        var newParent = new DivElement();
        var child = new SpanElement();
        oldParent.Children.Add(child);

        newParent.Children.Add(child);

        oldParent.Children.ShouldBeEmpty();
        newParent.Children.ShouldBe(new Element[] { child });
        child.Parent.ShouldBeSameAs(newParent);
    }

    [Fact]
    public void Insert_and_indexer_should_move_an_element_from_its_previous_parent()
    {
        var oldParent = new DivElement();
        var a = new SpanElement();
        var b = new SpanElement();
        oldParent.Children.Add(a);
        oldParent.Children.Add(b);

        var newParent = new DivElement { Children = { new DivElement() } };
        newParent.Children.Insert(0, a);
        newParent.Children[1] = b;

        oldParent.Children.ShouldBeEmpty();
        newParent.Children.ShouldBe(new Element[] { a, b });
        a.Parent.ShouldBeSameAs(newParent);
        b.Parent.ShouldBeSameAs(newParent);
    }

    [Fact]
    public void Adding_an_element_already_in_the_collection_should_move_it_not_duplicate_it()
    {
        var parent = new DivElement();
        var a = new SpanElement();
        var b = new SpanElement();
        var c = new SpanElement();
        parent.Children.AddRange(new Element[] { a, b, c });

        parent.Children.Add(a);
        parent.Children.ShouldBe(new Element[] { b, c, a });

        parent.Children.Insert(0, a);
        parent.Children.ShouldBe(new Element[] { a, b, c });

        // insertBefore 语义：插到索引 2 处现有元素（c）之前。
        parent.Children.Insert(2, a);
        parent.Children.ShouldBe(new Element[] { b, a, c });

        // replaceChild 语义：新节点从原位置移走，替换掉目标位置的旧节点。
        parent.Children[2] = b;
        parent.Children.ShouldBe(new Element[] { a, b });
        c.Parent.ShouldBeNull();
    }

    [Fact]
    public void Should_reject_null()
    {
        var parent = new DivElement { Children = { new SpanElement() } };

        Should.Throw<ArgumentNullException>(() => parent.Children.Add(null!));
        Should.Throw<ArgumentNullException>(() => parent.Children.Insert(0, null!));
        Should.Throw<ArgumentNullException>(() => parent.Children[0] = null!);
        Should.Throw<ArgumentNullException>(() => parent.AddChild(null!));
        parent.Children.Count.ShouldBe(1);
    }

    [Fact]
    public void Should_reject_cycles()
    {
        var root = new DivElement();
        var child = new DivElement();
        var grandChild = new DivElement();
        root.AddChild(child);
        child.AddChild(grandChild);

        Should.Throw<InvalidOperationException>(() => root.Children.Add(root));
        Should.Throw<InvalidOperationException>(() => grandChild.Children.Add(root));
        Should.Throw<InvalidOperationException>(() => grandChild.Children.Insert(0, child));
        Should.Throw<InvalidOperationException>(() => grandChild.AddChild(child));

        // 失败的操作不改变任何关系。
        root.Children.ShouldBe(new Element[] { child });
        child.Children.ShouldBe(new Element[] { grandChild });
        grandChild.Children.ShouldBeEmpty();
        child.Parent.ShouldBeSameAs(root);
        root.Parent.ShouldBeNull();
    }

    [Fact]
    public void Failed_indexer_replacement_should_leave_the_collection_unchanged()
    {
        var root = new DivElement();
        var child = new DivElement();
        var other = new SpanElement();
        root.AddChild(child);
        child.AddChild(other);

        Should.Throw<InvalidOperationException>(() => child.Children[0] = root);

        child.Children.ShouldBe(new Element[] { other });
        other.Parent.ShouldBeSameAs(child);
        root.Children.ShouldBe(new Element[] { child });
    }

    [Fact]
    public void AddRange_should_validate_every_item_before_changing_anything()
    {
        var root = new DivElement();
        var child = new DivElement();
        root.AddChild(child);
        var fresh = new SpanElement();

        Should.Throw<InvalidOperationException>(() => child.Children.AddRange(new Element[] { fresh, root }));
        Should.Throw<ArgumentNullException>(() => child.Children.AddRange(new Element[] { fresh, null! }));

        child.Children.ShouldBeEmpty();
        fresh.Parent.ShouldBeNull();
    }

    [Fact]
    public void AddRange_from_another_elements_children_should_move_them_all()
    {
        var source = new DivElement { Children = { new SpanElement(), new SpanElement(), new SpanElement() } };
        var target = new DivElement();
        var moved = source.Children.ToArray();

        target.Children.AddRange(source.Children);

        source.Children.ShouldBeEmpty();
        target.Children.ShouldBe(moved);
        target.Children.ShouldAllBe(c => ReferenceEquals(c.Parent, target));
    }

    [Fact]
    public void RemoveAll_should_evaluate_the_predicate_once_per_child()
    {
        var parent = new DivElement { Children = { new SpanElement(), new SpanElement(), new DivElement() } };
        var calls = 0;

        var removed = parent.Children.RemoveAll(e => { calls++; return e is SpanElement; });

        removed.ShouldBe(2);
        calls.ShouldBe(3);
        parent.Children.Single().ShouldBeOfType<DivElement>();
    }

    [Fact]
    public void Moving_between_engines_should_invalidate_both_and_transfer_ownership()
    {
        var sheets = new List<StyleSheet>();
        using var surface = SKSurface.Create(new SKImageInfo(200, 200));
        var engineA = new MikoEngineBuilder().Build();
        var engineB = new MikoEngineBuilder().Build();
        var rootA = new DivElement { Style = new Style { Width = Length.Px(100), Height = Length.Px(100) } };
        var rootB = new DivElement { Style = new Style { Width = Length.Px(100), Height = Length.Px(100) } };
        var moved = new DivElement { Children = { new SpanElement { TextContent = "x" } } };
        rootA.AddChild(moved);
        engineA.Initialize(rootA, sheets, surface.Canvas, 200, 200);
        engineB.Initialize(rootB, sheets, surface.Canvas, 200, 200);
        engineA.Render(surface.Canvas);
        engineB.Render(surface.Canvas);
        var versionA = engineA.Mutations.Version;
        var versionB = engineB.Mutations.Version;

        rootB.Children.Add(moved);

        engineA.Mutations.Version.ShouldBeGreaterThan(versionA, "the old engine's tree lost a child");
        engineB.Mutations.Version.ShouldBeGreaterThan(versionB, "the new engine's tree gained a child");
        rootA.Children.ShouldBeEmpty();

        // 此后对被移动子树的修改只记到新引擎头上。
        versionA = engineA.Mutations.Version;
        versionB = engineB.Mutations.Version;
        moved.Children[0].Class = "changed";
        engineA.Mutations.Version.ShouldBe(versionA);
        engineB.Mutations.Version.ShouldBeGreaterThan(versionB);
    }

    [Fact]
    public void Moving_into_a_detached_element_should_clear_the_old_engine_ownership()
    {
        using var surface = SKSurface.Create(new SKImageInfo(100, 100));
        var engine = new MikoEngineBuilder().Build();
        var root = new DivElement();
        var moved = new DivElement();
        root.AddChild(moved);
        engine.Initialize(root, new List<StyleSheet>(), surface.Canvas, 100, 100);
        engine.Render(surface.Canvas);

        var detached = new DivElement();
        detached.Children.Add(moved);
        var version = engine.Mutations.Version;
        moved.Class = "changed";

        engine.Mutations.Version.ShouldBe(version);
    }
}
