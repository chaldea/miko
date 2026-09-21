namespace Miko.Common;

/// <summary>
/// 单分量 <see cref="Length"/> 的单位判别式（打包进 <c>Length._packed</c> 的低 4 bit）。
///
/// <para>比公开的 <see cref="LengthUnit"/> 多出 safe-area 四个方向——它们在旧表示里是四个
/// 独立的系数字段，这里由判别式表达。<c>auto</c> / <c>fit-content</c> 同理：旧表示用两个
/// 独立 bool，那只是历史写法，判别式足以表达。</para>
/// </summary>
internal enum LengthKind : uint
{
    Px = 0,     // default(Length) 必须是 0px，故 Px 取 0
    Percent,
    Rem,
    Em,
    Vw,
    Vh,
    Number,
    Auto,
    FitContent,
    SafeTop,
    SafeRight,
    SafeBottom,
    SafeLeft,
}

/// <summary>
/// 混合分量长度的旁路载荷（calc 风格，如 <c>calc(100% - 2em)</c>），见 ISSUE-142。
///
/// <para>字段含义与 <see cref="Length"/> 旧表示的私有分量字段一一对应：最终像素值 =
/// <c>Px + Rem*RootFontSize + Em*fontSize + Number*fontSize + Percent/100*containerSize
/// + Vw/100*视口宽 + Vh/100*视口高 + Safe* * 对应方向的安全区 inset</c>，其中 vw/vh 与 safe*
/// 由 <see cref="Length.ResolveViewport"/> / <see cref="Length.ResolveSafeArea"/> 在样式计算
/// 阶段折进 <see cref="Px"/> 后清零。</para>
///
/// <para>实测全组件库的混合形状只有 <c>px+pct</c> 与 <c>px+pct+safeTop</c> 两种、最多 3 个分量，
/// 但这里保留全部 11 个分量以维持语义完整——只有 0.66% 的长度槽会分配这个对象。</para>
///
/// <para><b>逻辑上不可变</b>：字段可写只是为了让 <see cref="Length"/> 在构造结果时能就地填写。
/// 一旦被某个 <see cref="Length"/> 持有就绝不再改写（长度是值类型、自由复制，就地改写会波及
/// 所有副本）；需要修改的一方先 <see cref="Clone"/>。</para>
/// </summary>
internal sealed class MixedComponents : IEquatable<MixedComponents>
{
    public float Px, Em, Rem, Percent, Number, Vw, Vh;
    public float SafeTop, SafeRight, SafeBottom, SafeLeft;

    public bool HasSafeArea => SafeTop != 0 || SafeRight != 0 || SafeBottom != 0 || SafeLeft != 0;

    public MixedComponents Clone() => new()
    {
        Px = Px, Em = Em, Rem = Rem, Percent = Percent, Number = Number,
        Vw = Vw, Vh = Vh,
        SafeTop = SafeTop, SafeRight = SafeRight, SafeBottom = SafeBottom, SafeLeft = SafeLeft,
    };

    /// <summary>读取判别式对应的分量。<c>auto</c> / <c>fit-content</c> 不是分量，恒为 0。</summary>
    public float Get(LengthKind kind) => kind switch
    {
        LengthKind.Px => Px,
        LengthKind.Percent => Percent,
        LengthKind.Rem => Rem,
        LengthKind.Em => Em,
        LengthKind.Vw => Vw,
        LengthKind.Vh => Vh,
        LengthKind.Number => Number,
        LengthKind.SafeTop => SafeTop,
        LengthKind.SafeRight => SafeRight,
        LengthKind.SafeBottom => SafeBottom,
        LengthKind.SafeLeft => SafeLeft,
        _ => 0,
    };

    /// <summary>写入判别式对应的分量。<c>auto</c> / <c>fit-content</c> 无分量可写，忽略。</summary>
    public void Set(LengthKind kind, float value)
    {
        switch (kind)
        {
            case LengthKind.Px: Px = value; break;
            case LengthKind.Percent: Percent = value; break;
            case LengthKind.Rem: Rem = value; break;
            case LengthKind.Em: Em = value; break;
            case LengthKind.Vw: Vw = value; break;
            case LengthKind.Vh: Vh = value; break;
            case LengthKind.Number: Number = value; break;
            case LengthKind.SafeTop: SafeTop = value; break;
            case LengthKind.SafeRight: SafeRight = value; break;
            case LengthKind.SafeBottom: SafeBottom = value; break;
            case LengthKind.SafeLeft: SafeLeft = value; break;
        }
    }

