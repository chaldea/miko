using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Miko.Common;

namespace Miko.Styling;

/// <summary>
/// CSS 变量值的无装箱存储，支持所有 Style 属性值类型
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct StyleValue
{
    [FieldOffset(0)] private byte _type;
    [FieldOffset(4)] private Length _length;
    [FieldOffset(4)] private Color _color;
    [FieldOffset(4)] private float _float;
    [FieldOffset(4)] private int _int;

    private StyleValue(byte type) { _type = type; }

    public static implicit operator StyleValue(Length v) => new(1) { _length = v };
    public static implicit operator StyleValue(Color v) => new(2) { _color = v };
    public static implicit operator StyleValue(float v) => new(3) { _float = v };
    public static implicit operator StyleValue(int v) => new(4) { _int = v };

    /// <summary>
    /// 尝试获取指定类型的值，类型不匹配时返回 null（零装箱）
    /// </summary>
    public T? Get<T>() where T : struct
    {
        if (typeof(T) == typeof(Length) && _type == 1)
            return Unsafe.As<Length, T>(ref _length);
        if (typeof(T) == typeof(Color) && _type == 2)
            return Unsafe.As<Color, T>(ref _color);
        if (typeof(T) == typeof(float) && _type == 3)
            return Unsafe.As<float, T>(ref _float);
        if (typeof(T) == typeof(int) && _type == 4)
            return Unsafe.As<int, T>(ref _int);

        // 枚举类型统一用 int 存储
        if (typeof(T).IsEnum && _type == 4)
            return Unsafe.As<int, T>(ref _int);

        return null;
    }

    public override string ToString() => _type switch
    {
        1 => _length.ToString(),
        2 => _color.ToString(),
        3 => _float.ToString(),
        4 => _int.ToString(),
        _ => "unset"
    };
}
