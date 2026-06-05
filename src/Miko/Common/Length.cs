namespace Miko.Common;

/// <summary>
/// 长度值（支持像素、百分比、rem、em、auto）。
///
/// 内部以"分量求和"表示：一个 Length 可以同时持有 px / em / rem / percent 分量
/// （例如 CSS 的 <c>calc(1.5em + 0.5rem + 2px)</c>）。算术运算按分量累加，不会提前折算，
/// 因此 em / percent 的实际像素值会推迟到布局阶段、在已知元素字体大小与容器尺寸时才解析。
///
/// 这样可以正确实现 em 相对“元素自身字体大小”解析的语义——
/// 而不是在样式定义时用 RootFontSize 折算（那会让 1.5em 永远等于 1.5*16）。
/// </summary>
public struct Length
{
    /// <summary>根字体大小，用于解析 rem（以及缺少元素字体上下文时的 em 回退）。</summary>
    public static float RootFontSize { get; set; } = 16f;

    // 各单位分量。最终像素值 =
    //   px + rem*RootFontSize + em*fontSize + number*fontSize + percent/100*containerSize。
    // number 为无单位系数（用于无单位 line-height，按字体大小缩放）。
    private float _px;
    private float _em;
    private float _rem;
    private float _percent;
    private float _number;
    private bool _isAuto;

    public Length(float value, LengthUnit unit = LengthUnit.Px)
    {
        _px = _em = _rem = _percent = _number = 0;
        _isAuto = false;

        switch (unit)
        {
            case LengthUnit.Px: _px = value; break;
            case LengthUnit.Em: _em = value; break;
            case LengthUnit.Rem: _rem = value; break;
            case LengthUnit.Percent: _percent = value; break;
            case LengthUnit.Number: _number = value; break;
            case LengthUnit.Auto: _isAuto = true; break;
        }
    }

    public static Length Px(float value) => new Length(value, LengthUnit.Px);
    public static Length Percent(float value) => new Length(value, LengthUnit.Percent);
    public static Length Rem(float value) => new Length(value, LengthUnit.Rem);
    public static Length Em(float value) => new Length(value, LengthUnit.Em);
    /// <summary>无单位数值（如无单位 line-height）：解析为 系数 × 字体大小。</summary>
    public static Length Number(float value) => new Length(value, LengthUnit.Number);
    public static Length Auto => new Length(0, LengthUnit.Auto);

    /// <summary>是否为 auto（auto 不参与算术，且不与具体长度混合）。</summary>
    public bool IsAuto => _isAuto;

    /// <summary>
    /// 该长度是否仅由单一单位构成（auto，或恰好只有一个非零分量；全零视为 0px 的单一长度）。
    /// </summary>
    private bool IsSingleComponent
    {
        get
        {
            if (_isAuto) return true;
            int nonZero = 0;
            if (_px != 0) nonZero++;
            if (_em != 0) nonZero++;
            if (_rem != 0) nonZero++;
            if (_percent != 0) nonZero++;
            if (_number != 0) nonZero++;
            return nonZero <= 1;
        }
    }

    /// <summary>
    /// 兼容旧 API：返回“主分量”的数值。
    /// 单一单位长度返回该单位的数值；auto 返回 0；
    /// 复合长度（由算术产生）按 px 分量返回（仅作兼容，复合长度应通过 <see cref="ToPixels"/> 消费）。
    /// </summary>
    public float Value
    {
        get
        {
            if (_isAuto) return 0;
            if (_em != 0 && _px == 0 && _rem == 0 && _percent == 0 && _number == 0) return _em;
            if (_rem != 0 && _px == 0 && _em == 0 && _percent == 0 && _number == 0) return _rem;
            if (_percent != 0 && _px == 0 && _em == 0 && _rem == 0 && _number == 0) return _percent;
            if (_number != 0 && _px == 0 && _em == 0 && _rem == 0 && _percent == 0) return _number;
            return _px;
        }
    }

