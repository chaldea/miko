namespace Miko.Common;

/// <summary>
/// 长度值（支持像素、百分比、rem、em、auto）。
///
/// 语义上以"分量求和"表示：一个 Length 可以同时持有 px / em / rem / percent 分量
/// （例如 CSS 的 <c>calc(1.5em + 0.5rem + 2px)</c>）。算术运算按分量累加，不会提前折算，
/// 因此 em / percent 的实际像素值会推迟到布局阶段、在已知元素字体大小与容器尺寸时才解析。
///
/// 这样可以正确实现 em 相对“元素自身字体大小”解析的语义——
/// 而不是在样式定义时用 RootFontSize 折算（那会让 1.5em 永远等于 1.5*16）。
///
/// <para>
/// 存储布局（ISSUE-142）：常见路径是<b>单分量</b>——实测真实应用的 55,461 个长度槽里混合分量
/// 占 0.00%，全组件库样式表的 3,633 个已设置槽里也只占 0.66%（24 条，形如 px+pct 与
/// px+pct+safeTop）。因此单分量存在 <see cref="_value"/> + <see cref="_packed"/> 的判别式里，
/// 混合分量才逃逸到旁路对象 <see cref="_mix"/>——与 <see cref="Styling.StyleProperty{T}"/>
/// 为罕见的 var/calc 路径所做的处理同一套策略。
/// </para>
/// <para>
/// 这个体积很要紧：<see cref="Styling.Style"/> 有 58 个长度槽，经
/// <c>StyleProperty&lt;Length&gt;?</c> 两级放大后占其体积的 60%，而
/// <see cref="Styling.ComputedStyle"/> 是空闲托管堆的最大占用方（ISSUE-141）。
/// 瘦身后单实例 9,368 B → 6,024 B，101 个元素的冷样式解析 994 KB → 654 KB。
/// </para>
/// </summary>
[System.ComponentModel.TypeConverter(typeof(LengthConverter))]
public struct Length : IEquatable<Length>
{
    /// <summary>根字体大小，用于解析 rem（以及缺少元素字体上下文时的 em 回退）。</summary>
    public static float RootFontSize { get; set; } = 16f;

    // 单分量长度的数值。混合时该字段不参与求值——全部分量都在 _mix 里。
    private float _value;

    // 打包：低 4 bit 为单位判别式（LengthKind，13 个值），第 5 bit 为「是否混合」标志。
    // 打包而非分开存，是为了消除 8 字节对齐产生的填充（实测：分开存 24 B，打包后 16 B）。
    private uint _packed;

    // 罕见路径（实测占 0.66%）：混合分量逃逸到此，为 null 表示单分量。
    // 用引用而非索引，使其随 ComputedStyle 一起被 GC 回收——索引方案需要生命周期簿记，
    // 而 Length 是值类型、没有析构时机，那等同于一张只增不减的表（见 ISSUE-141）。
    private MixedComponents? _mix;

    private const uint KindMask = 0b1111;
    private const uint MixedFlag = 1u << 4;

    // 位域读写统一收口于这两个属性，不在各处手搓位运算（位域是易错点）。
    private LengthKind Kind
    {
        get => (LengthKind)(_packed & KindMask);
        set => _packed = (_packed & ~KindMask) | ((uint)value & KindMask);
    }

    /// <summary>是否走旁路存储。为 true 时 <see cref="_value"/> 无意义，求值全看 <see cref="_mix"/>。</summary>
    private bool IsMixed => (_packed & MixedFlag) != 0;

    /// <summary>用单一分量构造（快路径）。</summary>
    private static Length Single(LengthKind kind, float value)
    {
        var length = default(Length);
        length._value = value;
        length._packed = (uint)kind;
        return length;
    }

    /// <summary>
    /// 用一组分量构造，并在只剩 0 或 1 个非零分量时<b>降级回快路径</b>（丢弃旁路对象）。
    /// 降级很重要：否则 <c>calc(100vw - 240px)</c> 在 <see cref="ResolveViewport"/> 折算后
    /// 仍白占一个旁路对象，而且它与等值的纯 px 长度会不再相等。
    /// </summary>
    /// <param name="components">调用方刚产出的、未被任何长度持有的实例（就地接管，不复制）。</param>
    private static Length FromComponents(MixedComponents components)
    {
        if (components.TryGetSingle(out var kind, out float value))
            return Single(kind, value);

        var length = default(Length);
        length._packed = MixedFlag;
        length._mix = components;
        return length;
    }

