using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Miko.Common;

namespace Miko.Styling;

/// <summary>
/// 自定义样式变量的具体值。用一个联合结构封装 <see cref="Color"/>、<see cref="Length"/>、
/// <c>float</c>、<c>int</c>、任意（int 底层的）枚举以及 <c>string</c>，全程零装箱：
/// 数值型分量共享一段显式布局的内存（<see cref="Blittable"/>），读写经 <see cref="Unsafe"/>
/// 直接位重解释；<c>string</c> 为托管引用，单独存放（不能与非托管字段重叠）。
/// </summary>
public readonly struct VarValue
{
    private enum Kind : byte { None, Color, Length, Float, Int, Enum, String }

    /// <summary>
    /// 数值型分量的联合体：Length/Color/float/int/枚举 都是可 blittable 的值类型，
    /// 重叠在同一偏移上，互斥使用（由外层 <see cref="Kind"/> 判别）。
    /// 不含托管引用，故显式重叠安全。
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    private struct Blittable
    {
        [FieldOffset(0)] public Length Length;
        [FieldOffset(0)] public Color Color;
        [FieldOffset(0)] public float Float;
        [FieldOffset(0)] public int Int;   // 同时承载 int 与所有 int 底层的枚举
    }

    private readonly Kind _kind;
    private readonly Blittable _num;
    private readonly string? _str;      // 托管引用，单独存放
    private readonly Type? _enumType;   // 枚举 CLR 类型，用于精确类型校验（引用比较，不装箱）

    private VarValue(Kind kind, Blittable num = default, string? str = null, Type? enumType = null)
    {
        _kind = kind;
        _num = num;
        _str = str;
        _enumType = enumType;
    }

    public VarValue(Color value) : this(Kind.Color, new Blittable { Color = value }) { }
    public VarValue(Length value) : this(Kind.Length, new Blittable { Length = value }) { }
    public VarValue(float value) : this(Kind.Float, new Blittable { Float = value }) { }
    public VarValue(int value) : this(Kind.Int, new Blittable { Int = value }) { }
    public VarValue(string value) : this(Kind.String, str: value) { }

    /// <summary>
    /// 从枚举创建变量值（零装箱：按 int 位重解释存入联合体，另存枚举类型用于精确校验）。
    /// 仅支持 int 底层的枚举（Miko 中所有样式枚举均为默认 int 底层）。
    /// </summary>
    public static VarValue FromEnum<TEnum>(TEnum value) where TEnum : struct, Enum
        => new(Kind.Enum, new Blittable { Int = Unsafe.As<TEnum, int>(ref value) }, enumType: typeof(TEnum));

    public static implicit operator VarValue(Color value) => new(value);
    public static implicit operator VarValue(Length value) => new(value);
    public static implicit operator VarValue(float value) => new(value);
    public static implicit operator VarValue(int value) => new(value);
    public static implicit operator VarValue(string value) => new(value);

    /// <summary>
    /// 尝试按目标类型 <typeparamref name="T"/> 取出具体值；类型不匹配时返回 <c>false</c>。
    /// 全程零装箱：数值型经 <see cref="Unsafe"/> 位重解释，枚举按 int 还原，字符串直接返回。
    /// <typeparamref name="T"/> 无约束，便于统一被 <c>StyleProperty&lt;T&gt;</c> 的解析路径调用
    /// （引用型 <c>T</c> 除 <c>string</c> 外均不匹配、返回 false）。
    /// </summary>
    public bool TryGet<T>(out T value)
    {
        // 需要读取一个可变副本以取 ref；VarValue 为 readonly，故复制到局部。
        var blittable = _num;

        if (typeof(T) == typeof(Length) && _kind == Kind.Length)
        {
            value = Unsafe.As<Length, T>(ref blittable.Length);
            return true;
        }
        if (typeof(T) == typeof(Color) && _kind == Kind.Color)
        {
            value = Unsafe.As<Color, T>(ref blittable.Color);
            return true;
        }
        if (typeof(T) == typeof(float) && _kind == Kind.Float)
        {
            value = Unsafe.As<float, T>(ref blittable.Float);
            return true;
        }
        if (typeof(T) == typeof(int) && _kind == Kind.Int)
        {
            value = Unsafe.As<int, T>(ref blittable.Int);
            return true;
        }
        // 枚举：按 int 位重解释还原；精确校验枚举类型（引用比较，零装箱）。
        if (_kind == Kind.Enum && typeof(T) == _enumType)
        {
            value = Unsafe.As<int, T>(ref blittable.Int);
            return true;
        }
        // 字符串（如 font-family）：引用型，直接返回（引用转换不装箱）。
        if (typeof(T) == typeof(string) && _kind == Kind.String && _str != null)
        {
            value = (T)(object)_str;
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

    public override string ToString()
    {
        var blittable = _num;
        return _kind switch
        {
            Kind.Color => blittable.Color.ToString(),
            Kind.Length => blittable.Length.ToString(),
            Kind.Float => blittable.Float.ToString(),
            Kind.Int => blittable.Int.ToString(),
            Kind.Enum => _enumType != null ? Enum.GetName(_enumType, blittable.Int) ?? blittable.Int.ToString() : blittable.Int.ToString(),
            Kind.String => _str ?? "",
            _ => "none",
        };
    }
}
