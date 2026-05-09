using BenchmarkDotNet.Attributes;
using Miko.Benchmarks.Helpers;
using Miko.Core;
using Miko.Utils;

namespace Miko.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class TreeTraversalBenchmarks
{
    private Element _largeTree = null!;

    [GlobalSetup]
    public void Setup()
    {
        _largeTree = CreateWideTree(1000);
    }

    [Benchmark]
    public int PreOrder_LargeTree()
    {
        int count = 0;
        TreeTraversal.PreOrder(_largeTree, _ => count++);
        return count;
    }

    [Benchmark]
    public int PostOrder_LargeTree()
    {
        int count = 0;
        TreeTraversal.PostOrder(_largeTree, _ => count++);
        return count;
    }

    [Benchmark]
    public int LevelOrder_LargeTree()
    {
        int count = 0;
        TreeTraversal.LevelOrder(_largeTree, (_, _) => count++);
        return count;
    }

    [Benchmark]
    public Element? FindById_LargeTree()
        => _largeTree.FindById("item-999");

    [Benchmark]
    public List<Element> FindByClass_LargeTree()
        => _largeTree.FindByClass("even");

    private static Element CreateWideTree(int totalNodes)
    {
        var root = new Miko.Core.DomElements.DivElement { Id = "root" };
        int nodesPerGroup = 10;
        int groupCount = totalNodes / nodesPerGroup;

        for (int g = 0; g < groupCount; g++)
        {
            var group = new Miko.Core.DomElements.DivElement
            {
                Id = $"group-{g}",
                Class = "group"
            };
            for (int i = 0; i < nodesPerGroup; i++)
            {
                int idx = g * nodesPerGroup + i;
                group.AddChild(new Miko.Core.DomElements.DivElement
                {
                    Id = $"item-{idx}",
                    Class = idx % 2 == 0 ? "even" : "odd"
                });
            }
            root.AddChild(group);
        }
        return root;
    }
}
