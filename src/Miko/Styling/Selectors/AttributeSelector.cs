using Miko.Core;

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
        // Miko 元素把 HTML 属性作为 C# 属性暴露；查表由源生成器产出，不用反射
        // （反射版在 Native AOT 下被裁剪后恒失配，见 Element.TryGetAttributeValue，ISSUE-140）。
        if (!element.TryGetAttributeValue(AttributeName, out var attrValue))
            return false; // 属性不存在

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

    // 生成的属性访问表排除了 Style（见 ElementAttributeAccessorGenerator），能读到文字的只有
    // 文本节点的 Text 与自定义元素可能暴露的 TextContent；其余属性（class/id/value/…）
    // 都按样式变更记账或本就不是文字（ISSUE-146）。
    public override bool MayReadContentOrInlineStyle =>
        AttributeName.Equals("text", StringComparison.OrdinalIgnoreCase)
        || AttributeName.Equals("textcontent", StringComparison.OrdinalIgnoreCase)
        || AttributeName.Equals("style", StringComparison.OrdinalIgnoreCase);
}
