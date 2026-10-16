using System.Globalization;

namespace Miko.Common;

/// <summary>
/// CSS <c>aspect-ratio</c> 的值（ISSUE-150）：<c>auto | &lt;ratio&gt; | auto &amp;&amp; &lt;ratio&gt;</c>。
/// 为盒子设定首选纵横比（宽 / 高），用于在 width / height 至少一边为 auto 时推导另一边。
/// <code>
/// AspectRatio.Auto               // auto：仅替换元素（img / video）按自然纵横比
/// new AspectRatio(16, 9)         // 16 / 9：尺寸换算作用于 box-sizing 指定的盒
/// new AspectRatio(0.5f)          // 0.5（即 0.5 / 1）
/// AspectRatio.AutoOr(16, 9)      // auto 16 / 9：替换元素有自然纵横比时用自然值，否则 16 / 9；
///                                //   尺寸换算恒作用于内容盒
/// </code>
/// <para>任一分量为 0 或无穷大时比值「退化」，行为同 <c>auto</c>（CSS Box Sizing 4 §4.1）。
/// 负数与 NaN 在 CSS 中是非法值，构造时直接拒绝。</para>
/// <para><c>default(AspectRatio)</c> 即 <c>auto</c>（CSS 初始值）。</para>
/// </summary>
public readonly struct AspectRatio : IEquatable<AspectRatio>
{
    private readonly float _width;
    private readonly float _height;
    private readonly bool _autoKeyword;

    /// <summary>创建 <c>width / height</c> 形式的比值（<c>aspect-ratio: 16 / 9</c>）。</summary>
    public AspectRatio(float width, float height)
        : this(width, height, autoKeyword: false)
    {
    }

    /// <summary>创建单数值形式的比值（<c>aspect-ratio: 0.5</c>，即 <c>0.5 / 1</c>）。</summary>
    public AspectRatio(float ratio)
        : this(ratio, 1f, autoKeyword: false)
    {
    }

    private AspectRatio(float width, float height, bool autoKeyword)
    {
        if (float.IsNaN(width) || width < 0)
            throw new ArgumentOutOfRangeException(nameof(width), width, "aspect-ratio components must be non-negative numbers.");
        if (float.IsNaN(height) || height < 0)
            throw new ArgumentOutOfRangeException(nameof(height), height, "aspect-ratio components must be non-negative numbers.");

        _width = width;
        _height = height;
        _autoKeyword = autoKeyword;
    }

    /// <summary><c>aspect-ratio: auto</c>：非替换元素没有首选纵横比，替换元素用其自然纵横比。</summary>
    public static AspectRatio Auto => default;

    /// <summary>
    /// <c>aspect-ratio: auto width / height</c>：替换元素有自然纵横比时用自然值，否则用给定比值；
    /// 尺寸换算恒作用于内容盒（与 box-sizing 无关）。
    /// </summary>
    public static AspectRatio AutoOr(float width, float height) => new(width, height, autoKeyword: true);

    /// <summary>比值的宽度分量（<c>auto</c> 时为 0）。</summary>
    public float Width => _width;

    /// <summary>比值的高度分量（<c>auto</c> 时为 0）。</summary>
    public float Height => _height;

    /// <summary>
    /// 是否声明了一个可用的（非退化）比值。为 false 时行为同 <c>auto</c>。
    /// </summary>
    public bool HasRatio => _width > 0 && _height > 0 && float.IsFinite(_width) && float.IsFinite(_height);

    /// <summary>是否含 <c>auto</c> 关键词（<c>auto</c> 本身，或 <c>auto &amp;&amp; &lt;ratio&gt;</c>）。</summary>
    public bool HasAutoKeyword => _autoKeyword || !HasRatio;

    /// <summary>比值（宽 / 高）。仅当 <see cref="HasRatio"/> 时有意义，否则为 0。</summary>
    public float Value => HasRatio ? _width / _height : 0f;

    public bool Equals(AspectRatio other)
        => _width.Equals(other._width) && _height.Equals(other._height) && _autoKeyword == other._autoKeyword;

    public override bool Equals(object? obj) => obj is AspectRatio other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(_width, _height, _autoKeyword);

    public static bool operator ==(AspectRatio left, AspectRatio right) => left.Equals(right);

    public static bool operator !=(AspectRatio left, AspectRatio right) => !left.Equals(right);

    /// <summary>CSS 书写形式：<c>auto</c>、<c>16 / 9</c> 或 <c>auto 16 / 9</c>。</summary>
    public override string ToString()
    {
        if (_width == 0 && _height == 0 && !_autoKeyword)
            return "auto";

        string ratio = string.Create(CultureInfo.InvariantCulture, $"{_width:0.###} / {_height:0.###}");
        return _autoKeyword ? "auto " + ratio : ratio;
    }
}