    /// <summary>
    /// 把本长度展开成一个可就地修改的分量集（快路径按判别式填入对应分量）。
    /// 总是返回新实例——已存储的旁路对象视为不可变，绝不就地改写。
    /// </summary>
    private MixedComponents ToComponents()
    {
        if (IsMixed) return _mix!.Clone();

        var components = new MixedComponents();
        components.Set(Kind, _value);
        return components;
    }

    /// <summary>把本长度的各分量按 <paramref name="sign"/> 累加进 <paramref name="target"/>（不分配）。</summary>
    private void AccumulateInto(MixedComponents target, float sign)
    {
        if (IsMixed) target.Add(_mix!, sign);
        else target.Set(Kind, target.Get(Kind) + _value * sign);
    }

    public Length(float value, LengthUnit unit = LengthUnit.Px)
    {
        _value = 0;
        _packed = 0;
        _mix = null;

        switch (unit)
        {
            case LengthUnit.Px: Kind = LengthKind.Px; _value = value; break;
            case LengthUnit.Em: Kind = LengthKind.Em; _value = value; break;
            case LengthUnit.Rem: Kind = LengthKind.Rem; _value = value; break;
            case LengthUnit.Percent: Kind = LengthKind.Percent; _value = value; break;
            case LengthUnit.Number: Kind = LengthKind.Number; _value = value; break;
            case LengthUnit.Vw: Kind = LengthKind.Vw; _value = value; break;
            case LengthUnit.Vh: Kind = LengthKind.Vh; _value = value; break;
            // auto / fit-content 丢弃传入的数值（它们不是数量）。
            case LengthUnit.Auto: Kind = LengthKind.Auto; break;
            case LengthUnit.FitContent: Kind = LengthKind.FitContent; break;
        }
    }

    public static Length Px(float value) => Single(LengthKind.Px, value);
    public static Length Percent(float value) => Single(LengthKind.Percent, value);
    public static Length Rem(float value) => Single(LengthKind.Rem, value);
    public static Length Em(float value) => Single(LengthKind.Em, value);
    /// <summary>视窗宽度单位：<c>1vw</c> = 视口宽度的 1%。由 <see cref="ResolveViewport"/> 折算成 px。</summary>
    public static Length Vw(float value) => Single(LengthKind.Vw, value);
    /// <summary>视窗高度单位：<c>1vh</c> = 视口高度的 1%。由 <see cref="ResolveViewport"/> 折算成 px。</summary>
    public static Length Vh(float value) => Single(LengthKind.Vh, value);
    /// <summary>无单位数值（如无单位 line-height）：解析为 系数 × 字体大小。</summary>
    public static Length Number(float value) => Single(LengthKind.Number, value);
    public static Length Auto => Single(LengthKind.Auto, 0);

    /// <summary>
    /// CSS <c>fit-content</c>：收缩到内容尺寸。布局上与 <see cref="Auto"/> 同路径（因此
    /// <see cref="IsAuto"/> 亦为 true），差别只在脱离文档流的定型规则——见 <see cref="IsFitContent"/>。
    /// </summary>
    public static Length FitContent => Single(LengthKind.FitContent, 0);

    // CSS env(safe-area-inset-*)：在已知安全区边距前为符号性长度，由 ResolveSafeArea 折算成 px。
    // 内容元素用其做 padding（避开系统状态栏/导航栏），而全屏浮层不使用，从而仍覆盖整个屏幕。
    // 系数默认为 1，可用 calc 缩放（如 IonModal 的 env(safe-area-inset-top) * breakpoint）。
    public static Length SafeAreaInsetTop => Single(LengthKind.SafeTop, 1);
    public static Length SafeAreaInsetRight => Single(LengthKind.SafeRight, 1);
    public static Length SafeAreaInsetBottom => Single(LengthKind.SafeBottom, 1);
    public static Length SafeAreaInsetLeft => Single(LengthKind.SafeLeft, 1);

    /// <summary>
    /// 是否为 auto（auto 不参与算术，且不与具体长度混合）。
    /// <c>fit-content</c> 也报 true：它与 auto 走同一条「按内容测量」的布局路径，
    /// 各布局算法无需区分（区分点仅在 <see cref="IsFitContent"/> 的注释所述之处）。
    /// </summary>
    public bool IsAuto => !IsMixed && Kind is LengthKind.Auto or LengthKind.FitContent;

