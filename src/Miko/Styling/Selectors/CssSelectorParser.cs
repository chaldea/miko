using System.Globalization;
using Miko.Core;

namespace Miko.Styling.Selectors;

/// <summary>
/// CSS 选择器字符串解析器
/// </summary>
public static class CssSelectorParser
{
    public static Selector Parse(string input) => ParseList(input, nested: false);

    /// <summary>
    /// 解析选择器列表（逗号分隔）。<paramref name="nested"/> 表示处于函数式伪类的参数里
    /// （<c>:is()</c>、<c>:not()</c>、<c>:has()</c>、<c>:nth-child(… of S)</c>），那里不允许出现伪元素。
    /// </summary>
    private static Selector ParseList(string input, bool nested)
    {
        input = input.Trim();

        // 处理分组选择器 (逗号分隔)
        var groups = SplitGroups(input);
        if (groups.Count > 1)
        {
            var selectors = groups.Select(g => ParseSingle(g.Trim(), nested)).ToArray();
            return new GroupSelector(selectors);
        }

        return ParseSingle(input, nested);
    }

    private static Selector ParseSingle(string input, bool nested)
    {
        // 按组合器拆分: >, +, ~, 空格(后代)
        var segments = SplitByCombinators(input);
        if (segments.Count == 0)
            throw new ArgumentException($"Empty selector in '{input}'");

        if (segments.Count == 1)
            return ParseCompound(segments[0].selector, nested);

        // 从左到右构建组合器链
        var result = ParseCompound(segments[0].selector, nested);
        for (int i = 1; i < segments.Count; i++)
        {
            var right = ParseCompound(segments[i].selector, nested);
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

    private static Selector ParseCompound(string input, bool nested)
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
                    // 伪元素不是元素的条件，放进 :is()/:not()/:has() 的参数里没有意义（CSS 同样视为无效）；
                    // 静默接受会让 PseudoElementSelector.Matches 恒为 true 的语义混进参数匹配。
                    if (nested)
                        throw new ArgumentException($"Pseudo-element ::{pseudo} is not allowed inside a functional pseudo-class");
                    parts.Add(ParsePseudoElement(pseudo));
                }
                else if (i < input.Length && input[i] == '(')
                {
                    i++; // skip (
                    var argument = ReadUntilClose(input, ref i);
                    parts.Add(ParseFunctionalPseudoClass(pseudo, argument));
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
                // 读不出标识符说明遇到了不认识的字符（如游离的括号）：必须报错——不前进的话这里会死循环。
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException($"Unexpected character '{input[i]}' in selector '{input}'");
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
        "focus-within" => new FocusWithinSelector(),
        "disabled" => new DisabledSelector(),
        "enabled" => new EnabledSelector(),
        "first-child" => new FirstChildSelector(),
        "last-child" => new LastChildSelector(),
        "first-of-type" => new FirstOfTypeSelector(),
        "last-of-type" => new LastOfTypeSelector(),
        "empty" => new EmptySelector(),
        "checked" => new CheckedSelector(),
        "root" => new RootSelector(),
        "only-child" => new OnlyChildSelector(),
        "only-of-type" => new OnlyOfTypeSelector(),
        _ => throw new ArgumentException($"Unknown pseudo-class: :{name}")
    };

    /// <summary>
    /// 解析带参数的伪类（ISSUE-158）。参数里的选择器一律按 <c>nested</c> 解析；与浏览器的「宽容」
    /// 选择器列表不同，<c>:is()</c>/<c>:where()</c> 里有不认识的选择器同样抛出——Miko 的样式表由代码
    /// 构建，静默丢弃一支只会把拼写错误藏起来。
    /// </summary>
    private static Selector ParseFunctionalPseudoClass(string name, string argument)
    {
        switch (name)
        {
            case "not":
                return new NotSelector(ParseList(argument, nested: true));
            case "is":
                return new IsSelector(ParseList(argument, nested: true));
            case "where":
                return new WhereSelector(ParseList(argument, nested: true));
            case "has":
                return new HasSelector(SplitGroups(argument).Select(ParseRelative));
            case "nth-child":
            {
                var (a, b, of) = ParseNth(argument, allowOf: true);
                return new NthChildSelector(a, b, of);
            }
            case "nth-last-child":
            {
                var (a, b, of) = ParseNth(argument, allowOf: true);
                return new NthLastChildSelector(a, b, of);
            }
            case "nth-of-type":
            {
                var (a, b, _) = ParseNth(argument, allowOf: false);
                return new NthOfTypeSelector(a, b);
            }
            case "nth-last-of-type":
            {
                var (a, b, _) = ParseNth(argument, allowOf: false);
                return new NthLastOfTypeSelector(a, b);
            }
            default:
                throw new ArgumentException($"Unknown functional pseudo-class: :{name}()");
        }
    }

    private static Selector ParsePseudoElement(string name) => name switch
    {
        "before" => new BeforePseudoElement(),
        "after" => new AfterPseudoElement(),
        "range-thumb" => new RangeThumbPseudoElement(),
        "range-track" => new RangeTrackPseudoElement(),
        "range-progress" => new RangeProgressPseudoElement(),
        "placeholder" => new PlaceholderPseudoElement(),
        _ => throw new ArgumentException($"Unknown pseudo-element: ::{name}")
    };

    /// <summary>
    /// 解析 <c>:has()</c> 的一个相对选择器：可选的开头组合器（<c>&gt;</c> <c>+</c> <c>~</c>，省略即后代）
    /// 后接普通的组合器链，如 <c>&gt; .a .b</c>。
    /// </summary>
    private static RelativeSelector ParseRelative(string input)
    {
        input = input.Trim();
        var leading = SelectorCombinator.Descendant;
        if (input.Length > 0 && input[0] is '>' or '+' or '~')
        {
            leading = ToCombinator(input[0]);
            input = input[1..].TrimStart();
        }

        var segments = SplitByCombinators(input);
        if (segments.Count == 0)
            throw new ArgumentException("Empty relative selector in :has()");

        var steps = new RelativeSelectorStep[segments.Count];
        for (int k = 0; k < segments.Count; k++)
        {
            var combinator = k == 0 ? leading : ToCombinator(segments[k].combinator);
            steps[k] = new RelativeSelectorStep(combinator, ParseCompound(segments[k].selector, nested: true));
        }
        return new RelativeSelector(steps);
    }

    private static SelectorCombinator ToCombinator(char c) => c switch
    {
        '>' => SelectorCombinator.Child,
        '+' => SelectorCombinator.NextSibling,
        '~' => SelectorCombinator.SubsequentSibling,
        _ => SelectorCombinator.Descendant,
    };

    /// <summary>解析 <c>:nth-*()</c> 的参数：<c>An+B</c>，<paramref name="allowOf"/> 时可再跟 <c>of S</c>。</summary>
    private static (int a, int b, Selector? of) ParseNth(string argument, bool allowOf)
    {
        argument = argument.Trim();
        Selector? of = null;
        int ofAt = allowOf ? IndexOfOfKeyword(argument) : -1;
        if (ofAt >= 0)
        {
            of = ParseList(argument[(ofAt + 2)..], nested: true);
            argument = argument[..ofAt];
        }

        var (a, b) = ParseAnPlusB(argument);
        return (a, b, of);
    }

    // An+B 与选择器列表之间的 of 关键字：前后都须是空白。参数已去掉首尾空白，odd 的 o 位于开头，
    // 不会被误认（循环从 1 开始且要求前一个字符是空白）。
    private static int IndexOfOfKeyword(string s)
    {
        for (int i = 1; i + 2 < s.Length; i++)
        {
            if (char.IsWhiteSpace(s[i - 1]) && s[i] is 'o' or 'O' && s[i + 1] is 'f' or 'F' && char.IsWhiteSpace(s[i + 2]))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// 解析 CSS 的 An+B 微语法（<c>odd</c>、<c>even</c>、<c>3</c>、<c>2n+1</c>、<c>-n+3</c>、<c>n</c>……），返回 (A, B)。
    /// 符号与 B 之间允许空白（<c>2n + 1</c>），A 与 n 之间、正负号与 n 之间不允许（<c>2 n</c>、<c>+ n</c> 无效）。
    /// </summary>
    internal static (int a, int b) ParseAnPlusB(string text)
    {
        var s = text.Trim();
        if (s.Equals("odd", StringComparison.OrdinalIgnoreCase)) return (2, 1);
        if (s.Equals("even", StringComparison.OrdinalIgnoreCase)) return (2, 0);

        int n = s.IndexOfAny(['n', 'N']);
        if (n < 0)
            return (0, ParseSignedInteger(s, text));

        var aPart = s[..n];
        int a = aPart switch
        {
            "" or "+" => 1,
            "-" => -1,
            _ => ParseSignedInteger(aPart, text),
        };

        var rest = s[(n + 1)..].TrimStart();
        if (rest.Length == 0) return (a, 0);
        if (rest[0] is not ('+' or '-'))
            throw InvalidAnPlusB(text);

        var digits = rest[1..].TrimStart();
        if (digits.Length == 0 || !IsAsciiDigits(digits)
            || !int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out int b))
            throw InvalidAnPlusB(text);
        return (a, rest[0] == '-' ? -b : b);
    }

    // [+-]?数字，中间不得有空白。
    private static int ParseSignedInteger(string s, string original)
    {
        var digits = s.Length > 0 && s[0] is '+' or '-' ? s[1..] : s;
        if (digits.Length == 0 || !IsAsciiDigits(digits)
            || !int.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value))
            throw InvalidAnPlusB(original);
        return value;
    }

    private static bool IsAsciiDigits(string s)
    {
        foreach (var c in s)
        {
            if (!char.IsAsciiDigit(c)) return false;
        }
        return true;
    }

    private static ArgumentException InvalidAnPlusB(string text)
        => new($"Invalid An+B expression '{text}' (expected e.g. odd, even, 3, 2n+1, -n+3)");

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

    /// <summary>读取函数式伪类的参数：调用时 i 指向 ( 之后，返回时 i 指向配对的 ) 之后。</summary>
    private static string ReadUntilClose(string input, ref int i)
    {
        var start = i;
        SkipToClose(input, ref i);
        if (i >= input.Length)
            throw new ArgumentException($"Unclosed '(' in selector '{input}'");
        var result = input[start..i];
        i++; // skip )
        return result;
    }

    /// <summary>
    /// 从 ( 之后前进到与之配对的 )（找不到则停在末尾）。引号内的字符不计——属性值里可能有括号或逗号
    /// （<c>:is([title="a)b"])</c>）。
    /// </summary>
    private static void SkipToClose(string input, ref int i)
    {
        var depth = 1;
        while (i < input.Length)
        {
            var c = input[i];
            if (c is '"' or '\'')
            {
                SkipQuoted(input, ref i);
                continue;
            }
            if (c == '(') depth++;
            else if (c == ')' && --depth == 0) return;
            i++;
        }
    }

    // i 指向开引号；返回时 i 指向闭引号之后（未闭合则停在末尾）。
    private static void SkipQuoted(string input, ref int i)
    {
        var quote = input[i++];
        while (i < input.Length && input[i] != quote) i++;
        if (i < input.Length) i++;
    }

    private static List<string> SplitGroups(string input)
    {
        var result = new List<string>();
        var depth = 0;
        var start = 0;

        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] is '"' or '\'')
            {
                // 引号内的逗号不是分组分隔符（[data-x="a,b"]）。SkipQuoted 停在闭引号之后，抵消循环的 i++。
                SkipQuoted(input, ref i);
                i--;
            }
            else if (input[i] == '(') depth++;
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
                // 函数式伪类的参数整体属于当前段：其中的空格与 > + ~（:has(> .a)）不是本层的组合器。
                var start = i;
                i++;
                SkipToClose(input, ref i);
                if (i < input.Length) i++; // skip )
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
