namespace Miko.Styling;

/// <summary>
/// 对某个自定义样式变量的引用（如 <c>var(--button-color)</c>）。
/// <paramref name="Name"/> 为变量名（约定以 <c>--</c> 开头），
/// <paramref name="Fallback"/> 为变量在作用域内未定义时使用的回退值。
/// </summary>
public readonly record struct VarReference(string Name, VarValue? Fallback = null);

/// <summary>
/// 样式辅助方法的静态入口。通过 <c>using static Miko.Styling.Css;</c> 即可直接书写
/// <c>Color = Var("--button-color")</c>。
/// </summary>
public static class Css
{
    /// <summary>创建对自定义变量 <paramref name="name"/> 的引用。</summary>
    public static VarReference Var(string name) => new(name);

    /// <summary>创建带回退值的变量引用；变量在作用域内未定义时使用 <paramref name="fallback"/>。</summary>
    public static VarReference Var(string name, VarValue fallback) => new(name, fallback);

    // CSS 全局关键词的全局静态引用。经 StyleProperty<T> 的隐式转换可直接赋给任意样式属性，
    // 例如 using static Miko.Styling.Css; 后书写 style.Color = Initial;
    /// <summary>重置为属性的初始（默认）值。见 <see cref="StyleKeyword.Initial"/>。</summary>
    public const StyleKeyword Initial = StyleKeyword.Initial;

    /// <summary>取父元素该属性的计算值。见 <see cref="StyleKeyword.Inherit"/>。</summary>
    public const StyleKeyword Inherit = StyleKeyword.Inherit;

    /// <summary>可继承属性等价于 inherit，否则等价于 initial。见 <see cref="StyleKeyword.Unset"/>。</summary>
    public const StyleKeyword Unset = StyleKeyword.Unset;

    /// <summary>回退到 UA 层（Miko 中等价于 unset）。见 <see cref="StyleKeyword.Revert"/>。</summary>
    public const StyleKeyword Revert = StyleKeyword.Revert;

    /// <summary>回退到上一级联层（Miko 中等价于 unset）。见 <see cref="StyleKeyword.RevertLayer"/>。</summary>
    public const StyleKeyword RevertLayer = StyleKeyword.RevertLayer;
}