    /// <summary>
    /// 是否为 CSS <c>fit-content</c>。仅有两处需要与 auto 区分，都在脱离文档流的定型阶段：
    /// <list type="number">
    /// <item>它不被对边偏移方程接管——<c>left:0; right:0; width:fit-content</c> 仍收缩到内容，
    /// 而非撑满包含块（<c>width:auto</c> 会撑满）。</item>
    /// <item>因此该轴属于「尺寸确定」，两侧偏移都指定时方程过度约束，剩余空间归 auto 外边距
    /// ——这正是 <c>margin:auto</c> 让收缩盒在包含块内居中的机制。</item>
    /// </list>
    /// </summary>
    public bool IsFitContent => !IsMixed && Kind == LengthKind.FitContent;

    /// <summary>
    /// 是否含百分比分量。用于布局阶段判断：当百分比针对"不确定尺寸"的包含块解析时，
    /// 按 CSS 规范应退化为 auto（内容决定），而非解析为 0（见 ISSUE-077 循环依赖）。
    /// </summary>
    public bool HasPercentComponent => IsMixed
        ? _mix!.Percent != 0
        : Kind == LengthKind.Percent && _value != 0;

    /// <summary>该长度是否含未折算的 env(safe-area-inset-*) 分量。</summary>
    public bool HasSafeAreaComponent => IsMixed
        ? _mix!.HasSafeArea
        : (Kind is LengthKind.SafeTop or LengthKind.SafeRight
                or LengthKind.SafeBottom or LengthKind.SafeLeft) && _value != 0;

    /// <summary>该长度是否含未折算的视窗单位（vw/vh）分量。</summary>
    public bool HasViewportComponent => IsMixed
        ? _mix!.Vw != 0 || _mix!.Vh != 0
        : (Kind is LengthKind.Vw or LengthKind.Vh) && _value != 0;

    /// <summary>
    /// 兼容旧 API：返回“主分量”的数值。
    /// 单一单位长度返回该单位的数值；auto 返回 0；
    /// 复合长度（由算术产生）按 px 分量返回（仅作兼容，复合长度应通过 <see cref="ToPixels"/> 消费）。
    /// </summary>
    public float Value
    {
        get
        {
            if (IsMixed) return _mix!.Px;
            return Kind switch
            {
                // auto / fit-content 不是数量；safe-area 系数不在旧判别链里，与复合长度
                // 一样回落 px 分量（此时为 0）。这两条都是刻意保留的历史行为。
                LengthKind.Auto or LengthKind.FitContent => 0,
                LengthKind.SafeTop or LengthKind.SafeRight
                    or LengthKind.SafeBottom or LengthKind.SafeLeft => 0,
                _ => _value,
            };
        }
    }

    /// <summary>
    /// 兼容旧 API：返回主单位。复合长度统一报告为 Px（应改用 <see cref="ToPixels"/> 解析）。
    /// <para>
    /// 注意「全零长度回落 Px」这条：旧实现的判别链靠<b>非零</b>识别主分量，因此
    /// <c>0rem</c> 报告的单位是 Px。<see cref="Animation.AnimationManager"/> 的
    /// 单位兼容判定依赖这条放宽（0 与任意单位可插值），不能收紧。
    /// </para>
    /// </summary>
    public LengthUnit Unit
    {
        get
        {
            if (IsMixed) return LengthUnit.Px;
            return Kind switch
            {
                LengthKind.FitContent => LengthUnit.FitContent,
                LengthKind.Auto => LengthUnit.Auto,
                LengthKind.Em when _value != 0 => LengthUnit.Em,
                LengthKind.Rem when _value != 0 => LengthUnit.Rem,
                LengthKind.Percent when _value != 0 => LengthUnit.Percent,
                LengthKind.Number when _value != 0 => LengthUnit.Number,
                LengthKind.Vw when _value != 0 => LengthUnit.Vw,
                LengthKind.Vh when _value != 0 => LengthUnit.Vh,
                _ => LengthUnit.Px,
            };
        }
    }