    /// <summary>
    /// 兼容旧 API：返回主单位。复合长度统一报告为 Px（应改用 <see cref="ToPixels"/> 解析）。
    /// </summary>
    public LengthUnit Unit
    {
        get
        {
            if (_isAuto) return LengthUnit.Auto;
            if (_em != 0 && _px == 0 && _rem == 0 && _percent == 0 && _number == 0) return LengthUnit.Em;
            if (_rem != 0 && _px == 0 && _em == 0 && _percent == 0 && _number == 0) return LengthUnit.Rem;
            if (_percent != 0 && _px == 0 && _em == 0 && _rem == 0 && _number == 0) return LengthUnit.Percent;
            if (_number != 0 && _px == 0 && _em == 0 && _rem == 0 && _percent == 0) return LengthUnit.Number;
            return LengthUnit.Px;
        }
    }

    /// <summary>
    /// 计算实际像素值。
    /// </summary>
    /// <param name="containerSize">容器尺寸（用于百分比计算）。</param>
    /// <param name="fontSize">
    /// 当前元素的字体大小（用于 em 计算）。未提供时回退到 RootFontSize（此时 em 等价于 rem）。
    /// </param>
    public float ToPixels(float containerSize, float? fontSize = null)
    {
        if (_isAuto) return 0;

        float emBase = fontSize ?? RootFontSize;
        return _px
             + _rem * RootFontSize
             + _em * emBase
             + _number * emBase
             + _percent / 100f * containerSize;
    }

    public static implicit operator Length(float value) => new Length(value, LengthUnit.Px);

    // ---- 算术运算符 ----
    // 按分量累加 / 缩放，不提前折算任何单位（保留 em / rem / percent 语义到布局阶段解析）。
    // 含 auto 的运算无意义：auto 不与具体长度混合，遇到时直接返回 auto。

    public static Length operator +(Length x, Length y)
    {
        if (x._isAuto || y._isAuto) return Auto;
        return new Length
        {
            _px = x._px + y._px,
            _em = x._em + y._em,
            _rem = x._rem + y._rem,
            _percent = x._percent + y._percent,
            _number = x._number + y._number,
        };
    }

    public static Length operator -(Length x, Length y)
    {
        if (x._isAuto || y._isAuto) return Auto;
        return new Length
        {
            _px = x._px - y._px,
            _em = x._em - y._em,
            _rem = x._rem - y._rem,
            _percent = x._percent - y._percent,
            _number = x._number - y._number,
        };
    }

    public static Length operator -(Length x)
    {
        if (x._isAuto) return Auto;
        return new Length
        {
            _px = -x._px,
            _em = -x._em,
            _rem = -x._rem,
            _percent = -x._percent,
            _number = -x._number,
        };
    }

    // 与标量运算：缩放所有分量。
    public static Length operator *(Length x, float factor)
    {
        if (x._isAuto) return Auto;
        return new Length
        {
            _px = x._px * factor,
            _em = x._em * factor,
            _rem = x._rem * factor,
            _percent = x._percent * factor,
            _number = x._number * factor,
        };
    }

    public static Length operator *(float factor, Length x) => x * factor;

    public static Length operator /(Length x, float divisor)
    {
        if (x._isAuto) return Auto;
        return new Length
        {
            _px = x._px / divisor,
            _em = x._em / divisor,
            _rem = x._rem / divisor,
            _percent = x._percent / divisor,
            _number = x._number / divisor,
        };
    }

    public override string ToString()
    {
        if (_isAuto) return "auto";

        // 单一单位：沿用简洁写法（如 "16px"、"1.5rem"），保证与既有期望一致。
        if (IsSingleComponent)
        {
            return Unit switch
            {
                LengthUnit.Px => $"{_px}px",
                LengthUnit.Em => $"{_em}em",
                LengthUnit.Rem => $"{_rem}rem",
                LengthUnit.Percent => $"{_percent}%",
                LengthUnit.Number => $"{_number}",
                _ => Value.ToString()
            };
        }

        // 复合长度：列出各非零分量，如 "1.5em + 0.5rem + 2px"。
        var parts = new List<string>();
        if (_em != 0) parts.Add($"{_em}em");
        if (_rem != 0) parts.Add($"{_rem}rem");
        if (_number != 0) parts.Add($"{_number}");
        if (_px != 0) parts.Add($"{_px}px");
        if (_percent != 0) parts.Add($"{_percent}%");
        return parts.Count == 0 ? "0px" : string.Join(" + ", parts);
    }
}
