using Miko.Core;

namespace Miko.Styling.Selectors;

/// <summary>
/// CSS 选择器字符串解析器
/// </summary>
public static class CssSelectorParser
{
    public static Selector Parse(string input)
    {
        input = input.Trim();

        // 处理分组选择器 (逗号分隔)
        var groups = SplitGroups(input);
        if (groups.Count > 1)
        {
            var selectors = groups.Select(g => ParseSingle(g.Trim())).ToArray();
            return new GroupSelector(selectors);
        }

        return ParseSingle(input);
    }

    private static Selector ParseSingle(string input)
    {
        // 按组合器拆分: >, +, ~, 空格(后代)
        var segments = SplitByCombinators(input);

        if (segments.Count == 1)
            return ParseCompound(segments[0].selector);

        // 从左到右构建组合器链
        var result = ParseCompound(segments[0].selector);
        for (int i = 1; i < segments.Count; i++)
        {
            var right = ParseCompound(segments[i].selector);
            result = segments[i].combinator switch
            {
                '>' => new ChildSelector(result, right),
                '+' => new AdjacentSiblingSelector(result, right),
                '~' => new GeneralSiblingSelector(result, right),
                _ => new DescendantSelector(result, right), // 空格
            };
        }

        return result;
    }

    private static Selector ParseCompound(string input)
    {
        var parts = new List<Selector>();
        var i = 0;

        while (i < input.Length)
        {
            if (input[i] == '#')
            {
                i++;
                var name = ReadIdentifier(input, ref i);
                parts.Add(new IdSelector(name));
            }
            else if (input[i] == '.')
            {
                i++;
                var name = ReadIdentifier(input, ref i);
                parts.Add(new ClassSelector(name));
            }
            else if (input[i] == ':')
            {
                i++;
                // 检查是否是伪元素 (::)
                bool isPseudoElement = false;
                if (i < input.Length && input[i] == ':')
                {
                    isPseudoElement = true;
                    i++;
                }

                var pseudo = ReadIdentifier(input, ref i);

                if (isPseudoElement)
                {
                    parts.Add(ParsePseudoElement(pseudo));
                }
                else if (pseudo == "not" && i < input.Length && input[i] == '(')
                {
                    i++; // skip (
                    var inner = ReadUntilClose(input, ref i);
                    parts.Add(new NotSelector(ParseCompound(inner)));
                }
                else
                {
                    parts.Add(ParsePseudoClass(pseudo));
                }
            }
            else if (input[i] == '*')
            {
                parts.Add(new UniversalSelector());
                i++;
            }
            else if (input[i] == '[')
            {
                i++; // skip [
                var attrSelector = ParseAttributeSelector(input, ref i);
                parts.Add(attrSelector);
            }
            else
            {
                var name = ReadIdentifier(input, ref i);
                if (!string.IsNullOrEmpty(name))
                    parts.Add(new TagSelector(name));
            }
        }

        return parts.Count == 1 ? parts[0] : new CompoundSelector(parts);
    }

    private static Selector ParsePseudoClass(string name) => name switch
    {
        "hover" => new HoverSelector(),
        "active" => new ActiveSelector(),
        "focus" => new FocusSelector(),
        "disabled" => new DisabledSelector(),
        "enabled" => new EnabledSelector(),
        "first-child" => new FirstChildSelector(),
        "last-child" => new LastChildSelector(),
        "first-of-type" => new FirstOfTypeSelector(),
        "last-of-type" => new LastOfTypeSelector(),
        "empty" => new EmptySelector(),
        "checked" => new CheckedSelector(),
        _ => throw new ArgumentException($"Unknown pseudo-class: :{name}")
    };

    private static Selector ParsePseudoElement(string name) => name switch
    {
        "before" => new BeforePseudoElement(),
        "after" => new AfterPseudoElement(),
        _ => throw new ArgumentException($"Unknown pseudo-element: ::{name}")
    };