    /// <summary>
    /// 折算视窗单位（vw/vh）分量：<c>vw</c> 乘视口宽度、<c>vh</c> 乘视口高度（各按 1% 计），
    /// 并入 px 分量后清零视窗系数。其余分量（px/em/rem/percent/number/safe*）保持不变，
    /// 因此可与 calc 风格的复合长度共存（如 <c>calc(100vw - 240px)</c>）。
    /// 在样式计算阶段（已知视口尺寸时）调用一次即可，之后该长度的 ToPixels 行为与普通长度完全一致。
    /// </summary>
    /// <param name="viewportWidth">视口宽度（逻辑像素），用于折算 vw。</param>
    /// <param name="viewportHeight">视口高度（逻辑像素），用于折算 vh。</param>
    public Length ResolveViewport(float viewportWidth, float viewportHeight)
    {
        if (IsAuto || !HasViewportComponent) return this;

        // 单分量 vw/vh 直接降级成 px，不必经旁路对象。
        if (!IsMixed)
            return Px(_value / 100f * (Kind == LengthKind.Vw ? viewportWidth : viewportHeight));

        var components = _mix!.Clone();
        components.Px += components.Vw / 100f * viewportWidth + components.Vh / 100f * viewportHeight;
        // 视窗分量已折算进 Px，清零避免重复折算。
        components.Vw = components.Vh = 0;
        return FromComponents(components);
    }

    /// <summary>
    /// 折算 env(safe-area-inset-*) 分量：将各方向系数乘以对应的安全区边距并并入 px 分量，
    /// 然后清零安全区系数。其余分量（px/em/rem/percent/number）保持不变，因此可与
    /// calc 风格的复合长度共存（如 <c>env(safe-area-inset-bottom) + 8px</c>）。
    /// 在样式计算阶段调用一次即可，之后该长度的 ToPixels 行为与普通长度完全一致。
    /// </summary>
    public Length ResolveSafeArea(SafeAreaInsets insets)
    {
        if (IsAuto || !HasSafeAreaComponent) return this;

        if (!IsMixed)
        {
            float inset = Kind switch
            {
                LengthKind.SafeTop => insets.Top,
                LengthKind.SafeRight => insets.Right,
                LengthKind.SafeBottom => insets.Bottom,
                _ => insets.Left,
            };
            return Px(_value * inset);
        }

        var components = _mix!.Clone();
        components.Px += components.SafeTop * insets.Top
                       + components.SafeRight * insets.Right
                       + components.SafeBottom * insets.Bottom
                       + components.SafeLeft * insets.Left;
        // 安全区分量已折算进 Px，清零避免重复折算。
        components.SafeTop = components.SafeRight = components.SafeBottom = components.SafeLeft = 0;
        return FromComponents(components);
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
        if (IsAuto) return 0;

        float emBase = fontSize ?? RootFontSize;

        // 未折算的 vw/vh/safe* 分量在此按 0 计（缺少视口/安全区上下文）；正常路径下应已由
        // ResolveViewport / ResolveSafeArea 在样式计算阶段折算进 px 分量。
        if (!IsMixed)
            return Kind switch
            {
                LengthKind.Px => _value,
                LengthKind.Rem => _value * RootFontSize,
                LengthKind.Em or LengthKind.Number => _value * emBase,
                LengthKind.Percent => _value / 100f * containerSize,
                _ => 0,
            };

        var mix = _mix!;
        return mix.Px
             + mix.Rem * RootFontSize
             + mix.Em * emBase
             + mix.Number * emBase
             + mix.Percent / 100f * containerSize;
    }

    public static implicit operator Length(float value) => Px(value);

    // ---- 算术运算符 ----
    // 按分量累加 / 缩放，不提前折算任何单位（保留 em / rem / percent 语义到布局阶段解析）。
    // 含 auto 的运算无意义：auto 不与具体长度混合，遇到时直接返回 auto。
    //
    // 分流：两侧都是同一单位的单分量长度时留在快路径累加 _value；否则升级为混合
    // （分配一个 MixedComponents），并在分量抵消到只剩一个时由 FromComponents 降级回来。

    public static Length operator +(Length x, Length y)
    {
        if (x.IsAuto || y.IsAuto) return Auto;
        if (!x.IsMixed && !y.IsMixed && x.Kind == y.Kind)
            return Single(x.Kind, x._value + y._value);

        var components = x.ToComponents();
        y.AccumulateInto(components, 1f);
        return FromComponents(components);
    }

    public static Length operator -(Length x, Length y)
    {
        if (x.IsAuto || y.IsAuto) return Auto;
        if (!x.IsMixed && !y.IsMixed && x.Kind == y.Kind)
            return Single(x.Kind, x._value - y._value);

        var components = x.ToComponents();
        y.AccumulateInto(components, -1f);
        return FromComponents(components);
    }

    public static Length operator -(Length x) => x * -1f;

