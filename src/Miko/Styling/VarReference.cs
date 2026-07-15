using Miko.Common;

namespace Miko.Styling;

/// <summary>
/// 对某个自定义样式变量的引用（如 <c>var(--button-color)</c>）。
/// <paramref name="Name"/> 为变量名（约定以 <c>--</c> 开头），
/// <paramref name="Fallback"/> 为变量在作用域内未定义时使用的回退值。
/// </summary>
public readonly record struct VarReference(string Name, VarValue? Fallback = null)
{
    // ---- calc 算术运算符 ----
    // 对变量引用做算术（如 -1 * Var("--bs-border-width")）会构造成延迟求值的 CalcValue<Length>，
    // 在样式计算阶段拿到作用域后才折算。变量在此按长度域解析（见 CalcValue 说明）。

    public static CalcValue<Length> operator -(VarReference x) => -CalcValue<Length>.FromVar(x);

    public static CalcValue<Length> operator *(float factor, VarReference x) => factor * CalcValue<Length>.FromVar(x);
    public static CalcValue<Length> operator *(VarReference x, float factor) => factor * CalcValue<Length>.FromVar(x);

    public static CalcValue<Length> operator /(VarReference x, float divisor) => CalcValue<Length>.FromVar(x) / divisor;

    public static CalcValue<Length> operator +(VarReference x, VarReference y) => CalcValue<Length>.FromVar(x) + CalcValue<Length>.FromVar(y);
    public static CalcValue<Length> operator -(VarReference x, VarReference y) => CalcValue<Length>.FromVar(x) - CalcValue<Length>.FromVar(y);

    public static CalcValue<Length> operator +(VarReference x, Length y) => CalcValue<Length>.FromVar(x) + y;
    public static CalcValue<Length> operator +(Length x, VarReference y) => x + CalcValue<Length>.FromVar(y);
    public static CalcValue<Length> operator -(VarReference x, Length y) => CalcValue<Length>.FromVar(x) - y;
    public static CalcValue<Length> operator -(Length x, VarReference y) => x - CalcValue<Length>.FromVar(y);
}

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

    // ---- calc(...) ----
    // 含变量的算术（如 -1 * Var("--x")）本身即为 CalcValue<T>，可直接赋给样式属性
    // （经 StyleProperty<T> 的隐式转换）。Calc 为可读的 CSS 风格别名/包装：
    //   直接：MarginLeft = -1 * Var("--x");
    //   包装：MarginLeft = Calc(-1 * Var("--x"));
    //   带作用域 lambda：MarginLeft = Calc(s => -1 * Var("--x"));  或  Calc(s => -1 * s.Var("--x"));
    // lambda 形参 s（CalcScope）提供与自由函数 Var(...) 等价的 s.Var(...)，并让 lambda 具备
    // 具体委托类型以便类型推断。

    /// <summary>calc 表达式的透传别名（读作 CSS 的 <c>calc(...)</c>）。</summary>
    public static CalcValue<T> Calc<T>(CalcValue<T> expr) => expr;

    /// <summary>
    /// 以作用域 lambda 构造 calc 表达式：<c>Calc(s =&gt; -1 * Var("--x"))</c> 或
    /// <c>Calc(s =&gt; -1 * s.Var("--x"))</c>。<paramref name="factory"/> 立即执行以构建延迟闭包
    /// （实际求值仍推迟到样式计算阶段）。
    /// </summary>
    public static CalcValue<T> Calc<T>(Func<CalcScope, CalcValue<T>> factory) => factory(new CalcScope());

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
