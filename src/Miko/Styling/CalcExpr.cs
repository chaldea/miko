using Miko.Common;

namespace Miko.Styling;

/// <summary>
/// CSS calc() 表达式节点，支持对变量与常量进行延迟的算术运算。
/// </summary>
/// <remarks>
/// 与 <see cref="VarReference"/>（直接引用一个变量）不同，<see cref="CalcExpr"/> 表示一个
/// 需要在 <see cref="CustomPropertyScope"/> 可用时才能求值的表达式树，例如
/// <c>calc(-1 * var(--bs-border-width))</c>。
///
/// 数值统一以 <see cref="float"/> 参与运算；单位语义取第一个带单位的 Length 操作数
/// （默认 Px），足以覆盖以像素为主的运算。混合单位（% 与 px 互算）不做完整 CSS 语义。
/// </remarks>
public readonly struct CalcExpr
{
    // 延迟求值：在 scope 可用时计算出数值与单位
    private readonly Func<CustomPropertyScope, (float value, LengthUnit unit)> _eval;

    private CalcExpr(Func<CustomPropertyScope, (float, LengthUnit)> eval) => _eval = eval;

    /// <summary>
    /// 包装一个常量数值（无单位，默认按 Px 处理）
    /// </summary>
    public static CalcExpr Constant(float value) => new(_ => (value, LengthUnit.Px));

    /// <summary>
    /// 包装一个 Length（保留其单位）
    /// </summary>
    public static CalcExpr FromLength(Length length) => new(_ => (length.Value, length.Unit));

    /// <summary>
    /// 引用一个 CSS 变量。变量未定义时取其 Length fallback 或 0。
    /// </summary>
    public static CalcExpr FromVar(VarReference var) => new(scope =>
    {
        var resolved = scope.Get<Length>(var.Name);
        if (resolved.HasValue)
            return (resolved.Value.Value, resolved.Value.Unit);

        // 尝试 float 类型的变量（如 line-height 数值）
        var asFloat = scope.Get<float>(var.Name);
        if (asFloat.HasValue)
            return (asFloat.Value, LengthUnit.Px);

        var asInt = scope.Get<int>(var.Name);
        if (asInt.HasValue)
            return (asInt.Value, LengthUnit.Px);

        // 未定义：使用 Length fallback，否则 0
        if (var.Fallback is Length fallbackLength)
            return (fallbackLength.Value, fallbackLength.Unit);

        return (0f, LengthUnit.Px);
    });

    /// <summary>
    /// 求值为 Length（保留运算过程中确定的单位）
    /// </summary>
    public Length ToLength(CustomPropertyScope scope)
    {
        var (value, unit) = _eval(scope);
        return new Length(value, unit);
    }

    /// <summary>
    /// 求值为 float（忽略单位，取数值）
    /// </summary>
    public float ToFloat(CustomPropertyScope scope) => _eval(scope).value;

    /// <summary>
    /// 求值为 int（向最近整数取整）
    /// </summary>
    public int ToInt(CustomPropertyScope scope) => (int)MathF.Round(_eval(scope).value);

    // ---- 隐式提升 ----

    public static implicit operator CalcExpr(float value) => Constant(value);
    public static implicit operator CalcExpr(int value) => Constant(value);
    public static implicit operator CalcExpr(Length length) => FromLength(length);
    public static implicit operator CalcExpr(VarReference var) => FromVar(var);

    // ---- 算术运算符 ----
    // 单位选取规则：若任一操作数为非 Px 单位，则结果沿用该单位；否则保持 Px。

    private static CalcExpr Combine(CalcExpr a, CalcExpr b, Func<float, float, float> op) =>
        new(scope =>
        {
            var (av, au) = a._eval(scope);
            var (bv, bu) = b._eval(scope);
            var unit = au != LengthUnit.Px ? au : bu;
            return (op(av, bv), unit);
        });

    public static CalcExpr operator +(CalcExpr a, CalcExpr b) => Combine(a, b, static (x, y) => x + y);
    public static CalcExpr operator -(CalcExpr a, CalcExpr b) => Combine(a, b, static (x, y) => x - y);
    public static CalcExpr operator *(CalcExpr a, CalcExpr b) => Combine(a, b, static (x, y) => x * y);
    public static CalcExpr operator /(CalcExpr a, CalcExpr b) => Combine(a, b, static (x, y) => x / y);

    public static CalcExpr operator -(CalcExpr a) =>
        new(scope =>
        {
            var (v, u) = a._eval(scope);
            return (-v, u);
        });
}
