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
    //   px + rem*RootFontSize + em*fontSize + number*fontSize + percent/100*containerSize
    //   + safe* * 对应方向的安全区 inset（在样式计算阶段由 ResolveSafeArea 折算）。
    // number 为无单位系数（用于无单位 line-height，按字体大小缩放）。
    private float _px;
    private float _em;
    private float _rem;
    private float _percent;
    private float _number;
    // CSS env(safe-area-inset-*) 系数（通常为 1，可用 calc 缩放）。在已知安全区边距前不参与
    // ToPixels；由 ResolveSafeArea 在样式计算阶段折算进 _px 后清零，使其后的布局透明无感。
    private float _safeTop;
    private float _safeRight;
    private float _safeBottom;
    private float _safeLeft;
    private bool _isAuto;

    public Length(float value, LengthUnit unit = LengthUnit.Px)
    {
        _px = _em = _rem = _percent = _number = 0;
        _safeTop = _safeRight = _safeBottom = _safeLeft = 0;
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

    // CSS env(safe-area-inset-*)：在已知安全区边距前为符号性长度，由 ResolveSafeArea 折算成 px。
    // 内容元素用其做 padding（避开系统状态栏/导航栏），而全屏浮层不使用，从而仍覆盖整个屏幕。
    public static Length SafeAreaInsetTop => new Length { _safeTop = 1 };
    public static Length SafeAreaInsetRight => new Length { _safeRight = 1 };
    public static Length SafeAreaInsetBottom => new Length { _safeBottom = 1 };
    public static Length SafeAreaInsetLeft => new Length { _safeLeft = 1 };

    /// <summary>是否为 auto（auto 不参与算术，且不与具体长度混合）。</summary>
    public bool IsAuto => _isAuto;

    /// <summary>
    /// 是否含百分比分量。用于布局阶段判断：当百分比针对"不确定尺寸"的包含块解析时，
    /// 按 CSS 规范应退化为 auto（内容决定），而非解析为 0（见 ISSUE-077 循环依赖）。
    /// </summary>
    public bool HasPercentComponent => !_isAuto && _percent != 0;

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
            if (_safeTop != 0) nonZero++;
            if (_safeRight != 0) nonZero++;
            if (_safeBottom != 0) nonZero++;
            if (_safeLeft != 0) nonZero++;
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

    /// <summary>该长度是否含未折算的 env(safe-area-inset-*) 分量。</summary>
    public bool HasSafeAreaComponent =>
        _safeTop != 0 || _safeRight != 0 || _safeBottom != 0 || _safeLeft != 0;

    /// <summary>
    /// 折算 env(safe-area-inset-*) 分量：将各方向系数乘以对应的安全区边距并并入 px 分量，
    /// 然后清零安全区系数。其余分量（px/em/rem/percent/number）保持不变，因此可与
    /// calc 风格的复合长度共存（如 <c>env(safe-area-inset-bottom) + 8px</c>）。
    /// 在样式计算阶段调用一次即可，之后该长度的 ToPixels 行为与普通长度完全一致。
    /// </summary>
    public Length ResolveSafeArea(SafeAreaInsets insets)
    {
        if (_isAuto || !HasSafeAreaComponent) return this;

        return new Length
        {
            _px = _px
                + _safeTop * insets.Top
                + _safeRight * insets.Right
                + _safeBottom * insets.Bottom
                + _safeLeft * insets.Left,
            _em = _em,
            _rem = _rem,
            _percent = _percent,
            _number = _number,
            // 安全区分量已折算进 _px，清零避免重复折算。
        };
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
        // 未折算的 safe* 分量在此按 0 计（缺少安全区上下文）；正常路径下应已由
        // ResolveSafeArea 在样式计算阶段折算进 _px。
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
            _safeTop = x._safeTop + y._safeTop,
            _safeRight = x._safeRight + y._safeRight,
            _safeBottom = x._safeBottom + y._safeBottom,
            _safeLeft = x._safeLeft + y._safeLeft,
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
            _safeTop = x._safeTop - y._safeTop,
            _safeRight = x._safeRight - y._safeRight,
            _safeBottom = x._safeBottom - y._safeBottom,
            _safeLeft = x._safeLeft - y._safeLeft,
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
            _safeTop = -x._safeTop,
            _safeRight = -x._safeRight,
            _safeBottom = -x._safeBottom,
            _safeLeft = -x._safeLeft,
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
            _safeTop = x._safeTop * factor,
            _safeRight = x._safeRight * factor,
            _safeBottom = x._safeBottom * factor,
            _safeLeft = x._safeLeft * factor,
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
            _safeTop = x._safeTop / divisor,
            _safeRight = x._safeRight / divisor,
            _safeBottom = x._safeBottom / divisor,
            _safeLeft = x._safeLeft / divisor,
        };
    }

    public override string ToString()
    {
        if (_isAuto) return "auto";

        // 单一单位：沿用简洁写法（如 "16px"、"1.5rem"），保证与既有期望一致。
        if (IsSingleComponent)
        {
            if (_safeTop != 0) return SafeAreaString("top", _safeTop);
            if (_safeRight != 0) return SafeAreaString("right", _safeRight);
            if (_safeBottom != 0) return SafeAreaString("bottom", _safeBottom);
            if (_safeLeft != 0) return SafeAreaString("left", _safeLeft);

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
        if (_safeTop != 0) parts.Add(SafeAreaString("top", _safeTop));
        if (_safeRight != 0) parts.Add(SafeAreaString("right", _safeRight));
        if (_safeBottom != 0) parts.Add(SafeAreaString("bottom", _safeBottom));
        if (_safeLeft != 0) parts.Add(SafeAreaString("left", _safeLeft));
        return parts.Count == 0 ? "0px" : string.Join(" + ", parts);
    }

    private static string SafeAreaString(string side, float coeff)
    {
        string env = $"env(safe-area-inset-{side})";
        return coeff == 1f ? env : $"{coeff} * {env}";
    }
}
