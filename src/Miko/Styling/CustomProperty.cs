namespace Miko.Styling;

/// <summary>
/// 变量引用，等同于 CSS var(--name, fallback?)
/// </summary>
public readonly record struct VarReference(string Name, object? Fallback = null);

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
}
