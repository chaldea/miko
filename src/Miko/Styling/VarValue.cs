using Miko.Common;

namespace Miko.Styling;

/// <summary>
/// 自定义样式变量的具体值。用一个带判别字段的联合结构封装 <see cref="Color"/>、
/// <see cref="Length"/>、<c>float</c>、<c>int</c>、<c>string</c> 以及任意枚举，
/// 避免用 <c>object</c> 造成的装箱（仅枚举取值路径会有一次不可避免的装箱）。
/// </summary>
public readonly struct VarValue
{
    private enum Kind : byte { None, Color, Length, Float, Int, String, Enum }

    private readonly Kind _kind;
    private readonly Color _color;
    private readonly Length _length;
    private readonly double _num;      // float 或 int 共用一个槽位，避免额外字段
    private readonly long _enumRaw;    // 枚举底层整型值
    private readonly Type? _enumType;  // 枚举 CLR 类型（用于类型安全的还原）
    private readonly string? _str;

    private VarValue(Kind kind, Color color = default, Length length = default,
                     double num = 0, long enumRaw = 0, Type? enumType = null, string? str = null)
    {
        _kind = kind;
        _color = color;
        _length = length;
        _num = num;
        _enumRaw = enumRaw;
        _enumType = enumType;
        _str = str;
    }

    public VarValue(Color value) : this(Kind.Color, color: value) { }
    public VarValue(Length value) : this(Kind.Length, length: value) { }
    public VarValue(float value) : this(Kind.Float, num: value) { }
    public VarValue(int value) : this(Kind.Int, num: value) { }
    public VarValue(string value) : this(Kind.String, str: value) { }

    /// <summary>从枚举创建变量值（底层以 long 保存，还原时按目标类型解包）。</summary>
    public static VarValue FromEnum<TEnum>(TEnum value) where TEnum : struct, Enum
        => new(Kind.Enum, enumRaw: Convert.ToInt64(value), enumType: typeof(TEnum));

    public static implicit operator VarValue(Color value) => new(value);
    public static implicit operator VarValue(Length value) => new(value);
    public static implicit operator VarValue(float value) => new(value);
    public static implicit operator VarValue(int value) => new(value);
    public static implicit operator VarValue(string value) => new(value);

    /// <summary>
    /// 尝试按目标类型 <typeparamref name="T"/> 取出具体值。类型不匹配时返回 <c>false</c>。
    /// 枚举路径经 <see cref="Enum.ToObject(Type, long)"/> 有一次装箱。
    /// </summary>
    public bool TryGet<T>(out T value)
    {
        // 值类型主分支：按 typeof(T) 分派到对应槽位。
        if (typeof(T) == typeof(Color) && _kind == Kind.Color)
        {
            value = (T)(object)_color;
            return true;
        }
        if (typeof(T) == typeof(Length) && _kind == Kind.Length)
        {
            value = (T)(object)_length;
            return true;
        }
        if (typeof(T) == typeof(float) && _kind == Kind.Float)
        {
            value = (T)(object)(float)_num;
            return true;
        }
        if (typeof(T) == typeof(int) && _kind == Kind.Int)
        {
            value = (T)(object)(int)_num;
            return true;
        }
        if (typeof(T) == typeof(string) && _kind == Kind.String && _str != null)
        {
            value = (T)(object)_str;
            return true;
        }
        // 枚举：目标类型须与保存时的枚举类型一致。
        if (_kind == Kind.Enum && typeof(T) == _enumType)
        {
            value = (T)Enum.ToObject(_enumType!, _enumRaw);
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>按目标类型取出具体值；类型不匹配时抛出 <see cref="InvalidCastException"/>。</summary>
    public T As<T>()
        => TryGet<T>(out var value)
            ? value
            : throw new InvalidCastException(
                $"VarValue (kind={_kind}) cannot be read as {typeof(T).Name}.");

    public override string ToString() => _kind switch
    {
        Kind.Color => _color.ToString(),
        Kind.Length => _length.ToString(),
        Kind.Float => ((float)_num).ToString(),
        Kind.Int => ((int)_num).ToString(),
        Kind.String => _str ?? "",
        Kind.Enum => Enum.ToObject(_enumType!, _enumRaw).ToString() ?? "",
        _ => "none",
    };
}
