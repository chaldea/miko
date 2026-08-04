namespace Miko.Components;

/// <summary>
/// Declares that <c>@bind</c> (and <c>@bind-{suffix}</c>) on <paramref name="element"/> maps to the
/// pair of attributes <paramref name="valueAttribute"/> / <paramref name="changeAttribute"/>.
/// <para>
/// The Razor compiler reads these off the <c>BindAttributes</c> class (see
/// <see cref="BindAttributes"/>) to data-drive <c>@bind</c> on plain markup elements, so that
/// <c>&lt;input @bind="_text" /&gt;</c> lowers to a <c>value</c> attribute plus an
/// <c>onchange</c> handler.
/// </para>
/// </summary>
/// <param name="element">Tag name this mapping applies to, e.g. <c>"input"</c>.</param>
/// <param name="suffix">
/// The <c>@bind-{suffix}</c> suffix, or <c>null</c> for the unsuffixed <c>@bind</c> form.
/// </param>
/// <param name="valueAttribute">The attribute that receives the current value.</param>
/// <param name="changeAttribute">The event attribute that reports changes.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class BindElementAttribute(
    string element,
    string? suffix,
    string valueAttribute,
    string changeAttribute) : Attribute
{
    public string Element { get; } = element;

    public string? Suffix { get; } = suffix;

    public string ValueAttribute { get; } = valueAttribute;

    public string ChangeAttribute { get; } = changeAttribute;
}

/// <summary>
/// The <c>&lt;input&gt;</c>-specific form of <see cref="BindElementAttribute"/>: the mapping is
/// additionally keyed on the input's <c>type</c> attribute, because
/// <c>&lt;input type="text"&gt;</c> binds <c>value</c> while
/// <c>&lt;input type="checkbox"&gt;</c> binds <c>checked</c>.
/// </summary>
/// <param name="type">
/// The <c>type</c> attribute value this mapping applies to, or <c>null</c> to match any input.
/// </param>
/// <param name="suffix">
/// The <c>@bind-{suffix}</c> suffix, or <c>null</c> for the unsuffixed <c>@bind</c> form.
/// </param>
/// <param name="valueAttribute">The attribute that receives the current value.</param>
/// <param name="changeAttribute">The event attribute that reports changes.</param>
/// <param name="isInvariantCulture">
/// When true the value round-trips through the invariant culture, matching how HTML5 field types
/// (number, date, …) talk to the DOM.
/// </param>
/// <param name="format">Default format string applied when the user supplies no <c>@bind:format</c>.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class BindInputElementAttribute(
    string? type,
    string? suffix,
    string valueAttribute,
    string changeAttribute,
    bool isInvariantCulture,
    string? format) : Attribute
{
    public string? Type { get; } = type;

    public string? Suffix { get; } = suffix;

    public string ValueAttribute { get; } = valueAttribute;

    public string ChangeAttribute { get; } = changeAttribute;

    public bool IsInvariantCulture { get; } = isInvariantCulture;

    public string? Format { get; } = format;
}

/// <summary>
/// Carrier for the built-in element <c>@bind</c> mappings. The Razor compiler discovers this type by
/// name (a public type called <c>BindAttributes</c>) and reads its
/// <see cref="BindElementAttribute"/> / <see cref="BindInputElementAttribute"/> annotations — the
/// class itself is never instantiated.
/// <para>
/// Each entry mirrors what <see cref="RenderTreeBuilder"/> actually understands: the value attribute
/// must be one the builder assigns onto the element, and the change attribute must be a registered
/// event (see <see cref="EventHandlers"/>).
/// </para>
/// </summary>
[BindInputElement(null, null, "value", "onchange", false, null)]
[BindInputElement("checkbox", null, "checked", "onchange", false, null)]
[BindInputElement("text", null, "value", "onchange", false, null)]
[BindInputElement("password", null, "value", "onchange", false, null)]
[BindInputElement("search", null, "value", "onchange", false, null)]
[BindInputElement("range", null, "value", "onchange", true, null)]
[BindElement("select", null, "value", "onchange")]
[BindElement("textarea", null, "value", "onchange")]
public static class BindAttributes
{
}
