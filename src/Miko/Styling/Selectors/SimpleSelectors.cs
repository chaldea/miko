using Miko.Core;

namespace Miko.Styling.Selectors;

/// <summary>
/// Class 选择器 (.class-name)
/// </summary>
public class ClassSelector : Selector
{
    public string ClassName { get; set; }

    public ClassSelector(string className)
    {
        ClassName = className.TrimStart('.');
    }

    public override bool Matches(Element element)
    {
        return element.HasClass(ClassName);
    }

    public override int Specificity => 10; // Class选择器特异性为10
}

/// <summary>
/// ID 选择器 (#id)
/// </summary>
public class IdSelector : Selector
{
    public string Id { get; set; }

    public IdSelector(string id)
    {
        Id = id.TrimStart('#');
    }

    public override bool Matches(Element element)
    {
        return element.Id == Id;
    }

    public override int Specificity => 100; // ID选择器特异性为100
}

/// <summary>
/// 标签选择器 (div, span)
/// </summary>
public class TagSelector : Selector
{
    public string TagName { get; set; }

    public TagSelector(string tagName)
    {
        TagName = tagName.ToLower();
    }

    public override bool Matches(Element element)
    {
        return element.TagName.Equals(TagName, StringComparison.OrdinalIgnoreCase);
    }

    public override int Specificity => 1; // 标签选择器特异性为1
}

/// <summary>
/// 通用选择器 (*)
/// </summary>
public class UniversalSelector : Selector
{
    public override bool Matches(Element element) => true;
    public override int Specificity => 0;
}
