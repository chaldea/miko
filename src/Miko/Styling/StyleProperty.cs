namespace Miko.Styling;

/// <summary>
/// 样式属性的联合值，三选一：具体值 <typeparamref name="T"/>、变量引用
/// <see cref="Styling.VarReference"/>（如 <c>Color = Var("--button-color")</c>），
/// 或一个 CSS 全局关键词 <see cref="StyleKeyword"/>（如 <c>Color = Initial</c>）。
/// <para>
/// 通过隐式转换，既有写法 <c>Color = Color.Red</c> / <c>Width = Length.Px(100)</c> 保持不变；
/// 变量写法 <c>Color = Var("--x")</c> 经 <see cref="VarReference"/> 隐式转换成立；
/// 关键词写法 <c>Color = Initial</c> 经 <see cref="StyleKeyword"/> 隐式转换成立。
/// </para>
/// </summary>
public readonly struct StyleProperty<T>
{
    /// <summary>联合体判别式：当前 <see cref="StyleProperty{T}"/> 持有的是哪一路。</summary>
    private enum Slot : byte { Value, Var, Keyword }

    private readonly T _value;
    private readonly VarReference _var;
    private readonly StyleKeyword _keyword;
    private readonly Slot _slot;

    public StyleProperty(T value)
    {
        _value = value;
        _var = default;
        _keyword = default;
        _slot = Slot.Value;
    }

    public StyleProperty(VarReference var)
    {
        _value = default!;
        _var = var;
        _keyword = default;
        _slot = Slot.Var;
    }

    public StyleProperty(StyleKeyword keyword)
    {
        _value = default!;
        _var = default;
        _keyword = keyword;
        _slot = Slot.Keyword;
    }

    /// <summary>是否为变量引用（而非具体值或关键词）。</summary>
    public bool IsVar => _slot == Slot.Var;

    /// <summary>是否为 CSS 全局关键词（而非具体值或变量引用）。</summary>
    public bool IsKeyword => _slot == Slot.Keyword;

    /// <summary>变量引用（仅当 <see cref="IsVar"/> 为 true 时有意义）。</summary>
    public VarReference VarRef => _var;

    /// <summary>CSS 全局关键词（仅当 <see cref="IsKeyword"/> 为 true 时有意义）。</summary>
    public StyleKeyword Keyword => _keyword;

    /// <summary>
    /// 尝试取出具体值。当持有变量引用或关键词时返回 <c>false</c>（此时需先经作用域/级联解析）。
    /// </summary>
    public bool TryGetValue(out T value)
    {
        value = _value;
        return _slot == Slot.Value;
    }

    /// <summary>
    /// 具体值。仅在确定为具体值时使用（否则抛出）。主要用于测试断言与已解析场景。
    /// </summary>
    public T Value => _slot != Slot.Value
        ? throw new InvalidOperationException("StyleProperty holds a variable reference or keyword; resolve it against a scope first.")
        : _value;

    public static implicit operator StyleProperty<T>(T value) => new(value);
    public static implicit operator StyleProperty<T>(VarReference var) => new(var);
    public static implicit operator StyleProperty<T>(StyleKeyword keyword) => new(keyword);

    public override string ToString() => _slot switch
    {
        Slot.Var => $"var({_var.Name})",
        Slot.Keyword => _keyword.ToString(),
        _ => _value?.ToString() ?? "",
    };
}

/// <summary>
/// <see cref="StyleProperty{T}"/> 的读取辅助，用于从可空的 <c>StyleProperty&lt;T&gt;?</c>
/// 取回旧式的 <c>T?</c>（变量引用或 null 时视为“未设置”）。方便适配既有消费点。
/// </summary>
public static class StylePropertyExtensions
{
    /// <summary>取出值类型属性的具体值；为变量引用或 null 时返回 <c>null</c>。</summary>
    public static T? ValueOrNull<T>(this StyleProperty<T>? property) where T : struct
        => property is { } p && p.TryGetValue(out var value) ? value : (T?)null;

    /// <summary>取出引用类型属性的具体值；为变量引用或 null 时返回 <c>null</c>。</summary>
    public static T? RefValueOrNull<T>(this StyleProperty<T>? property) where T : class
        => property is { } p && p.TryGetValue(out var value) ? value : null;
}
