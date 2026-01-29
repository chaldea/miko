using Miko.Layout;
using Miko.Styling;

namespace Miko.Core;

/// <summary>
/// 元素基类
/// </summary>
public abstract class Element
{
    public string? Id { get; set; }
    public string? Class { get; set; }
    public List<Element> Children { get; set; } = new();
    public Element? Parent { get; private set; }
    public Style? Style { get; set; }
    public string? TextContent { get; set; }

    // 布局后的盒子模型引用
    internal LayoutBox? LayoutBox { get; set; }

    // 脏标记
    internal bool IsDirty { get; set; }

    public abstract string TagName { get; }

    /// <summary>
    /// 添加子元素
    /// </summary>
    public void AddChild(Element child)
    {
        if (child.Parent != null)
        {
            child.Parent.RemoveChild(child);
        }

        Children.Add(child);
        child.Parent = this;
        IsDirty = true;
    }

    /// <summary>
    /// 移除子元素
    /// </summary>
    public bool RemoveChild(Element child)
    {
        if (Children.Remove(child))
        {
            child.Parent = null;
            IsDirty = true;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 查找元素（通过ID）
    /// </summary>
    public Element? FindById(string id)
    {
        if (Id == id) return this;

        foreach (var child in Children)
        {
            var found = child.FindById(id);
            if (found != null) return found;
        }

        return null;
    }

    /// <summary>
    /// 查找元素（通过Class）
    /// </summary>
    public List<Element> FindByClass(string className)
    {
        var results = new List<Element>();

        if (Class != null && Class.Split(' ').Contains(className))
        {
            results.Add(this);
        }

        foreach (var child in Children)
        {
            results.AddRange(child.FindByClass(className));
        }

        return results;
    }

    /// <summary>
    /// 查找元素（通过标签名）
    /// </summary>
    public List<Element> FindByTagName(string tagName)
    {
        var results = new List<Element>();

        if (TagName.Equals(tagName, StringComparison.OrdinalIgnoreCase))
        {
            results.Add(this);
        }

        foreach (var child in Children)
        {
            results.AddRange(child.FindByTagName(tagName));
        }

        return results;
    }

    /// <summary>
    /// 检查是否有指定的class
    /// </summary>
    public bool HasClass(string className)
    {
        return Class != null && Class.Split(' ').Contains(className);
    }

    public override string ToString() => $"<{TagName} id=\"{Id}\" class=\"{Class}\">";
}
