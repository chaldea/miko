using Miko.Core;

namespace Miko.DevTools;

/// <summary>
/// DevTools服务接口，对外暴露DOM检查和调试能力。
/// </summary>
public interface IDevToolsService
{
    /// <summary>获取DOM树结构的JSON表示。</summary>
    string GetDomTree();

    /// <summary>根据元素ID查找元素。</summary>
    ElementInfo? FindElementById(string id);

    /// <summary>根据CSS选择器查找元素列表。</summary>
    IReadOnlyList<ElementInfo> QueryElements(string selector);

    /// <summary>获取指定元素的计算样式。</summary>
    string? GetComputedStyle(string elementId);

    /// <summary>模拟点击指定元素。</summary>
    bool ClickElement(string elementId);

    /// <summary>获取指定元素的盒模型信息。</summary>
    BoxModelInfo? GetBoxModel(string elementId);

    /// <summary>获取所有日志条目。</summary>
    IReadOnlyList<LogEntryInfo> GetLogs(int maxCount = 100);
}

/// <summary>元素信息（对外暴露的简化版本）。</summary>
public sealed record ElementInfo(
    string Id,
    string TagName,
    string? Class,
    string? TextContent,
    int ChildCount);

/// <summary>盒模型信息。</summary>
public sealed record BoxModelInfo(
    float X,
    float Y,
    float Width,
    float Height,
    float ContentWidth,
    float ContentHeight,
    float PaddingTop,
    float PaddingRight,
    float PaddingBottom,
    float PaddingLeft,
    float BorderTop,
    float BorderRight,
    float BorderBottom,
    float BorderLeft,
    float MarginTop,
    float MarginRight,
    float MarginBottom,
    float MarginLeft);

/// <summary>日志条目信息。</summary>
public sealed record LogEntryInfo(
    string Level,
    string Message,
    string Timestamp);
