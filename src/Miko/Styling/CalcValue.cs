using Miko.Common;

namespace Miko.Styling;

/// <summary>
/// 延迟求值的 calc 表达式（对应 CSS 的 <c>calc(...)</c>）。
/// 封装一个「变量作用域 → 长度」的闭包：变量的具体值只有在样式计算阶段、拿到作用域后才已知，
/// 因此 <c>-1 * Var("--bs-border-width")</c> 这类含变量的算术会先构造成 <see cref="CalcValue{T}"/>，
/// 推迟到 <see cref="ComputedStyle"/> 解析属性时再对当前作用域求值。
/// <para>
/// 运算按 <see cref="Length"/> 的分量语义进行（px/em/rem/percent 不提前折算），故 calc 结果里的
/// 相对单位会一路保留到布局阶段解析——无需在此重复实现单位算术。
/// </para>
/// <para>
/// 当前仅面向 <b>长度域</b>（实际构造的恒为 <c>T = Length</c>）：运算符只接受/产出
/// <see cref="Length"/> / <see cref="VarReference"/> / <c>float</c>，因此 Color/枚举/字符串
/// 天然无法参与 calc（编译期即被类型系统拦下），无需运行时判定。
/// </para>
/// </summary>
public readonly struct CalcValue<T>
{
    // 作用域 → 长度；返回 null 表示引用的变量未解析（缺失且无回退，或类型不匹配）。
    private readonly Func<Dictionary<string, VarValue>?, Length?> _eval;

    internal CalcValue(Func<Dictionary<string, VarValue>?, Length?> eval) => _eval = eval;

    /// <summary>
    /// 对给定的变量作用域求值。任一引用的变量未解析时返回 <c>false</c>（调用方据此保留默认值，
    /// 与未解析的 <c>Var(...)</c> 行为一致）。
    /// </summary>
    public bool TryEvaluate(Dictionary<string, VarValue>? scope, out T value)
    {
        if (_eval != null && _eval(scope) is { } length)
        {
            // 长度域：内部恒为 Length，此处的装箱转换在 T == Length 时是恒等的。
            value = (T)(object)length;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>从变量引用创建 calc 叶子：按现有 Var 解析路径（作用域命中 → 回退值）取长度。</summary>
    public static CalcValue<T> FromVar(VarReference reference)
    {
        // VarReference 是结构体：闭包不能捕获 this，故先复制字段到局部（见 CS1673）。
        var name = reference.Name;
        var fallback = reference.Fallback;
        return new CalcValue<T>(scope =>
        {
            if (scope != null && scope.TryGetValue(name, out var resolved) && resolved.TryGet<Length>(out var value))
                return value;
            if (fallback is { } fb && fb.TryGet<Length>(out var fbValue))
                return fbValue;
            return null;
        });
    }

    /// <summary>从具体长度创建 calc 叶子（恒定值）。</summary>
    public static CalcValue<T> FromLength(Length value) => new CalcValue<T>(_ => value);

    // ---- 运算符 ----
    // 全部返回 CalcValue<T>，按 Length 语义组合；任一操作数未解析（null）则整体未解析（null）。

    public static CalcValue<T> operator -(CalcValue<T> x)
    {
        var xe = x._eval;
        return new CalcValue<T>(s => xe(s) is { } l ? -l : (Length?)null);
    }

    public static CalcValue<T> operator *(float factor, CalcValue<T> x)
    {
        var xe = x._eval;
        return new CalcValue<T>(s => xe(s) is { } l ? factor * l : (Length?)null);
    }

    public static CalcValue<T> operator *(CalcValue<T> x, float factor) => factor * x;

    public static CalcValue<T> operator /(CalcValue<T> x, float divisor)
    {
        var xe = x._eval;
        return new CalcValue<T>(s => xe(s) is { } l ? l / divisor : (Length?)null);
    }

    public static CalcValue<T> operator +(CalcValue<T> x, CalcValue<T> y)
    {
        var xe = x._eval;
        var ye = y._eval;
        return new CalcValue<T>(s => xe(s) is { } a && ye(s) is { } b ? a + b : (Length?)null);
    }

    public static CalcValue<T> operator -(CalcValue<T> x, CalcValue<T> y)
    {
        var xe = x._eval;
        var ye = y._eval;
        return new CalcValue<T>(s => xe(s) is { } a && ye(s) is { } b ? a - b : (Length?)null);
    }

    public static CalcValue<T> operator +(CalcValue<T> x, Length y) => x + FromLength(y);
    public static CalcValue<T> operator +(Length x, CalcValue<T> y) => FromLength(x) + y;
    public static CalcValue<T> operator -(CalcValue<T> x, Length y) => x - FromLength(y);
    public static CalcValue<T> operator -(Length x, CalcValue<T> y) => FromLength(x) - y;

    // 与变量引用交叉运算（VarReference 侧同样提供对称重载，便于链式书写）。
    public static CalcValue<T> operator +(CalcValue<T> x, VarReference y) => x + FromVar(y);
    public static CalcValue<T> operator +(VarReference x, CalcValue<T> y) => FromVar(x) + y;
    public static CalcValue<T> operator -(CalcValue<T> x, VarReference y) => x - FromVar(y);
    public static CalcValue<T> operator -(VarReference x, CalcValue<T> y) => FromVar(x) - y;

    public override string ToString() => "calc(...)";
}

/// <summary>
/// calc 闭包的作用域句柄。作为 <c>Calc(s =&gt; ...)</c> lambda 的形参，提供与
/// <see cref="Css.Var(string)"/> 等价的 <c>s.Var("--x")</c> 写法（返回同一个 <see cref="VarReference"/>，
/// 因此 <c>s.Var(...)</c> 与自由函数 <c>Var(...)</c> 可互换）。它同时给 lambda 一个具体委托类型，
/// 使 <c>Calc(s =&gt; ...)</c> 无需类型标注即可推断。
/// </summary>
public readonly struct CalcScope
{
    /// <summary>创建对自定义变量 <paramref name="name"/> 的引用（等价于 <see cref="Css.Var(string)"/>）。</summary>
    public VarReference Var(string name) => new(name);

    /// <summary>创建带回退值的变量引用（等价于 <see cref="Css.Var(string, VarValue)"/>）。</summary>
    public VarReference Var(string name, VarValue fallback) => new(name, fallback);
}
