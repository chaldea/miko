using System.Collections.Generic;

namespace Miko.SourceGenerators.Models;

internal sealed class PropertyInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsValueType { get; set; }

    /// <summary>
    /// 后备属性内层解析类型 <c>T</c>（<c>StyleProperty&lt;T&gt;?</c> 的 <c>T</c>，或集合属性本身）是否为引用类型。
    /// 引用类型的父计算值可空，读取时需补 <c>?? default!</c> 以匹配非空的解析形参。
    /// </summary>
    public bool InnerIsReferenceType { get; set; }

    /// <summary>
    /// ComputedStyle 是否以非空的 <c>new</c> 属性遮蔽此属性（即该属性有已解析的计算值）。
    /// 仅这些属性可从父元素读取计算值以支持 inherit/unset 关键词；未遮蔽的属性无计算值可继承。
    /// </summary>
    public bool ShadowedByComputedStyle { get; set; }

    /// <summary>
    /// ComputedStyle 上的计算值本身是否可空（如 <c>Color? CaretColor</c>，用 null 表达
    /// CSS 初始值 <c>auto</c>）。此类父值读取时需补 <c>?? default!</c>。
    /// </summary>
    public bool ComputedValueIsNullable { get; set; }
}

internal sealed class StyleInfo
{
    public List<PropertyInfo> Properties { get; set; } = new();
}
