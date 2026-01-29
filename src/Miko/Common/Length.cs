namespace Miko.Common;

/// <summary>
/// 长度值（支持像素、百分比、auto）
/// </summary>
public struct Length
{
    public float Value { get; set; }
    public LengthUnit Unit { get; set; }

    public Length(float value, LengthUnit unit = LengthUnit.Px)
    {
        Value = value;
        Unit = unit;
    }

    public static Length Px(float value) => new Length(value, LengthUnit.Px);
    public static Length Percent(float value) => new Length(value, LengthUnit.Percent);
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
            LengthUnit.Auto => 0,
            _ => 0
        };
    }

    public bool IsAuto => Unit == LengthUnit.Auto;

    public static implicit operator Length(float value) => new Length(value, LengthUnit.Px);

    public override string ToString()
    {
        return Unit switch
        {
            LengthUnit.Px => $"{Value}px",
            LengthUnit.Percent => $"{Value}%",
            LengthUnit.Auto => "auto",
            _ => Value.ToString()
        };
    }
}
