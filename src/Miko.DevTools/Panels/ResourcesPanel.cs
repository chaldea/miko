using Miko.Common;
using Miko.Core.DomElements;
using Miko.Platform.Resources;
using Miko.Styling;

namespace Miko.DevTools.Panels;

/// <summary>
/// Resources 面板：当前应用的资产清单（ISSUE-139）。三节内容：
/// <list type="bullet">
///   <item><b>Embedded</b> — 注册程序集里的嵌入资源（可书写路径、清单名、所属程序集、大小）。
///     数据来自程序集元数据，与加载器无关，故总能显示。</item>
///   <item><b>Network</b> — <b>由 Miko 下载的</b>网络资源（地址、大小、耗时）。应用自己用
///     <c>HttpClient</c> 拉的字节不经过加载器，也就无从记录。</item>
///   <item><b>Cache</b> — 加载器的解码缓存（源、协议、像素尺寸、位图字节数、状态）。</item>
/// </list>
/// 后两节需要加载器实现 <see cref="IResourceDiagnostics"/>（默认的
/// <see cref="ResourceManager"/> 实现了）。
/// </summary>
internal static class ResourcesPanel
{
    /// <summary>
    /// 单节最多渲染的行数。每行会展开成多个元素，需要限制 DOM 规模——一个大应用的嵌入
    /// 资源清单可达上千条（Ionic 图标集本身就是数千个 SVG）。
    /// </summary>
    private const int MaxRenderedRows = 300;

    public static DivElement Build(DevToolsBridge bridge, bool visible)
    {
        var panel = new DivElement { Class = "resources-panel" };
        if (!visible)
        {
            panel.Style = new Style { Display = Display.None };
        }

        var output = new DivElement { Class = "resources-output" };

        BuildEmbeddedSection(output, bridge);
        BuildNetworkSection(output, bridge);
        BuildCacheSection(output, bridge);

        panel.AddChild(output);
        return panel;
    }

    // ---------------------------------------------------------------------
    // 嵌入资源
    // ---------------------------------------------------------------------

    private static void BuildEmbeddedSection(DivElement parent, DevToolsBridge bridge)
    {
        var resources = EmbeddedResources.Enumerate(bridge.ResourceAssemblies);

        long totalBytes = 0;
        foreach (var r in resources) totalBytes += r.ByteCount;

        parent.AddChild(SectionTitle(
            "Embedded Resources",
            resources.Count == 0
                ? "none"
                : $"{resources.Count} items · {FormatBytes(totalBytes)}"));

        if (bridge.ResourceAssemblies == null)
        {
            parent.AddChild(Empty("No resource assembly provider registered"));
            return;
        }

        if (resources.Count == 0)
        {
            parent.AddChild(Empty("No embedded resources in the registered assemblies"));
            return;
        }

        parent.AddChild(HeaderRow("res:// path", "assembly", "size"));

        int rendered = 0;
        foreach (var resource in resources)
        {
            if (rendered++ >= MaxRenderedRows) break;

            var row = Row();
            // 主列显示可书写的 res:// 路径（用户要复制的就是这个），清单名作为副标题。
            row.AddChild(PrimaryCell("res://" + resource.LogicalPath, resource.ManifestName));
            row.AddChild(Cell(resource.AssemblyName, "res-cell-assembly"));
            row.AddChild(Cell(FormatBytes(resource.ByteCount), "res-cell-size"));
            parent.AddChild(row);
        }

        AddTruncationNote(parent, resources.Count, rendered);
    }

    // ---------------------------------------------------------------------
    // 网络资源
    // ---------------------------------------------------------------------

    private static void BuildNetworkSection(DivElement parent, DevToolsBridge bridge)
    {
        var diagnostics = bridge.ResourceDiagnostics;
        var requests = diagnostics?.GetNetworkResources() ?? Array.Empty<NetworkResourceInfo>();

        long totalBytes = 0;
        foreach (var r in requests) totalBytes += r.ByteCount;

        parent.AddChild(SectionTitle(
            "Network Resources",
            requests.Count == 0
                ? "none"
                : $"{requests.Count} requests · {FormatBytes(totalBytes)}"));

        if (diagnostics == null)
        {
            parent.AddChild(Empty("The registered image loader does not report diagnostics"));
            return;
        }

        if (requests.Count == 0)
        {
            parent.AddChild(Empty("Nothing downloaded by Miko yet"));
            return;
        }

        parent.AddChild(HeaderRow("url", "size", "time"));

        // 最近的请求排在最前：这是排查时真正关心的那些。
        int rendered = 0;
        for (int i = requests.Count - 1; i >= 0 && rendered < MaxRenderedRows; i--, rendered++)
        {
            var request = requests[i];
            var row = Row();
            row.AddChild(PrimaryCell(
                request.Url,
                request.Succeeded
                    ? request.CompletedAt.ToString("HH:mm:ss")
                    : $"{request.CompletedAt:HH:mm:ss} · {request.Error}"));
            row.AddChild(Cell(request.Succeeded ? FormatBytes(request.ByteCount) : "failed", "res-cell-size"));
            row.AddChild(Cell(FormatDuration(request.Duration), "res-cell-size"));
            parent.AddChild(row);
        }

        AddTruncationNote(parent, requests.Count, rendered);
    }

