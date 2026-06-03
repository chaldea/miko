namespace Miko.Common;

/// <summary>
/// 长度值（支持像素、百分比、rem、auto）
/// </summary>
public struct Length
{
    public static float RootFontSize { get; set; } = 16f;

    public float Value { get; set; }
    public LengthUnit Unit { get; set; }

    public Length(float value, LengthUnit unit = LengthUnit.Px)
    {
        Value = value;
        Unit = unit;
    }

    public static Length Px(float value) => new Length(value, LengthUnit.Px);
    public static Length Percent(float value) => new Length(value, LengthUnit.Percent);
    public static Length Rem(float value) => new Length(value, LengthUnit.Rem);
    public static Length Auto => new Length(0, LengthUnit.Auto);

    /// <summary>
    /// 计算实际像素值
    /// </summary>
    /// <param name="containerSize">容器尺寸（用于百分比计算）</param>
    public float ToPixels(float containerSize)
    {
        return Unit switch
        {
            LengthUnit.Px => Value,
            LengthUnit.Percent => containerSize * Value / 100f,
            LengthUnit.Rem => Value * RootFontSize,
            LengthUnit.Auto => 0,
            _ => 0
        };
    }

    public bool IsAuto => Unit == LengthUnit.Auto;

    public static implicit operator Length(float value) => new Length(value, LengthUnit.Px);

    // ---- 算术运算符 ----
    // 单位规则：
    //   - 同单位：保留单位，对数值运算（如 Rem(0.375) + Rem(1) = Rem(1.375)）。
    //   - Px 与 Rem 混算：用 RootFontSize 换算为 Px 后运算，结果为 Px。
    //   - 含 Percent / Auto 的混合单位：缺少容器/布局上下文，无法确定，抛 InvalidOperationException。

    /// <summary>
    /// 将 Length 归一化为可直接相加的数值与单位。
    /// 同单位直接返回；Px/Rem 统一折算为 Px。
    /// </summary>
    private static (float a, float b, LengthUnit unit) Align(Length x, Length y)
    {
        if (x.Unit == y.Unit)
            return (x.Value, y.Value, x.Unit);

        // 仅 Px 与 Rem 之间可确定性换算
        bool xPxLike = x.Unit is LengthUnit.Px or LengthUnit.Rem;
        bool yPxLike = y.Unit is LengthUnit.Px or LengthUnit.Rem;
        if (xPxLike && yPxLike)
            return (ToPx(x), ToPx(y), LengthUnit.Px);

        throw new InvalidOperationException(
            $"Cannot combine Length values with units '{x.Unit}' and '{y.Unit}' without a layout context.");
    }

    /// <summary>
    /// 将 Px / Rem 折算为像素值（Rem 使用 RootFontSize）。
    /// </summary>
    private static float ToPx(Length l) => l.Unit switch
    {
        LengthUnit.Px => l.Value,
        LengthUnit.Rem => l.Value * RootFontSize,
        _ => throw new InvalidOperationException($"Unit '{l.Unit}' is not px-resolvable without context.")
    };

    public static Length operator +(Length x, Length y)
    {
        var (a, b, unit) = Align(x, y);
        return new Length(a + b, unit);
    }

    public static Length operator -(Length x, Length y)
    {
        var (a, b, unit) = Align(x, y);
        return new Length(a - b, unit);
    }

    public static Length operator -(Length x) => new Length(-x.Value, x.Unit);

    // 与标量运算：保留原单位，仅缩放/偏移数值。
    public static Length operator *(Length x, float factor) => new Length(x.Value * factor, x.Unit);
    public static Length operator *(float factor, Length x) => new Length(x.Value * factor, x.Unit);
    public static Length operator /(Length x, float divisor) => new Length(x.Value / divisor, x.Unit);

    public override string ToString()
    {
        return Unit switch
        {
            LengthUnit.Px => $"{Value}px",
            LengthUnit.Percent => $"{Value}%",
            LengthUnit.Rem => $"{Value}rem",
            LengthUnit.Auto => "auto",
            _ => Value.ToString()
        };
    }
}
