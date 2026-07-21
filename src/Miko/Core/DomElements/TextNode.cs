namespace Miko.Core.DomElements;

/// <summary>
/// 匿名文本节点（对应 DOM 的 Text 节点，非用户直接书写的标签）。
///
/// 历史上 Miko 把元素的直接文本存放在 <see cref="Element.TextContent"/> 单一字符串中，
/// 与 <see cref="Element.Children"/>（标签）互不感知顺序，因此无法表达
/// <c>text1 &lt;span/&gt; text3</c> 这样的文本-标签交错结构（见 ISSUE-086）。
///
/// 现在文本被提升为一等有序节点：<see cref="Miko.Components.RenderTreeBuilder"/> 把每段
/// 直接文本包装为一个 <see cref="TextNode"/> 追加进父元素的 <c>Children</c>，交错顺序由
/// 子节点列表天然表达。<see cref="Element.TextContent"/> 退化为便利外观（facade）：
/// 读取时拼接所有子 <see cref="TextNode"/> 文本，写入时重建单个前置文本节点。
///
/// 文本样式（字体、颜色、对齐、行高等）通过样式继承从父元素获得（Miko 无 CSS
/// <c>inherit</c> 关键字，但 <see cref="Styling.StyleResolver"/> 会把父元素的可继承
/// 计算值填入本节点），因此 <see cref="TextNode"/> 自身通常不需要设置样式。
/// </summary>
public sealed class TextNode : Element
{
    public override string TagName => "#text";

    public TextNode() { }

    public TextNode(string text)
    {
        Text = text;
    }

    /// <summary>
    /// 文本内容。这是文本节点唯一承载的数据，直接存于基类的原始文本存储中。
    /// </summary>
    public string Text
    {
        get => RawTextContent ?? "";
        set
        {
            if (RawTextContent != value)
            {
                RawTextContent = value;
                IsDirty = true;
                BumpMutationVersion();
            }
        }
    }

    /// <summary>
    /// 文本节点的 <see cref="Element.TextContent"/> 直接映射到其自身文本，
    /// 不走 facade 的「拼接子文本节点」逻辑（文本节点没有子节点）。
    /// </summary>
    public override string? TextContent
    {
        get => RawTextContent;
        set => Text = value ?? "";
    }

    public override string ToString() => $"#text(\"{Text}\")";

    // ---- 多行测量缓存（ISSUE-096）----
    // TextLayout 的换行测量（TextMeasurer.MeasureTextWithWrap）没有全局缓存，
    // 每次布局都为每个文本节点分配 StringBuilder / 行列表。这里以全部测量输入为键
    // 缓存最近一次结果：重排时输入通常不变（直接命中，零分配）；文本修改
    // （Text setter）或样式/约束变化自然导致键不匹配而重算，无需显式失效。
    internal string? McText;
    internal string? McFontFamily;
    internal float McFontSize;
    internal Miko.Common.FontWeight McFontWeight;
    internal float McAvailWidth;
    internal float McLineHeight;
    internal Miko.Common.WhiteSpace McWhiteSpace;
    internal bool McBreakLongWords;
    internal float McLetterSpacing;
    internal (float Width, float Height) McResult;
}