    // ---------------------------------------------------------------------
    // 缓存资源
    // ---------------------------------------------------------------------

    private static void BuildCacheSection(DivElement parent, DevToolsBridge bridge)
    {
        var diagnostics = bridge.ResourceDiagnostics;
        var cached = diagnostics?.GetCachedResources() ?? Array.Empty<CachedResourceInfo>();

        long totalBytes = 0;
        foreach (var c in cached) totalBytes += c.ByteCount;

        parent.AddChild(SectionTitle(
            "Cached Resources",
            cached.Count == 0
                ? "none"
                : $"{cached.Count} entries · {FormatBytes(totalBytes)} decoded"));

        if (diagnostics == null)
        {
            parent.AddChild(Empty("The registered image loader does not report diagnostics"));
            return;
        }

        if (cached.Count == 0)
        {
            parent.AddChild(Empty("Decode cache is empty"));
            return;
        }

        parent.AddChild(HeaderRow("source", "pixels", "size"));

        int rendered = 0;
        foreach (var entry in cached)
        {
            if (rendered++ >= MaxRenderedRows) break;

            var row = Row();
            row.AddChild(PrimaryCell(entry.Source, $"{FormatScheme(entry.Scheme)} · {FormatState(entry.State)}"));
            row.AddChild(Cell(
                entry.State == CachedResourceState.Loaded ? $"{entry.PixelWidth}×{entry.PixelHeight}" : "—",
                "res-cell-size"));
            row.AddChild(Cell(
                entry.State == CachedResourceState.Loaded ? FormatBytes(entry.ByteCount) : "—",
                "res-cell-size"));
            parent.AddChild(row);
        }

        AddTruncationNote(parent, cached.Count, rendered);
    }

    // ---------------------------------------------------------------------
    // 行构件
    // ---------------------------------------------------------------------

    private static DivElement SectionTitle(string title, string summary)
    {
        var row = new DivElement { Class = "res-section-title" };
        row.AddChild(new SpanElement { TextContent = title });
        row.AddChild(new SpanElement { Class = "res-section-summary", TextContent = summary });
        return row;
    }

    private static DivElement HeaderRow(string primary, string second, string third)
    {
        var row = new DivElement { Class = "res-row res-row-header" };
        row.AddChild(Cell(primary, "res-cell-primary"));
        row.AddChild(Cell(second, "res-cell-assembly"));
        row.AddChild(Cell(third, "res-cell-size"));
        return row;
    }

    private static DivElement Row() => new() { Class = "res-row" };

    /// <summary>主列：一行主文本 + 一行灰色副文本（清单名 / 时间戳 / 协议与状态）。</summary>
    private static DivElement PrimaryCell(string primary, string secondary)
    {
        var cell = new DivElement { Class = "res-cell-primary" };
        cell.AddChild(new DivElement { Class = "res-cell-path", TextContent = primary });
        cell.AddChild(new DivElement { Class = "res-cell-sub", TextContent = secondary });
        return cell;
    }

    private static DivElement Cell(string text, string cssClass) =>
        new() { Class = cssClass, TextContent = text };

    private static DivElement Empty(string text) =>
        new() { Class = "console-empty", TextContent = text };

    private static void AddTruncationNote(DivElement parent, int total, int rendered)
    {
        if (total > rendered)
            parent.AddChild(Empty($"… {total - rendered} more not shown"));
    }

    // ---------------------------------------------------------------------
    // 格式化
    // ---------------------------------------------------------------------

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.#} KB";
        return $"{bytes / (1024.0 * 1024.0):0.##} MB";
    }

    private static string FormatDuration(TimeSpan duration) =>
        duration.TotalMilliseconds < 1000
            ? $"{duration.TotalMilliseconds:0} ms"
            : $"{duration.TotalSeconds:0.##} s";

    private static string FormatScheme(MediaSourceScheme scheme) => scheme switch
    {
        MediaSourceScheme.File => "file",
        MediaSourceScheme.Resource => "res",
        MediaSourceScheme.Http => "http",
        MediaSourceScheme.Https => "https",
        MediaSourceScheme.Data => "data",
        _ => "none",
    };

    private static string FormatState(CachedResourceState state) => state switch
    {
        CachedResourceState.Loading => "loading",
        CachedResourceState.Loaded => "loaded",
        CachedResourceState.Failed => "failed",
        _ => "unknown",
    };
}
