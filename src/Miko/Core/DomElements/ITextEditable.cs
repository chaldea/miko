using System.Globalization;

namespace Miko.Core.DomElements;

/// <summary>
/// 可编辑文本控件的公共抽象：由 <see cref="InputElement"/>（单行文本类）与
/// <see cref="TextAreaElement"/>（多行）实现，供
/// <see cref="Miko.Platform.MikoInteractionController"/> 以统一方式处理键盘编辑与文本输入
/// （插入、退格、删除、光标移动），避免为每种控件重复实现相同的编辑逻辑。
///
/// <para><b>索引与计数单位（ISSUE-154 BUG-04）。</b>与 DOM 的 <c>selectionStart</c> /
/// <c>maxlength</c> 一致，位置与长度一律按 <see cref="Value"/> 的 <b>UTF-16 码元</b>计：
/// <see cref="CursorPosition"/>、输入法选区（<c>InputMethodState.SelectionStart/End</c>、
/// <c>MikoInteractionController.SetTextSelection</c>）以及 <c>MaxLength</c> 都是码元数。
/// 编辑操作则以<b>字素簇</b>（用户感知的一个字符：emoji、组合重音、ZWJ 序列、国旗）为单位：
/// 插入在 <c>MaxLength</c> 处截断时不会拆开字素簇（因而不会留下孤立代理项），退格/删除/左右
/// 移动一次跨过一整个字素簇。落在字素簇中间的位置在下一次编辑时先退到该簇的起点。</para>
/// </summary>
public interface ITextEditable
{
    /// <summary>当前文本值</summary>
    string? Value { get; set; }

    /// <summary>光标位置：<see cref="Value"/> 中的 UTF-16 码元索引，编辑后总落在字素簇边界上。</summary>
    int CursorPosition { get; set; }

    /// <summary>是否接受编辑（如 InputElement 仅在文本/密码类型时可编辑）</summary>
    bool IsEditable { get; }

    /// <summary>是否为多行控件（多行控件的回车键应插入换行而非提交）</summary>
    bool IsMultiline { get; }

    /// <summary>
    /// 在光标处插入整段文本。超出 <c>MaxLength</c> 的部分按字素簇截掉（一个簇要么整个插入，
    /// 要么整个丢弃）；光标随后位于插入内容之后。
    /// </summary>
    void InsertText(string text);

    /// <summary>删除光标前一个字素簇，成功返回 true</summary>
    bool Backspace();

    /// <summary>删除光标后一个字素簇，成功返回 true</summary>
    bool Delete();

    /// <summary>将光标移动到文本末尾</summary>
    void MoveCursorToEnd();
}

/// <summary>
/// 字素簇（UAX #29 扩展字素簇，经 <see cref="StringInfo"/>）边界计算，供文本控件与控制器共用。
///
/// <para>所有位置都是 UTF-16 码元索引。扫描从目标位置之前最近的换行符之后开始：换行符之后
/// 总是字素簇边界（UAX #29 GB4），因此多行长文本里的一次光标移动不必从头扫描整篇文本。</para>
/// </summary>
internal static class TextEditing
{
    /// <summary>
    /// 按 <c>maxlength</c> 截断待插入的文本：只保留还放得下的完整字素簇（HTML 按 UTF-16 码元计数）。
    /// 截断永远落在字素簇边界上，不会留下孤立代理项或拆开的组合序列。
    /// </summary>
    internal static string LimitInsertion(string text, int currentLength, int? maxLength)
    {
        if (maxLength is not { } max) return text;
        var room = Math.Max(0, max - currentLength);
        if (text.Length <= room) return text;

        int fitted = 0;
        while (fitted < text.Length)
        {
            var next = fitted + StringInfo.GetNextTextElementLength(text.AsSpan(fitted));
            if (next > room) break;
            fitted = next;
        }
        return text[..fitted];
    }

    /// <summary>不大于 <paramref name="index"/> 的最近字素簇边界（<paramref name="index"/> 本身是边界时返回它）。</summary>
    internal static int SnapToBoundary(string text, int index)
    {
        if (index <= 0) return 0;
        if (index >= text.Length) return text.Length;

        var boundary = ScanStart(text, index);
        while (true)
        {
            var next = boundary + StringInfo.GetNextTextElementLength(text.AsSpan(boundary));
            if (next > index) return boundary;
            boundary = next;
        }
    }

    /// <summary><paramref name="index"/> 之前的最近字素簇边界（退格删除、左移的目标）；已在开头时返回 0。</summary>
    internal static int PreviousBoundary(string text, int index)
    {
        if (index <= 0) return 0;
        index = Math.Min(index, text.Length);

        // 从 index - 1 之前的边界开始扫描，返回包含 index - 1 那个码元的字素簇的起点。
        var boundary = ScanStart(text, index - 1);
        while (true)
        {
            var next = boundary + StringInfo.GetNextTextElementLength(text.AsSpan(boundary));
            if (next >= index) return boundary;
            boundary = next;
        }
    }

    /// <summary><paramref name="index"/> 之后的最近字素簇边界（向前删除、右移的目标）；已在末尾时返回文本长度。</summary>
    internal static int NextBoundary(string text, int index)
    {
        if (index >= text.Length) return text.Length;
        index = Math.Max(index, 0);

        var boundary = ScanStart(text, index);
        while (boundary <= index)
            boundary += StringInfo.GetNextTextElementLength(text.AsSpan(boundary));
        return boundary;
    }

    /// <summary>不大于 <paramref name="index"/> 的一个已知边界：其前最近换行符之后的位置（或 0）。</summary>
    private static int ScanStart(string text, int index)
    {
        if (index <= 0) return 0;
        var lineFeed = text.LastIndexOf('\n', index - 1);
        return lineFeed < 0 ? 0 : lineFeed + 1;
    }
}
