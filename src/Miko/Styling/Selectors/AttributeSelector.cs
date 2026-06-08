using Miko.Core;
using System.Reflection;

namespace Miko.Styling.Selectors;

/// <summary>
/// 属性选择器的匹配操作符
/// </summary>
public enum AttributeMatchOperator
{
    /// <summary>[attr] — 属性存在</summary>
    Exists,
    /// <summary>[attr="value"] — 属性值完全等于</summary>
    Equals,
    /// <summary>[attr~="value"] — 属性值是以空格分隔的词列表，其中一个词等于指定值</summary>
    Includes,
    /// <summary>[attr|="value"] — 属性值等于指定值或以"指定值-"开头（用于语言代码等）</summary>
    DashMatch,
    /// <summary>[attr^="value"] — 属性值以指定值开头</summary>
    Prefix,
    /// <summary>[attr$="value"] — 属性值以指定值结尾</summary>
    Suffix,
    /// <summary>[attr*="value"] — 属性值包含指定值</summary>
    Substring
}

/// <summary>
/// 属性选择器 ([attr], [attr="value"], [attr~="value"], 等)
/// </summary>
public class AttributeSelector : Selector
{
    public string AttributeName { get; set; }
    public AttributeMatchOperator Operator { get; set; }
    public string? Value { get; set; }

    public AttributeSelector(string attributeName, AttributeMatchOperator op = AttributeMatchOperator.Exists, string? value = null)
    {
        AttributeName = attributeName;
        Operator = op;
        Value = value;
    }

    public override bool Matches(Element element)
    {
        // 使用反射获取元素的属性值（Miko 元素将 HTML 属性作为 C# 属性暴露）
        var propInfo = element.GetType().GetProperty(AttributeName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (propInfo == null)
            return false; // 属性不存在

        var attrValue = propInfo.GetValue(element);

        // [attr] — 仅检查属性存在（非 null）
        if (Operator == AttributeMatchOperator.Exists)
            return attrValue != null;

        // 其他操作符需要比较值；null 属性值视为不匹配
        if (attrValue == null || Value == null)
            return false;

        // 将属性值转换为字符串进行比较（InputType 等枚举会转为字符串形式）
        string attrStr = attrValue.ToString() ?? "";
        string valueStr = Value;

        return Operator switch
        {
            AttributeMatchOperator.Equals => attrStr.Equals(valueStr, StringComparison.OrdinalIgnoreCase),
            AttributeMatchOperator.Includes => attrStr.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Any(word => word.Equals(valueStr, StringComparison.OrdinalIgnoreCase)),
            AttributeMatchOperator.DashMatch => attrStr.Equals(valueStr, StringComparison.OrdinalIgnoreCase) ||
                                                attrStr.StartsWith(valueStr + "-", StringComparison.OrdinalIgnoreCase),
            AttributeMatchOperator.Prefix => attrStr.StartsWith(valueStr, StringComparison.OrdinalIgnoreCase),
            AttributeMatchOperator.Suffix => attrStr.EndsWith(valueStr, StringComparison.OrdinalIgnoreCase),
            AttributeMatchOperator.Substring => attrStr.Contains(valueStr, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    public override int Specificity => 10; // 属性选择器特异性为 10（与 class 相同）
}