    /// <summary>逐分量累加 <paramref name="other"/> 的 <paramref name="sign"/> 倍（减法传 -1）。</summary>
    public void Add(MixedComponents other, float sign)
    {
        Px += other.Px * sign;
        Em += other.Em * sign;
        Rem += other.Rem * sign;
        Percent += other.Percent * sign;
        Number += other.Number * sign;
        Vw += other.Vw * sign;
        Vh += other.Vh * sign;
        SafeTop += other.SafeTop * sign;
        SafeRight += other.SafeRight * sign;
        SafeBottom += other.SafeBottom * sign;
        SafeLeft += other.SafeLeft * sign;
    }

    public void Scale(float factor)
    {
        Px *= factor; Em *= factor; Rem *= factor; Percent *= factor; Number *= factor;
        Vw *= factor; Vh *= factor;
        SafeTop *= factor; SafeRight *= factor; SafeBottom *= factor; SafeLeft *= factor;
    }

    public void Divide(float divisor)
    {
        Px /= divisor; Em /= divisor; Rem /= divisor; Percent /= divisor; Number /= divisor;
        Vw /= divisor; Vh /= divisor;
        SafeTop /= divisor; SafeRight /= divisor; SafeBottom /= divisor; SafeLeft /= divisor;
    }

    /// <summary>
    /// 若非零分量不超过一个，输出它的判别式与数值（全零输出 <c>0px</c>）——调用方据此把长度
    /// 降级回快路径，丢弃本对象。含两个及以上非零分量时返回 <c>false</c>。
    /// </summary>
    public bool TryGetSingle(out LengthKind kind, out float value)
    {
        kind = LengthKind.Px;
        value = 0;
        int nonZero = 0;

        if (Px != 0) { kind = LengthKind.Px; value = Px; nonZero++; }
        if (Em != 0) { kind = LengthKind.Em; value = Em; nonZero++; }
        if (Rem != 0) { kind = LengthKind.Rem; value = Rem; nonZero++; }
        if (Percent != 0) { kind = LengthKind.Percent; value = Percent; nonZero++; }
        if (Number != 0) { kind = LengthKind.Number; value = Number; nonZero++; }
        if (Vw != 0) { kind = LengthKind.Vw; value = Vw; nonZero++; }
        if (Vh != 0) { kind = LengthKind.Vh; value = Vh; nonZero++; }
        if (SafeTop != 0) { kind = LengthKind.SafeTop; value = SafeTop; nonZero++; }
        if (SafeRight != 0) { kind = LengthKind.SafeRight; value = SafeRight; nonZero++; }
        if (SafeBottom != 0) { kind = LengthKind.SafeBottom; value = SafeBottom; nonZero++; }
        if (SafeLeft != 0) { kind = LengthKind.SafeLeft; value = SafeLeft; nonZero++; }

        if (nonZero <= 1) return true;

        kind = LengthKind.Px;
        value = 0;
        return false;
    }

    /// <summary>
    /// 按<b>值</b>相等——绝不能退化成引用相等：两个分量相同的混合长度必须相等，否则所有
    /// 断言混合长度的测试与「值未变」判定都会悄悄失准（见 ISSUE-142 风险表）。
    /// </summary>
    public bool Equals(MixedComponents? other) =>
        other is not null
        && Px.Equals(other.Px) && Em.Equals(other.Em) && Rem.Equals(other.Rem)
        && Percent.Equals(other.Percent) && Number.Equals(other.Number)
        && Vw.Equals(other.Vw) && Vh.Equals(other.Vh)
        && SafeTop.Equals(other.SafeTop) && SafeRight.Equals(other.SafeRight)
        && SafeBottom.Equals(other.SafeBottom) && SafeLeft.Equals(other.SafeLeft);

    public override bool Equals(object? obj) => Equals(obj as MixedComponents);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Px); hash.Add(Em); hash.Add(Rem); hash.Add(Percent); hash.Add(Number);
        hash.Add(Vw); hash.Add(Vh);
        hash.Add(SafeTop); hash.Add(SafeRight); hash.Add(SafeBottom); hash.Add(SafeLeft);
        return hash.ToHashCode();
    }
}
