using System.ComponentModel;
using ModelContextProtocol.Server;
using Miko.DevTools;

namespace Miko.McpServer.Tools;

/// <summary>
/// DevTools MCP 工具集。通过 <see cref="IDevToolsService"/> 暴露 DOM 检查、样式查询、
/// 盒模型、模拟点击与日志读取能力。
/// </summary>
[McpServerToolType]
public sealed class DevToolsTools
{
    private readonly IDevToolsService _devTools;

    public DevToolsTools(IDevToolsService devTools)
    {
        _devTools = devTools;
    }

    [McpServerTool(Name = "devtools_get_dom_tree", ReadOnly = true)]
    [Description("获取当前应用完整的 DOM 树结构（JSON）。每个节点含 id、tagName、class、textContent 与 children。id 可用于其他 devtools 工具定位元素。")]
    public string GetDomTree() => _devTools.GetDomTree();

    [McpServerTool(Name = "devtools_find_element", ReadOnly = true)]
    [Description("按元素 id 查找单个元素，返回其摘要信息；未找到返回提示。")]
    public object FindElement(
        [Description("元素 id。可以是显式设置的 Id，或 DOM 树中自动生成的 'elem_XXXXXXXX'。")] string id)
    {
        var info = _devTools.FindElementById(id);
        return info ?? (object)$"未找到 id 为 '{id}' 的元素。";
    }

    [McpServerTool(Name = "devtools_query_elements", ReadOnly = true)]
    [Description("按简单 CSS 选择器查询元素列表。支持标签名（如 'div'）、类（如 '.btn'）、id（如 '#login'）。")]
    public IReadOnlyList<ElementInfo> QueryElements(
        [Description("选择器：'div' 匹配标签、'.klass' 匹配类、'#id' 匹配 id。")] string selector)
        => _devTools.QueryElements(selector);

    [McpServerTool(Name = "devtools_get_computed_style", ReadOnly = true)]
    [Description("获取指定元素经样式级联后的计算样式（JSON）。")]
    public object GetComputedStyle(
        [Description("目标元素 id。")] string elementId)
    {
        var style = _devTools.GetComputedStyle(elementId);
        return style ?? (object)$"元素 '{elementId}' 无计算样式（可能未布局或不存在）。";
    }

    [McpServerTool(Name = "devtools_get_box_model", ReadOnly = true)]
    [Description("获取指定元素的盒模型：位置、内容/内边距/边框/外边距尺寸（逻辑像素）。")]
    public object GetBoxModel(
        [Description("目标元素 id。")] string elementId)
    {
        var box = _devTools.GetBoxModel(elementId);
        return box ?? (object)$"元素 '{elementId}' 无布局盒（可能未布局或不存在）。";
    }

    [McpServerTool(Name = "devtools_click_element")]
    [Description("模拟点击指定元素（派发 click 事件，触发其事件处理器）。")]
    public string ClickElement(
        [Description("目标元素 id。")] string elementId)
    {
        var ok = _devTools.ClickElement(elementId);
        return ok ? $"已点击元素: {elementId}" : $"未找到元素: {elementId}";
    }

    [McpServerTool(Name = "devtools_get_logs", ReadOnly = true)]
    [Description("获取应用最近的日志条目（含级别、消息、时间戳）。")]
    public IReadOnlyList<LogEntryInfo> GetLogs(
        [Description("最多返回的条目数，默认 100。")] int maxCount = 100)
        => _devTools.GetLogs(maxCount);
}
