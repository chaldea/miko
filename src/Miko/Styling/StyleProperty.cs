namespace Miko.Styling;

/// <summary>
/// 样式属性的 union 类型，支持具体值、CSS 变量引用或 calc 表达式
/// </summary>
public struct StyleProperty<T>
{
    private readonly byte _index; // 0=Value, 1=VarReference, 2=Calc
    private readonly T _value;
    private readonly VarReference _var;
    private readonly Func<CustomPropertyScope, CalcExpr>? _calc;

    public StyleProperty(T value)
    {
        _index = 0;
        _value = value;
        _var = default;
        _calc = null;
    }

    public StyleProperty(VarReference var)
    {
        _index = 1;
        _value = default!;
        _var = var;
        _calc = null;
    }

    public StyleProperty(Func<CustomPropertyScope, CalcExpr> calc)
    {
        _index = 2;
        _value = default!;
        _var = default;
        _calc = calc;
    }

    public bool IsVar => _index == 1;

    public bool IsCalc => _index == 2;

    public T Value => _value;

    public VarReference Var => _var;

    public Func<CustomPropertyScope, CalcExpr> Calc => _calc!;

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
    public static implicit operator StyleProperty<T>(Func<CustomPropertyScope, CalcExpr> calc) => new(calc);

    public override string ToString() => _index switch
    {
        1 => $"var({_var.Name})",
        2 => "calc(…)",
        _ => _value?.ToString() ?? "null"
    };
}

/// <summary>
/// 全局入口：Var("--name") 创建变量引用，Calc(...) 包装 calc 表达式
/// </summary>
public static class StyleVar
{
    public static VarReference Var(string name) => new(name);

    /// <summary>
    /// 显式包装一个 calc 表达式，用于隐式推断不生效的场景（C# 不会对裸 lambda 应用用户隐式转换）。
    /// </summary>
    /// <example>
    /// <code>MarginLeft = Calc(s => -1 * Var("--bs-border-width"))</code>
    /// </example>
    public static Func<CustomPropertyScope, CalcExpr> Calc(Func<CustomPropertyScope, CalcExpr> expr) => expr;
}
