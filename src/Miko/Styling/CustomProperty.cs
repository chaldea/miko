namespace Miko.Styling;

/// <summary>
/// 变量引用，等同于 CSS var(--name, fallback?)
/// </summary>
public readonly record struct VarReference(string Name, object? Fallback = null)
{
    // calc 语境下的算术运算符：让 `-1 * Var("--x")` 这类表达式可直接构建 CalcExpr 树。
    // C# 不会对 VarReference 在运算时隐式转换到 CalcExpr，故在此显式提供运算符。
    public static CalcExpr operator +(VarReference a, CalcExpr b) => (CalcExpr)a + b;
    public static CalcExpr operator +(CalcExpr a, VarReference b) => a + (CalcExpr)b;
    public static CalcExpr operator +(VarReference a, VarReference b) => (CalcExpr)a + (CalcExpr)b;

    public static CalcExpr operator -(VarReference a, CalcExpr b) => (CalcExpr)a - b;
    public static CalcExpr operator -(CalcExpr a, VarReference b) => a - (CalcExpr)b;
    public static CalcExpr operator -(VarReference a, VarReference b) => (CalcExpr)a - (CalcExpr)b;

    public static CalcExpr operator *(VarReference a, CalcExpr b) => (CalcExpr)a * b;
    public static CalcExpr operator *(CalcExpr a, VarReference b) => a * (CalcExpr)b;
    public static CalcExpr operator *(VarReference a, VarReference b) => (CalcExpr)a * (CalcExpr)b;

    public static CalcExpr operator /(VarReference a, CalcExpr b) => (CalcExpr)a / b;
    public static CalcExpr operator /(CalcExpr a, VarReference b) => a / (CalcExpr)b;
    public static CalcExpr operator /(VarReference a, VarReference b) => (CalcExpr)a / (CalcExpr)b;

    public static CalcExpr operator -(VarReference a) => -(CalcExpr)a;
}

/// <summary>
/// 自定义属性作用域，通过链式结构实现变量继承（零装箱）
/// </summary>
public class CustomPropertyScope
{
    private readonly Dictionary<string, StyleValue> _values = new();
    private readonly CustomPropertyScope? _parent;

    public CustomPropertyScope(CustomPropertyScope? parent = null) => _parent = parent;

    public void Set(string name, StyleValue value) => _values[name] = value;

    public StyleValue? Get(string name)
    {
        if (_values.TryGetValue(name, out var v))
            return v;
        return _parent?.Get(name);
    }

    public T? Get<T>(string name) where T : struct
    {
        var sv = Get(name);
        return sv?.Get<T>();
    }

    public CustomPropertyScope CreateChild() => new(this);

    /// <summary>
    /// 在 calc 表达式中引用一个变量，返回可参与算术运算的 <see cref="CalcExpr"/>。
    /// 例如 <c>Calc(s => -1 * s.Var("--bs-border-width"))</c>。
    /// </summary>
    public CalcExpr Var(string name) => CalcExpr.FromVar(new VarReference(name));
}
