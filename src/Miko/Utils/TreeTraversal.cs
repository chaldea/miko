using Miko.Core;

namespace Miko.Utils;

/// <summary>
/// 树遍历工具
/// </summary>
public static class TreeTraversal
{
    /// <summary>
    /// 前序遍历
    /// </summary>
    public static void PreOrder(Element root, Action<Element> action)
    {
        action(root);

        foreach (var child in root.Children)
        {
            PreOrder(child, action);
        }
    }

    /// <summary>
    /// 后序遍历
    /// </summary>
    public static void PostOrder(Element root, Action<Element> action)
    {
        foreach (var child in root.Children)
        {
            PostOrder(child, action);
        }

        action(root);
    }

    /// <summary>
    /// 层序遍历
    /// </summary>
    public static void LevelOrder(Element root, Action<Element, int> action)
    {
        var queue = new Queue<(Element element, int level)>();
        queue.Enqueue((root, 0));

        while (queue.Count > 0)
        {
            var (element, level) = queue.Dequeue();
            action(element, level);

            foreach (var child in element.Children)
            {
                queue.Enqueue((child, level + 1));
            }
        }
    }

    /// <summary>
    /// 查找第一个满足条件的元素
    /// </summary>
    public static Element? FindFirst(Element root, Predicate<Element> predicate)
    {
        if (predicate(root))
        {
            return root;
        }

        foreach (var child in root.Children)
        {
            var found = FindFirst(child, predicate);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// 查找所有满足条件的元素
    /// </summary>
    public static List<Element> FindAll(Element root, Predicate<Element> predicate)
    {
        var results = new List<Element>();

        if (predicate(root))
        {
            results.Add(root);
        }

        foreach (var child in root.Children)
        {
            results.AddRange(FindAll(child, predicate));
        }

        return results;
    }

    /// <summary>
    /// 获取元素的深度
    /// </summary>
    public static int GetDepth(Element element)
    {
        int depth = 0;
        var current = element.Parent;

        while (current != null)
        {
            depth++;
            current = current.Parent;
        }

        return depth;
    }

    /// <summary>
    /// 获取树的高度
    /// </summary>
    public static int GetHeight(Element root)
    {
        if (root.Children.Count == 0)
        {
            return 0;
        }

        int maxHeight = 0;
        foreach (var child in root.Children)
        {
            maxHeight = Math.Max(maxHeight, GetHeight(child));
        }

        return maxHeight + 1;
    }
}
