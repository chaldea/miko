using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Miko.Routing;

/// <summary>
/// A route template in Blazor's syntax (ISSUE-164): literal segments, parameters (<c>{id}</c>),
/// typed constraints (<c>{id:int}</c>), optional parameters (<c>{id?}</c>, <c>{id:int?}</c>) and a
/// trailing catch-all (<c>{*rest}</c>).
///
/// <para>Matching follows Blazor: literals compare case-insensitively, segments are URL-decoded,
/// the query string and fragment are ignored, a value that fails its constraint means the route
/// does not match (also for an optional parameter), and a catch-all takes zero or more segments.
/// Constraint values are parsed with the invariant culture, so <c>/price/19.99</c> means the same
/// on every device.</para>
/// </summary>
public sealed partial class RouteTemplate
{
    private static readonly IReadOnlyDictionary<string, object?> s_noValues =
        new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>());

    private readonly TemplateSegment[] _segments;

    private RouteTemplate(string template, TemplateSegment[] segments)
    {
        Template = template;
        _segments = segments;
    }

    /// <summary>The template as written, e.g. <c>/user/{id:int}</c>.</summary>
    public string Template { get; }

    public IReadOnlyList<TemplateSegment> Segments => _segments;

    /// <summary>
    /// Parses <paramref name="template"/>. Throws <see cref="ArgumentException"/> for a template
    /// Blazor would reject, so a typo fails when the route is registered, not when it is visited.
    /// </summary>
    public static RouteTemplate Parse(string template)
    {
        ArgumentNullException.ThrowIfNull(template);
        if (!template.StartsWith('/'))
            throw Invalid(template, "Route templates must start with '/'.");

        var parts = template.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var segments = new TemplateSegment[parts.Length];
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenOptional = false;

        for (int i = 0; i < parts.Length; i++)
        {
            var segment = ParseSegment(template, parts[i]);

            if (segment.IsCatchAll && i != parts.Length - 1)
                throw Invalid(template, "A catch-all parameter can only appear as the last segment of the route template.");
            if (seenOptional && !segment.IsOptional && !segment.IsCatchAll)
                throw Invalid(template, "Non-optional parameters or literal segments cannot appear after optional parameters.");
            if (segment.IsParameter && !names.Add(segment.ParameterName!))
                throw Invalid(template, $"The route parameter name '{segment.ParameterName}' appears more than one time.");

            seenOptional |= segment.IsOptional;
            segments[i] = segment;
        }

        return new RouteTemplate(template, segments);
    }

    /// <summary>
    /// Matches <paramref name="path"/> against this template. Returns the route values keyed by
    /// parameter name (case-insensitive; typed by the constraint, <see cref="string"/> without
    /// one), or null when the path does not match. Absent optional parameters and an empty
    /// catch-all have no entry.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Match(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return Match(SplitPath(path));
    }

    /// <summary>
    /// Splits a navigation path into decoded segments: the query string and fragment are dropped,
    /// empty segments (leading, trailing or doubled slashes) are skipped. The router splits once
    /// and matches the result against every template.
    /// </summary>
    internal static string[] SplitPath(string path)
    {
        var end = path.AsSpan().IndexOfAny('?', '#');
        if (end >= 0)
            path = path[..end];

        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
            parts[i] = Uri.UnescapeDataString(parts[i]);
        return parts;
    }

    internal IReadOnlyDictionary<string, object?>? Match(string[] parts)
    {
        Dictionary<string, object?>? values = null;

        for (int i = 0; i < _segments.Length; i++)
        {
            var segment = _segments[i];

            if (segment.IsCatchAll)
            {
                if (i < parts.Length)
                    (values ??= NewValues())[segment.ParameterName!] = string.Join('/', parts, i, parts.Length - i);
                return values ?? s_noValues;
            }

            if (i >= parts.Length)
            {
                // Parse guarantees that only optional parameters (and a catch-all) follow an
                // optional one, so the path may stop at the first optional segment.
                return segment.IsOptional ? values ?? s_noValues : null;
            }

            if (segment.IsLiteral)
            {
                if (!string.Equals(segment.Value, parts[i], StringComparison.OrdinalIgnoreCase))
                    return null;
                continue;
            }

            // A present value must satisfy the constraint, optional or not: /user/1/abc does not
            // match /user/{id:int}/{option:bool?}.
            if (!TryConvert(parts[i], segment.Kind, out var value))
                return null;
            (values ??= NewValues())[segment.ParameterName!] = value;
        }

        return parts.Length == _segments.Length ? values ?? s_noValues : null;
    }

    /// <summary>
    /// Orders two templates by Blazor's route precedence: segment by segment, a literal beats a
    /// constrained parameter, which beats an unconstrained one, which beats a catch-all; on a tie
    /// the template with fewer segments wins. 0 means equal precedence (the router then keeps
    /// registration order).
    /// </summary>
    internal static int ComparePrecedence(RouteTemplate x, RouteTemplate y)
    {
        var shared = Math.Min(x._segments.Length, y._segments.Length);
        for (int i = 0; i < shared; i++)
        {
            var result = x._segments[i].Rank.CompareTo(y._segments[i].Rank);
            if (result != 0)
                return result;
        }
        return x._segments.Length.CompareTo(y._segments.Length);
    }

    private static Dictionary<string, object?> NewValues() => new(StringComparer.OrdinalIgnoreCase);

    private static TemplateSegment ParseSegment(string template, string part)
    {
        if (part.IndexOfAny(['{', '}']) < 0)
            return new TemplateSegment(part, null, null, RouteConstraintKind.None, isOptional: false, isCatchAll: false);

        var match = ParameterPattern().Match(part);
        if (!match.Success)
            throw Invalid(template,
                $"'{part}' is not a valid parameter segment. Use {{name}}, {{name:constraint}}, {{name?}} or {{*name}}; " +
                "a segment cannot mix literal text and a parameter.");

        var isCatchAll = match.Groups["catchAll"].Success;
        var name = match.Groups["name"].Value;
        var isOptional = match.Groups["optional"].Success;
        string? constraint = null;
        var kind = RouteConstraintKind.None;

        if (match.Groups["constraint"].Success)
        {
            constraint = match.Groups["constraint"].Value.ToLowerInvariant();
            kind = constraint switch
            {
                "bool" => RouteConstraintKind.Bool,
                "datetime" => RouteConstraintKind.DateTime,
                "decimal" => RouteConstraintKind.Decimal,
                "double" => RouteConstraintKind.Double,
                "float" => RouteConstraintKind.Float,
                "guid" => RouteConstraintKind.Guid,
                "int" => RouteConstraintKind.Int,
                "long" => RouteConstraintKind.Long,
                _ => throw Invalid(template,
                    $"Unsupported constraint '{match.Groups["constraint"].Value}' on parameter '{name}'. " +
                    "Supported constraints: bool, datetime, decimal, double, float, guid, int, long."),
            };
        }

        if (isCatchAll && isOptional)
            throw Invalid(template, $"The catch-all parameter '{name}' cannot be marked optional; it already matches zero segments.");
        if (isCatchAll && constraint != null)
            throw Invalid(template, $"The catch-all parameter '{name}' cannot have a constraint; its value is always a string.");

        return new TemplateSegment(null, name, constraint, kind, isOptional, isCatchAll);
    }

    private static bool TryConvert(string text, RouteConstraintKind kind, out object? value)
    {
        var culture = CultureInfo.InvariantCulture;
        bool ok;
        switch (kind)
        {
            case RouteConstraintKind.None:
                value = text;
                return true;
            case RouteConstraintKind.Bool:
                ok = bool.TryParse(text, out var b); value = b; return ok;
            case RouteConstraintKind.DateTime:
                ok = DateTime.TryParse(text, culture, DateTimeStyles.None, out var dt); value = dt; return ok;
            case RouteConstraintKind.Decimal:
                ok = decimal.TryParse(text, NumberStyles.Number, culture, out var m); value = m; return ok;
            case RouteConstraintKind.Double:
                ok = double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, culture, out var d); value = d; return ok;
            case RouteConstraintKind.Float:
                ok = float.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, culture, out var f); value = f; return ok;
            case RouteConstraintKind.Guid:
                ok = Guid.TryParse(text, out var g); value = g; return ok;
            case RouteConstraintKind.Int:
                ok = int.TryParse(text, NumberStyles.Integer, culture, out var n); value = n; return ok;
            case RouteConstraintKind.Long:
                ok = long.TryParse(text, NumberStyles.Integer, culture, out var l); value = l; return ok;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    private static ArgumentException Invalid(string template, string reason)
        => new($"Invalid route template '{template}'. {reason}", "template");

    [GeneratedRegex(@"^\{(?<catchAll>\*)?(?<name>[A-Za-z_][A-Za-z0-9_]*)(?::(?<constraint>[A-Za-z]+))?(?<optional>\?)?\}$")]
    private static partial Regex ParameterPattern();
}

