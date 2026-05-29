namespace Miko.Styling;

/// <summary>
/// 样式属性的 union 类型，支持具体值或 CSS 变量引用
/// </summary>
public struct StyleProperty<T>
{
    private readonly byte _index; // 0=Value, 1=VarReference
    private readonly T _value;
    private readonly VarReference _var;

    public StyleProperty(T value)
    {
        _index = 0;
        _value = value;
        _var = default;
    }

    public StyleProperty(VarReference var)
    {
        _index = 1;
        _value = default!;
        _var = var;
    }

    public bool IsVar => _index == 1;

    public T Value => _value;

    public VarReference Var => _var;

    public bool TryGetValue(out T value)
    {
        if (_index == 0)
        {
            value = _value;
            return true;
        }
        value = default!;
        return false;
    }

    public static implicit operator StyleProperty<T>(T value) => new(value);
    public static implicit operator StyleProperty<T>(VarReference var) => new(var);

    public override string ToString() => IsVar ? $"var({_var.Name})" : _value?.ToString() ?? "null";
}

/// <summary>
/// 全局入口：Var("--name") 创建变量引用
/// </summary>
public static class StyleVar
{
    public static VarReference Var(string name) => new(name);
}
