// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language.Components.Css;

/// <summary>
/// The C# a declaration block translates to (ISSUE-161): assignments to <c>Style</c> properties
/// (longhands only — the last declaration of a property wins, as in CSS) and custom properties.
/// Expressions may contain <see cref="CssPlaceholder"/> markers where a C# expression from the
/// markup goes.
/// </summary>
internal sealed class CssStyleBlock
{
    private readonly List<string> _order = new();
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);
    private readonly List<string> _varOrder = new();
    private readonly Dictionary<string, string> _vars = new(StringComparer.Ordinal);

    public bool IsEmpty => _order.Count == 0 && _varOrder.Count == 0;

    public IReadOnlyList<string> Properties => _order;

    public string this[string property] => _values[property];

    public bool TryGet(string property, out string expression) => _values.TryGetValue(property, out expression!);

    public void Set(string property, string expression)
    {
        if (!_values.ContainsKey(property)) _order.Add(property);
        _values[property] = expression;
    }

    public void SetVar(string name, string expression)
    {
        if (!_vars.ContainsKey(name)) _varOrder.Add(name);
        _vars[name] = expression;
    }

    /// <summary><c>new TypeName { A = …, B = …, Vars = … }</c>.</summary>
    public string ToInitializer(string typeName, string indent)
    {
        var builder = new StringBuilder();
        builder.Append("new ").Append(typeName);
        if (IsEmpty)
        {
            return builder.Append("()").ToString();
        }

        var inner = indent + "    ";
        builder.Append('\n').Append(indent).Append("{\n");
        foreach (var property in _order)
        {
            builder.Append(inner).Append(property).Append(" = ").Append(_values[property]).Append(",\n");
        }

        if (_varOrder.Count > 0)
        {
            builder.Append(inner).Append("Vars = new global::System.Collections.Generic.Dictionary<string, global::Miko.Styling.VarValue>\n")
                .Append(inner).Append("{\n");
            foreach (var name in _varOrder)
            {
                builder.Append(inner).Append("    [").Append(CssPropertyTranslator.Str(name)).Append("] = ").Append(_vars[name]).Append(",\n");
            }

            builder.Append(inner).Append("},\n");
        }

        builder.Append(indent).Append('}');
        return builder.ToString();
    }
}