    /// <summary>
    /// 解析属性选择器 [attr], [attr="value"], [attr~="value"], 等。
    /// 调用时 i 指向 [ 之后的第一个字符。
    /// </summary>
    private static AttributeSelector ParseAttributeSelector(string input, ref int i)
    {
        // 跳过前导空格
        while (i < input.Length && char.IsWhiteSpace(input[i])) i++;

        // 读取属性名
        int start = i;
        while (i < input.Length && IsIdentifierChar(input[i])) i++;
        if (i == start)
            throw new ArgumentException($"Expected attribute name in attribute selector at position {i}");
        string attrName = input.Substring(start, i - start);

        // 跳过空格
        while (i < input.Length && char.IsWhiteSpace(input[i])) i++;

        // 检查操作符
        AttributeMatchOperator op = AttributeMatchOperator.Exists;
        string? value = null;

        if (i < input.Length && input[i] != ']')
        {
            // 读取操作符: =, ~=, |=, ^=, $=, *=
            if (input[i] == '=')
            {
                op = AttributeMatchOperator.Equals;
                i++;
            }
            else if (i + 1 < input.Length && input[i + 1] == '=')
            {
                op = input[i] switch
                {
                    '~' => AttributeMatchOperator.Includes,
                    '|' => AttributeMatchOperator.DashMatch,
                    '^' => AttributeMatchOperator.Prefix,
                    '$' => AttributeMatchOperator.Suffix,
                    '*' => AttributeMatchOperator.Substring,
                    _ => throw new ArgumentException($"Unknown attribute operator '{input[i]}=' at position {i}")
                };
                i += 2;
            }
            else
            {
                throw new ArgumentException($"Expected attribute operator at position {i}, got '{input[i]}'");
            }

            // 跳过空格
            while (i < input.Length && char.IsWhiteSpace(input[i])) i++;

            // 读取值（带引号或不带）
            if (i >= input.Length)
                throw new ArgumentException($"Expected attribute value after operator at position {i}");

            if (input[i] == '"' || input[i] == '\'')
            {
                char quote = input[i];
                i++;
                start = i;
                while (i < input.Length && input[i] != quote)
                    i++;
                if (i >= input.Length)
                    throw new ArgumentException($"Unclosed string in attribute selector starting at {start - 1}");
                value = input.Substring(start, i - start);
                i++; // skip closing quote
            }
            else
            {
                // 无引号标识符
                start = i;
                while (i < input.Length && IsIdentifierChar(input[i])) i++;
                value = input.Substring(start, i - start);
            }

            // 跳过空格
            while (i < input.Length && char.IsWhiteSpace(input[i])) i++;
        }

        // 期望 ]
        if (i >= input.Length || input[i] != ']')
            throw new ArgumentException($"Expected ']' to close attribute selector at position {i}");
        i++; // skip ]

        return new AttributeSelector(attrName, op, value);
    }

    private static bool IsIdentifierChar(char c)
        => char.IsLetterOrDigit(c) || c == '_' || c == '-';

    private static string ReadIdentifier(string input, ref int i)
    {
        var start = i;
        while (i < input.Length && (char.IsLetterOrDigit(input[i]) || input[i] == '-' || input[i] == '_'))
            i++;
        return input[start..i];
    }

    private static string ReadUntilClose(string input, ref int i)
    {
        var depth = 1;
        var start = i;
        while (i < input.Length && depth > 0)
        {
            if (input[i] == '(') depth++;
            else if (input[i] == ')') depth--;
            if (depth > 0) i++;
        }
        var result = input[start..i];
        if (i < input.Length) i++; // skip )
        return result;
    }

    private static List<string> SplitGroups(string input)
    {
        var result = new List<string>();
        var depth = 0;
        var start = 0;

        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == '(') depth++;
            else if (input[i] == ')') depth--;
            else if (input[i] == ',' && depth == 0)
            {
                result.Add(input[start..i]);
                start = i + 1;
            }
        }
        result.Add(input[start..]);
        return result;
    }

    private static List<(char combinator, string selector)> SplitByCombinators(string input)
    {
        var result = new List<(char combinator, string selector)>();
        var i = 0;
        var current = "";
        char pendingCombinator = ' ';
        bool isFirst = true;

        while (i < input.Length)
        {
            if (input[i] == '(')
            {
                var start = i;
                var depth = 1;
                i++;
                while (i < input.Length && depth > 0)
                {
                    if (input[i] == '(') depth++;
                    else if (input[i] == ')') depth--;
                    i++;
                }
                current += input[start..i];
            }
            else if (input[i] == '[')
            {
                // 属性选择器：跳过 [...] 内的所有内容（包括空格），避免将其误判为组合器
                var start = i;
                i++; // skip [
                while (i < input.Length && input[i] != ']')
                {
                    // 处理引号内的字符（可能包含 ] 字符）
                    if (input[i] == '"' || input[i] == '\'')
                    {
                        char quote = input[i];
                        i++;
                        while (i < input.Length && input[i] != quote)
                            i++;
                        if (i < input.Length) i++; // skip closing quote
                    }
                    else
                    {
                        i++;
                    }
                }
                if (i < input.Length) i++; // skip ]
                current += input[start..i];
            }
            else if (input[i] == '>' || input[i] == '+' || input[i] == '~')
            {
                if (current.Trim().Length > 0)
                {
                    result.Add((isFirst ? ' ' : pendingCombinator, current.Trim()));
                    isFirst = false;
                }
                pendingCombinator = input[i];
                current = "";
                i++;
            }
            else if (input[i] == ' ')
            {
                // 可能是后代组合器，也可能是组合器周围的空格
                var trimmed = current.Trim();
                if (trimmed.Length > 0)
                {
                    // 向前看是否有显式组合器
                    var j = i;
                    while (j < input.Length && input[j] == ' ') j++;
                    if (j < input.Length && (input[j] == '>' || input[j] == '+' || input[j] == '~'))
                    {
                        // 空格后面是显式组合器，跳过空格
                        i = j;
                        continue;
                    }
                    // 这是后代组合器
                    result.Add((isFirst ? ' ' : pendingCombinator, trimmed));
                    isFirst = false;
                    pendingCombinator = ' ';
                    current = "";
                }
                i++;
            }
            else
            {
                current += input[i];
                i++;
            }
        }

        if (current.Trim().Length > 0)
            result.Add((isFirst ? ' ' : pendingCombinator, current.Trim()));

        return result;
    }
}