    // 与标量运算：缩放所有分量。
    public static Length operator *(Length x, float factor)
    {
        if (x.IsAuto) return Auto;
        if (!x.IsMixed) return Single(x.Kind, x._value * factor);

        var components = x._mix!.Clone();
        components.Scale(factor);
        return FromComponents(components);
    }

    public static Length operator *(float factor, Length x) => x * factor;

    public static Length operator /(Length x, float divisor)
    {
        if (x.IsAuto) return Auto;
        if (!x.IsMixed) return Single(x.Kind, x._value / divisor);

        var components = x._mix!.Clone();
        // 逐分量除，而非乘以 1/divisor：后者会引入一次额外的舍入。
        components.Divide(divisor);
        return FromComponents(components);
    }

    // ---- 相等 ----
    // 必须显式实现：旧表示是纯值类型，用的是 ValueType 的默认逐字段比较，而大量测试与
    // 级联/插值的「值未变」判定依赖它（`style.Width.ShouldBe(Length.Px(1))`）。改用旁路
    // 引用存储后，默认比较会退化成引用比较，两个分量相同的混合长度将不再相等。

    public bool Equals(Length other)
    {
        if (IsMixed) return other.IsMixed && _mix!.Equals(other._mix!);
        if (other.IsMixed) return false;

        // auto / fit-content 不携带数值：只比判别式，避免 auto 与 fit-content 因数值相同而混同。
        if (Kind is LengthKind.Auto or LengthKind.FitContent || other.Kind is LengthKind.Auto or LengthKind.FitContent)
            return Kind == other.Kind;

        return Kind == other.Kind && _value.Equals(other._value);
    }

    public override bool Equals(object? obj) => obj is Length other && Equals(other);

    public override int GetHashCode()
    {
        if (IsMixed) return _mix!.GetHashCode();
        return Kind is LengthKind.Auto or LengthKind.FitContent
            ? HashCode.Combine(Kind)
            : HashCode.Combine(Kind, _value);
    }

    public static bool operator ==(Length left, Length right) => left.Equals(right);
    public static bool operator !=(Length left, Length right) => !left.Equals(right);

    public override string ToString()
    {
        if (!IsMixed)
        {
            // 单一单位：沿用简洁写法（如 "16px"、"1.5rem"），保证与既有期望一致。
            return Kind switch
            {
                LengthKind.FitContent => "fit-content",
                LengthKind.Auto => "auto",
                LengthKind.Em => $"{_value}em",
                LengthKind.Rem => $"{_value}rem",
                LengthKind.Percent => $"{_value}%",
                LengthKind.Number => $"{_value}",
                LengthKind.Vw => $"{_value}vw",
                LengthKind.Vh => $"{_value}vh",
                LengthKind.SafeTop => SafeAreaString("top", _value),
                LengthKind.SafeRight => SafeAreaString("right", _value),
                LengthKind.SafeBottom => SafeAreaString("bottom", _value),
                LengthKind.SafeLeft => SafeAreaString("left", _value),
                _ => $"{_value}px",
            };
        }

        // 复合长度：列出各非零分量，如 "1.5em + 0.5rem + 2px"。
        var mix = _mix!;
        var parts = new List<string>();
        if (mix.Em != 0) parts.Add($"{mix.Em}em");
        if (mix.Rem != 0) parts.Add($"{mix.Rem}rem");
        if (mix.Number != 0) parts.Add($"{mix.Number}");
        if (mix.Px != 0) parts.Add($"{mix.Px}px");
        if (mix.Percent != 0) parts.Add($"{mix.Percent}%");
        if (mix.Vw != 0) parts.Add($"{mix.Vw}vw");
        if (mix.Vh != 0) parts.Add($"{mix.Vh}vh");
        if (mix.SafeTop != 0) parts.Add(SafeAreaString("top", mix.SafeTop));
        if (mix.SafeRight != 0) parts.Add(SafeAreaString("right", mix.SafeRight));
        if (mix.SafeBottom != 0) parts.Add(SafeAreaString("bottom", mix.SafeBottom));
        if (mix.SafeLeft != 0) parts.Add(SafeAreaString("left", mix.SafeLeft));
        return parts.Count == 0 ? "0px" : string.Join(" + ", parts);
    }

    private static string SafeAreaString(string side, float coeff)
    {
        string env = $"env(safe-area-inset-{side})";
        return coeff == 1f ? env : $"{coeff} * {env}";
    }
}
