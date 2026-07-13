namespace Miko.Styling;

/// <summary>
/// 样式属性的联合值：要么是一个具体值 <typeparamref name="T"/>，要么是一个变量引用
/// <see cref="Styling.VarReference"/>（如 <c>Color = Var("--button-color")</c>）。
/// <para>
/// 通过隐式转换，既有写法 <c>Color = Color.Red</c> / <c>Width = Length.Px(100)</c> 保持不变；
/// 变量写法 <c>Color = Var("--x")</c> 经 <see cref="VarReference"/> 隐式转换成立。
/// </para>
/// </summary>
public readonly struct StyleProperty<T>
{
    private readonly T _value;
    private readonly VarReference _var;
    private readonly bool _isVar;

    public StyleProperty(T value)
    {
        _value = value;
        _var = default;
        _isVar = false;
    }

    public StyleProperty(VarReference var)
    {
        _value = default!;
        _var = var;
        _isVar = true;
    }

    /// <summary>是否为变量引用（而非具体值）。</summary>
    public bool IsVar => _isVar;

    /// <summary>变量引用（仅当 <see cref="IsVar"/> 为 true 时有意义）。</summary>
    public VarReference VarRef => _var;

    /// <summary>
    /// 尝试取出具体值。当持有变量引用时返回 <c>false</c>（此时需先经作用域解析）。
    /// </summary>
    public bool TryGetValue(out T value)
    {
        value = _value;
        return !_isVar;
    }

    /// <summary>
    /// 具体值。仅在确定不是变量引用时使用（否则抛出）。主要用于测试断言与已解析场景。
    /// </summary>
    public T Value => _isVar
        ? throw new InvalidOperationException("StyleProperty holds a variable reference; resolve it against a scope first.")
        : _value;

    public static implicit operator StyleProperty<T>(T value) => new(value);
    public static implicit operator StyleProperty<T>(VarReference var) => new(var);

    public override string ToString() => _isVar ? $"var({_var.Name})" : _value?.ToString() ?? "";
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
