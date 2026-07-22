namespace Miko.Core.DomElements;

using Miko.Highlight;

/// <summary>
/// 预格式化文本元素 (pre)。
///
/// <c>pre</c> 是块级元素，其 UA 默认样式（<see cref="Styling.StyleResolver"/>）为
/// <c>white-space: pre</c> + 等宽字体：文本中的空格、制表符与换行符全部保留，
/// 不做空白折叠、不做软换行。Razor 编译器对 <c>&lt;pre&gt;</c> 子树的空白裁剪
/// 也会被跳过（见 Miko.Razor.Compiler 的 ComponentWhitespacePass /
/// ComponentRuntimeNodeWriter），因此标记中的缩进与换行可原样到达 DOM。
/// </summary>
public class PreElement : Element
{
    public override string TagName => "pre";
}

/// <summary>
/// 代码元素 (code)。
///
/// <c>code</c> 是行内语义标签，UA 默认样式为等宽字体。除语义外，Miko 为其扩展了
/// 两个非标准属性（Miko 兼容浏览器标准但非严格遵循，见 ISSUE-098）：
/// <list type="bullet">
/// <item><c>language</c>：代码语言（如 "csharp"、"json"）。设置后默认启用语法高亮。</item>
/// <item><c>highlight</c>：显式 <c>"false"</c> 可关闭高亮，其余取值（含缺省）视为启用。</item>
/// </list>
/// 高亮在绘制阶段按 token 范围着色（见 <see cref="Rendering.Painter.DrawHighlightedText"/>），
/// DOM 保持单一 <see cref="TextNode"/> 文本子节点，布局与测量不受影响。
/// </summary>
public class CodeElement : Element
{
    public override string TagName => "code";

    private string? _language;

    /// <summary>代码语言标识（如 "csharp"、"json"、"xml"）。设置后默认启用语法高亮。</summary>
    public string? Language
    {
        get => _language;
        set
        {
            if (_language != value)
            {
                _language = value;
                _highlightText = null;
                _highlightTokens = null;
                IsDirty = true;
            }
        }
    }

    /// <summary>
    /// 是否启用语法高亮。缺省为 true——只要设置了 <see cref="Language"/> 即高亮，
    /// 除非显式 <c>highlight="false"</c>。
    /// </summary>
    public bool Highlight { get; set; } = true;

    /// <summary>设置了语言且未显式关闭时，语法高亮生效。</summary>
    public bool IsHighlightActive => Highlight && !string.IsNullOrWhiteSpace(Language);

    // 高亮 token 缓存：以「预处理后的文本 + 高亮器实例」为键（white-space 处理在绘制前
    // 完成，tab 展开/换行归一会改变偏移量，因此 token 必须针对绘制用文本计算）。
    // 文本、语言或高亮器变化时键不再匹配，自然重算；未变时连续绘制零分配。
    private string? _highlightText;
    private ISyntaxHighlighter? _highlighter;
    private IReadOnlyList<CodeToken>? _highlightTokens;

    /// <summary>
    /// 获取绘制用文本的高亮 token 序列。<paramref name="highlighter"/> 由渲染引擎
    /// 从 DI 解析后传入。未知语言返回 null（调用方按普通文本绘制）。
    /// </summary>
    internal IReadOnlyList<CodeToken>? GetHighlightTokens(string processedText, ISyntaxHighlighter highlighter)
    {
        if (!IsHighlightActive) return null;
        if (_highlightText != processedText || !ReferenceEquals(_highlighter, highlighter))
        {
            _highlightTokens = highlighter.Tokenize(processedText, Language!);
            _highlightText = processedText;
            _highlighter = highlighter;
        }
        return _highlightTokens;
    }
}
/// <summary>
/// 强制换行元素 (br)。
///
/// <c>br</c> 是行内级的空元素：它不产生任何可见盒，但会强制结束当前行盒，
/// 使其后的行内内容排到新的一行。布局阶段由
/// <see cref="Layout.LayoutAlgorithms.BlockLayout"/> 识别并作为“行分隔标记”处理
/// （见 <see cref="Layout.LayoutAlgorithms.BlockLayout.IsForcedLineBreak"/>）。
/// </summary>
public class BrElement : Element
{
    public override string TagName => "br";
}

/// <summary>
/// 主题分隔线元素 (hr)。
///
/// <c>hr</c> 是块级元素，浏览器默认渲染为一条水平分隔线。Miko 通过 UA 默认样式
/// （<see cref="Styling.StyleResolver"/>）为其设置一条上边框来绘制该线条，内容高度为 0，
/// 因此无需在渲染层做特殊处理，直接复用既有的边框绘制路径。
/// </summary>
public class HrElement : Element
{
    public override string TagName => "hr";
}
