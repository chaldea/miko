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
        _ => throw new ArgumentException($"Unknown pseudo-class: :{name}")
    };

    private static Selector ParsePseudoElement(string name) => name switch
    {
        "before" => new BeforePseudoElement(),
        "after" => new AfterPseudoElement(),
        _ => throw new ArgumentException($"Unknown pseudo-element: ::{name}")
    };

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
