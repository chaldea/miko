namespace Miko.Styling;

/// <summary>
/// CSS 对象，支持嵌套选择器的样式声明
/// </summary>
public class CssObject : Style
{
    /// <summary>
    /// 合并操作符（spread）。以此为键赋值时，值不作为嵌套子选择器存储，而是把其样式属性
    /// 合并进当前规则本身（对应 SCSS 的 <c>@include</c> 混入）。等价于就地展开一组声明。
    /// </summary>
    public const string Spread = "...";

    private readonly Dictionary<string, CssObject> _children = new();
    private List<CssObject>? _mixins;

    public CssObject this[string selector]
    {
        get => selector == Spread
            ? throw new InvalidOperationException("The spread key \"...\" is write-only; it merges a mixin into the rule and cannot be read back.")
            : _children[selector];
        set
        {
            if (selector == Spread)
                (_mixins ??= new()).Add(value); // 按书写顺序累积，先合并者优先（见 CssObjectResolver）
            else
                _children[selector] = value;
        }
    }

    internal IReadOnlyDictionary<string, CssObject> Children => _children;

    /// <summary>
    /// 把 <paramref name="source"/> 的嵌套子选择器逐条并入本对象，同名选择器以 source 为准。
    /// 混入（<see cref="Spread"/>）不参与——它们属于 source 自身的规则，不是子选择器。
    ///
    /// <para>供样式库把若干片段合成一张总表用（见 <c>Miko.Ionic.Styles.GlobalStyle</c>）。
    /// <see cref="Children"/> 是 <c>internal</c>，程序集外原本只能靠
    /// <c>typeof(CssObject).GetProperty("Children", NonPublic | Instance)</c> 反射读取——
    /// 那是跨程序集的非公开反射，裁剪后会静默返回 null 并悄悄丢掉整张样式表（ISSUE-140）。</para>
    /// </summary>
    public void MergeChildrenFrom(CssObject source)
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var (selector, child) in source._children)
            _children[selector] = child;
    }

    /// <summary>通过 <see cref="Spread"/> 键合并进来的混入，按书写顺序排列。</summary>
    internal IReadOnlyList<CssObject> Mixins => (IReadOnlyList<CssObject>?)_mixins ?? Array.Empty<CssObject>();
}
