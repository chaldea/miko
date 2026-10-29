// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Components.Css;

internal sealed partial class CssPropertyTranslator
{
    /// <summary>
    /// A calc() operand or result: a number or a length. Lengths are emitted as C# arithmetic on
    /// <c>Length</c> / <c>VarReference</c> / <c>CalcValue&lt;Length&gt;</c>, whose operators keep
    /// units apart until layout (and var() unresolved until the cascade); constant numbers fold.
    /// </summary>
    private sealed class CalcTerm(bool isNumber, string expression, double? constant)
    {
        public bool IsNumber { get; } = isNumber;
        public string Expression { get; } = expression;
        public double? Constant { get; } = constant;
    }

    private string? CalcLength(CssValue calc, LengthOptions options)
    {
        if ((options & LengthOptions.Percent) == 0 && ContainsKind(calc, CssTokenKind.Percentage))
            return Fail("percentages are not supported here.");

        var term = CalcRoot(calc);
        if (term == null) return null;
        if (!term.IsNumber) return term.Expression;
        if (term.Constant == 0) return $"{LengthType}.Px(0f)";
        if ((options & LengthOptions.Unitless) != 0) return $"{LengthType}.Number({term.Expression})";
        return Fail("calc() must produce a length here.");
    }

    private double? CalcConstant(CssValue calc)
    {
        var term = CalcRoot(calc);
        if (term == null) return null;
        if (!term.IsNumber || term.Constant is not { } constant)
        {
            Fail("calc() must produce a constant number here.");
            return null;
        }

        return constant;
    }

    private CalcTerm? CalcRoot(CssValue calc)
    {
        var tokens = calc.Children!;
        var position = 0;
        var term = CalcSum(tokens, ref position);
        if (term == null) return null;
        SkipWs(tokens, ref position);
        if (position < tokens.Count) return CalcFail("invalid calc() expression.");
        return term;
    }

    private CalcTerm? CalcFail(string message)
    {
        Fail(message);
        return null;
    }

    private CalcTerm? CalcSum(List<CssValue> tokens, ref int position)
    {
        var left = CalcProduct(tokens, ref position);
        while (left != null)
        {
            var save = position;
            var spaceBefore = SkipWs(tokens, ref position);
            if (position >= tokens.Count || !(tokens[position].IsDelim('+') || tokens[position].IsDelim('-')))
            {
                position = save;
                break;
            }

            var op = tokens[position].Token.Value;
            position++;
            var spaceAfter = SkipWs(tokens, ref position);
            if (!spaceBefore || !spaceAfter) return CalcFail("'+' and '-' in calc() need whitespace on both sides.");
            var right = CalcProduct(tokens, ref position);
            if (right == null) return null;
            left = Combine(left, op, right);
        }

        return left;
    }

    private CalcTerm? CalcProduct(List<CssValue> tokens, ref int position)
    {
        var left = CalcOperand(tokens, ref position);
        while (left != null)
        {
            var save = position;
            SkipWs(tokens, ref position);
            if (position >= tokens.Count || !(tokens[position].IsDelim('*') || tokens[position].IsDelim('/')))
            {
                position = save;
                break;
            }

            var op = tokens[position].Token.Value;
            position++;
            var right = CalcOperand(tokens, ref position);
            if (right == null) return null;
            left = Combine(left, op, right);
        }

        return left;
    }

    private CalcTerm? CalcOperand(List<CssValue> tokens, ref int position)
    {
        SkipWs(tokens, ref position);
        if (position >= tokens.Count) return CalcFail("invalid calc() expression.");
        var value = tokens[position++];
        if (value.Kind == CssTokenKind.LeftParen || (value.IsFunction && value.FunctionName == "calc"))
        {
            var inner = 0;
            var term = CalcSum(value.Children!, ref inner);
            if (term == null) return null;
            SkipWs(value.Children!, ref inner);
            if (inner < value.Children!.Count) return CalcFail("invalid calc() expression.");
            return new CalcTerm(term.IsNumber, "(" + term.Expression + ")", term.Constant);
        }

        if (value.Kind == CssTokenKind.Number && value.Children == null)
            return new CalcTerm(true, Float(value.Token.Number), value.Token.Number);
        if (value.Kind == CssTokenKind.Placeholder && value.Token.Value.Length == 0)
            return new CalcTerm(true, $"{BuilderType}.CssNumber({Placeholder(value)})", null);

        var length = Length(value, LengthOptions.Percent | LengthOptions.Negative);
        return length == null ? null : new CalcTerm(false, length, null);
    }

    private CalcTerm? Combine(CalcTerm a, string op, CalcTerm b)
    {
        switch (op)
        {
            case "+" or "-":
                if (a.IsNumber != b.IsNumber) return CalcFail("calc() cannot add a number to a length.");
                if (a.Constant is { } x && b.Constant is { } y)
                {
                    var sum = op == "+" ? x + y : x - y;
                    return new CalcTerm(true, Float(sum), sum);
                }

                return new CalcTerm(a.IsNumber, $"{a.Expression} {op} {b.Expression}", null);

            case "*":
                if (!a.IsNumber && !b.IsNumber) return CalcFail("calc() cannot multiply two lengths.");
                if (a.Constant is { } m && b.Constant is { } n) return new CalcTerm(true, Float(m * n), m * n);
                return new CalcTerm(a.IsNumber && b.IsNumber, $"{a.Expression} * {b.Expression}", null);

            default:
                if (!b.IsNumber) return CalcFail("calc() can only divide by a number.");
                if (b.Constant == 0) return CalcFail("division by zero in calc().");
                if (a.Constant is { } p && b.Constant is { } q) return new CalcTerm(true, Float(p / q), p / q);
                return new CalcTerm(a.IsNumber, $"{a.Expression} / {b.Expression}", null);
        }
    }

    private static bool SkipWs(List<CssValue> tokens, ref int position)
    {
        var start = position;
        while (position < tokens.Count && tokens[position].IsWhitespace) position++;
        return position > start;
    }

    private static bool ContainsKind(CssValue value, CssTokenKind kind)
    {
        if (value.Children == null) return value.Kind == kind;
        foreach (var child in value.Children)
        {
            if (ContainsKind(child, kind)) return true;
        }

        return false;
    }
}