/// <summary>One segment of a <see cref="RouteTemplate"/>: a literal or a parameter.</summary>
public sealed class TemplateSegment
{
    internal TemplateSegment(
        string? value, string? parameterName, string? constraint, RouteConstraintKind kind, bool isOptional, bool isCatchAll)
    {
        Value = value;
        ParameterName = parameterName;
        Constraint = constraint;
        Kind = kind;
        IsOptional = isOptional;
        IsCatchAll = isCatchAll;
    }

    /// <summary>The literal text; null for a parameter.</summary>
    public string? Value { get; }

    /// <summary>The parameter name; null for a literal.</summary>
    public string? ParameterName { get; }

    /// <summary>The constraint in lower case (<c>int</c>, <c>bool</c>, …); null when unconstrained.</summary>
    public string? Constraint { get; }

    /// <summary>Whether the parameter ends with <c>?</c>.</summary>
    public bool IsOptional { get; }

    /// <summary>Whether the parameter starts with <c>*</c> and takes the rest of the path.</summary>
    public bool IsCatchAll { get; }

    public bool IsLiteral => Value != null;

    public bool IsParameter => ParameterName != null;

    internal RouteConstraintKind Kind { get; }

    /// <summary>Precedence rank (lower wins): literal, constrained, unconstrained, catch-all.</summary>
    internal int Rank => IsLiteral ? 0 : IsCatchAll ? 3 : Kind != RouteConstraintKind.None ? 1 : 2;
}

internal enum RouteConstraintKind
{
    None,
    Bool,
    DateTime,
    Decimal,
    Double,
    Float,
    Guid,
    Int,
    Long,
}

/// <summary>The route a path resolved to, with the values of its parameters.</summary>
public sealed class RouteMatch
{
    internal RouteMatch(RouteData route, IReadOnlyDictionary<string, object?> parameters)
    {
        Route = route;
        Parameters = parameters;
    }

    /// <summary>The registered route that matched.</summary>
    public RouteData Route { get; }

    [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(Components.ComponentTypeMembers.Activation)]
    public Type ComponentType => Route.ComponentType;

    /// <summary>
    /// Route values keyed by parameter name (case-insensitive), typed by their constraint
    /// (<c>{id:int}</c> → <see cref="int"/>; unconstrained and catch-all → <see cref="string"/>).
    /// </summary>
    public IReadOnlyDictionary<string, object?> Parameters { get; }
}
