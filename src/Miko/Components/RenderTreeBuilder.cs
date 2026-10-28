using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Miko.Components;

public class RenderTreeBuilder
{
    private readonly Stack<Element> _stack = new();
    private readonly Stack<ComponentFrame> _componentStack = new();
    private Element? _root;

    // ---------------------------------------------------------------------
    // 组件身份与复用（ISSUE-154 BUG-01）
    //
    // 一个组件的每次渲染用一个新的构建器。构建器拿着该组件「上一次渲染产出的子组件」
    // (_previousComponents)：OpenComponent 按身份（见 ComponentIdentity）从中认领实例——
    // 身份不变就复用原实例（字段、订阅、进行中的异步工作都保留，OnInitialized 不再执行），
    // 否则新建。本次渲染产出的子组件记入 _components，成为下一次渲染的 _previousComponents；
    // 没被认领的旧实例随旧子树一起被拆解（见 ComponentBase.StampRoot）。
    // ---------------------------------------------------------------------

    private readonly ComponentRegistry? _previousComponents;
    private ComponentRegistry? _components;
    private ComponentRegistry? _recycledComponents;

    /// <summary>当前身份作用域：所在的渲染片段调用点与带键元素（见 <see cref="RenderScope"/>）。</summary>
    private RenderScope? _scope;

    /// <summary>打开中的带键元素，及打开它之前的作用域（元素关闭时恢复）。</summary>
    private Stack<(Element Element, RenderScope? Scope)>? _keyedElements;

    /// <summary>本次渲染已用过的元素键，按父元素区分，用于报告重复的 <c>@key</c>。</summary>
    private HashSet<(object Parent, object Key)>? _elementKeys;

    /// <summary>直接成为构建器根的那个组件产出（组件的标记以另一个组件开头时）。</summary>
    private (ComponentBase Component, Element Element)? _rootComponent;

    /// <summary>Creates a builder with no previous render to reuse components from.</summary>
    public RenderTreeBuilder() { }

    /// <summary>
    /// Creates the builder for one render of a component. <paramref name="previousComponents"/> are
    /// the nested components its previous render produced; components whose identity is unchanged are
    /// claimed from it instead of being recreated.
    /// </summary>
    /// <param name="previousComponents">The previous render's nested components.</param>
    /// <param name="recycled">An emptied registry whose storage this render may use for its own.</param>
    internal RenderTreeBuilder(ComponentRegistry? previousComponents, ComponentRegistry? recycled = null)
    {
        _previousComponents = previousComponents;
        _recycledComponents = recycled;
    }

    /// <summary>The nested components this render produced, by identity (null when there were none).</summary>
    internal ComponentRegistry? Components => _components;

    /// <summary>
    /// The component whose produced element is this builder's root — set when the rendered markup
    /// is a single component (the outer component then shares that component's root element).
    /// </summary>
    internal ComponentBase? RootComponent
        => _rootComponent is { } root && ReferenceEquals(_root, root.Element) ? root.Component : null;

    /// <summary>
    /// An open component. Strongly typed opens resolve the instance immediately (the compiler passes
    /// the key along); the name-based <see cref="OpenComponent{T}(int)"/> path may receive its key
    /// later through <see cref="SetKey"/>, so it records its parameters and resolves the instance on
    /// <see cref="CloseComponent"/>.
    /// </summary>
    private readonly struct ComponentFrame
    {
        public ComponentFrame(ComponentBase component) { Component = component; Deferred = null; }
        public ComponentFrame(DeferredComponent deferred) { Component = null; Deferred = deferred; }

        public ComponentBase? Component { get; }
        public DeferredComponent? Deferred { get; }
    }

    private sealed class DeferredComponent
    {
        public DeferredComponent(
            [DynamicallyAccessedMembers(ComponentTypeMembers.Activation)] Type type, int sequence)
        {
            Type = type;
            Sequence = sequence;
        }

        [DynamicallyAccessedMembers(ComponentTypeMembers.Activation)]
        public Type Type { get; }
        public int Sequence { get; }
        public object? Key { get; set; }
        public List<(string Name, object? Value)>? Parameters { get; set; }
        public List<Action<object>>? Captures { get; set; }
    }

    private static readonly Dictionary<string, Func<Element>> _tagMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["a"] = () => new AnchorElement(),
        ["div"] = () => new DivElement(),
        ["span"] = () => new SpanElement(),
        ["p"] = () => new ParagraphElement(),
        ["button"] = () => new ButtonElement(),
        ["input"] = () => new InputElement(),
        ["textarea"] = () => new TextAreaElement(),
        ["select"] = () => new SelectElement(),
        ["option"] = () => new OptionElement(),
        ["optgroup"] = () => new OptGroupElement(),
        ["label"] = () => new LabelElement(),
        ["br"] = () => new BrElement(),
        ["hr"] = () => new HrElement(),
        ["h1"] = () => new H1Element(),
        ["h2"] = () => new H2Element(),
        ["h3"] = () => new H3Element(),
        ["h4"] = () => new H4Element(),
        ["h5"] = () => new H5Element(),
        ["h6"] = () => new H6Element(),
        ["ul"] = () => new UlElement(),
        ["ol"] = () => new OlElement(),
        ["li"] = () => new LiElement(),
        ["img"] = () => new ImageElement(),
        ["video"] = () => new VideoElement(),
        ["table"] = () => new TableElement(),
        ["caption"] = () => new CaptionElement(),
        ["colgroup"] = () => new ColgroupElement(),
        ["col"] = () => new ColElement(),
        ["thead"] = () => new TheadElement(),
        ["tbody"] = () => new TbodyElement(),
        ["tfoot"] = () => new TfootElement(),
        ["tr"] = () => new TrElement(),
        ["th"] = () => new ThElement(),
        ["td"] = () => new TdElement(),
        ["nav"] = () => new NavElement(),
        ["strong"] = () => new StrongElement(),
        ["b"] = () => new BElement(),
        ["pre"] = () => new PreElement(),
        ["code"] = () => new CodeElement(),
        // 绘制回调只能在 C# 里给（@ref 拿到元素后赋 Paint），标签本身此前却会抛 Unknown tag。
        ["canvas"] = () => new CanvasElement(),
    };

    public void OpenElement(int seq, string tagName)
    {
        if (!_tagMap.TryGetValue(tagName, out var factory))
            throw new InvalidOperationException($"Unknown tag: {tagName}");
        _stack.Push(factory());
    }

    // ---------------------------------------------------------------------
    // 强类型构建 API（ISSUE-136）
    //
    // 旧 API 是 Blazor 形态的「序号 + 字符串名 + object? 值」，于是每个节点都要在运行时
    // 重做一遍编译期已知的事：标签名查字典取工厂委托、属性名过约 70 分支的 switch +
    // 类型测试链、组件参数走 GetProperty + SetValue 反射（无缓存，值类型还要装箱）。
    //
    // 新 API 把这些搬到编译期：Razor 编译器直接发射具体元素/组件类型与属性赋值，
    // 构建路径只剩下 new T() 与字段写入——零反射、零装箱。旧重载保留为回退路径，
    // 供 AddMarkupContent 的运行时 HTML 解析（标签与属性名只有运行时才知道）使用。
    // ---------------------------------------------------------------------

    /// <summary>
    /// Opens an element of a statically known type and returns the instance so the caller
    /// (normally generated code) can assign its properties directly.
    ///
    /// <para>Must be paired with <see cref="CloseElement"/>, which performs the textarea
    /// normalization and parent attachment that the string-based path also does.</para>
    /// </summary>
    public T OpenElement<T>() where T : Element, new()
    {
        var element = new T();
        _stack.Push(element);
        return element;
    }

    /// <summary>
    /// Opens an element that has already been constructed (used when generated code needs to
    /// pass constructor arguments, or when a caller reuses a retained element).
    /// </summary>
    public T OpenElement<T>(T element) where T : Element
    {
        ArgumentNullException.ThrowIfNull(element);
        _stack.Push(element);
        return element;
    }

    /// <summary>
    /// The element currently open, or <c>null</c> when none is. Generated code uses this to
    /// reach the open element without keeping its own local when convenient.
    /// </summary>
    public Element? CurrentElement => _stack.Count > 0 ? _stack.Peek() : null;

    /// <summary>
    /// Sets an element's <c>value</c> the way an HTML <c>value</c> attribute behaves.
    ///
    /// <para>A <c>null</c> value means the Razor expression evaluated to null, i.e. no value is
    /// being declared. An absent <c>value</c> attribute does not clear an input in the browser,
    /// so the element's own text is left alone — otherwise an ancestor re-render would wipe what
    /// the user typed (see <see cref="InputElement.DeclaredValue"/>, ISSUE-121).</para>
    ///
    /// <para>Range inputs carry their position in <see cref="InputElement.NumericValue"/>; the
    /// two are kept in step so <c>&lt;input type="range" @bind="_v" /&gt;</c> reflects the bound
    /// number.</para>
    /// </summary>
    public static void SetInputValue(InputElement input, string? value)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (value is null) return;
        input.Value = value;
        if (input.Type == InputType.Range &&
            float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var numeric))
        {
            input.NumericValue = numeric;
        }
    }

    /// <summary>See <see cref="SetInputValue"/> — same ISSUE-121 semantics for textarea.</summary>
    public static void SetTextAreaValue(TextAreaElement textArea, string? value)
    {
        ArgumentNullException.ThrowIfNull(textArea);
        if (value is not null) textArea.Value = value;
    }

    /// <summary>
    /// Sets <see cref="TextAreaElement.Rows"/> from an HTML attribute string. A value that is not
    /// an integer leaves the element's default in place, matching the legacy attribute switch.
    /// </summary>
    public static void SetTextAreaRows(TextAreaElement textArea, string? value)
    {
        ArgumentNullException.ThrowIfNull(textArea);
        if (int.TryParse(value, out var rows)) textArea.Rows = rows;
    }

    /// <summary>See <see cref="SetTextAreaRows"/>.</summary>
    public static void SetTextAreaCols(TextAreaElement textArea, string? value)
    {
        ArgumentNullException.ThrowIfNull(textArea);
        if (int.TryParse(value, out var cols)) textArea.Cols = cols;
    }

    // ---------------------------------------------------------------------
    // ISSUE-153 问题 3：此前不在属性表里、被静默丢弃的常用属性。
    //
    // 值参数一律接受 object?：属性值可以是字面量字符串，也可以是任意类型的表达式
    // （<c>&lt;option value="@item.Id"&gt;</c>、<c>colspan="@n"</c>）。生成代码把表达式原样
    // 传进来，限定为 string 会让这些写法编译失败——而它们此前虽无效果，却能编译通过。
    // ---------------------------------------------------------------------

    /// <summary>
    /// A string-valued attribute (<c>placeholder</c>, <c>&lt;label for&gt;</c>,
    /// <c>&lt;option value&gt;</c>, …). Strings pass through; other values are formatted with the
    /// invariant culture, so a numeric option value matches the same number bound with <c>@bind</c>.
    /// </summary>
    public static string? FormatAttributeValue(string? value) => value;

    /// <inheritdoc cref="FormatAttributeValue(string?)"/>
    public static string? FormatAttributeValue(object? value)
        => value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : value?.ToString();

    /// <summary>
    /// HTML <c>disabled</c>: marks the element <see cref="ElementState.Disabled"/>, which also
    /// disables its descendants (<see cref="Element.IsDisabled"/>) and matches <c>:disabled</c>.
    /// A null value means the attribute is absent.
    /// </summary>
    public static void SetDisabled(Element element, string? value)
        => SetDisabled(element, value is not null && ParseHtmlBool(value));

    /// <inheritdoc cref="SetDisabled(Element, string?)"/>
    public static void SetDisabled(Element element, bool value)
    {
        ArgumentNullException.ThrowIfNull(element);
        // 此刻元素尚未进入布局树，无需失效。不写 false：新建的元素本来就没有禁用态。
        if (value) element.SetState(ElementState.Disabled, invalidate: false);
    }

    /// <inheritdoc cref="SetDisabled(Element, string?)"/>
    public static void SetDisabled(Element element, bool? value) => SetDisabled(element, value ?? false);

    /// <summary>
    /// <c>colspan</c> / <c>rowspan</c> / <c>span</c>. HTML treats a missing, invalid or zero value
    /// as 1 and caps it at 1000 (<c>rowspan="0"</c>'s "to the end of the section" is not modelled).
    /// </summary>
    public static int ParseSpanAttribute(object? value)
        => TryParseInteger(value, out var span) ? Math.Clamp(span, 1, 1000) : 1;

    /// <summary>
    /// <c>maxlength</c> on <c>&lt;input&gt;</c> / <c>&lt;textarea&gt;</c>. A negative or invalid value
    /// means no limit, as in HTML.
    /// </summary>
    public static int? ParseMaxLength(object? value)
        => TryParseInteger(value, out var max) && max >= 0 ? max : null;

    /// <summary><c>&lt;ol start&gt;</c>. An invalid value keeps the default numbering.</summary>
    public static void SetListStart(OlElement list, object? value)
    {
        ArgumentNullException.ThrowIfNull(list);
        if (TryParseInteger(value, out var start)) list.Start = start;
    }

    /// <summary><c>&lt;li value&gt;</c>. An invalid value keeps the item's ordinal number.</summary>
    public static void SetListItemValue(LiElement item, object? value)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (TryParseInteger(value, out var ordinal)) item.Value = ordinal;
    }

    /// <summary>Range <c>min</c>. An invalid value keeps the default (0).</summary>
    public static void SetInputMin(InputElement input, object? value)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (TryParseFloat(value, out var min)) input.Min = min;
    }

    /// <summary>Range <c>max</c>. An invalid value keeps the default (100).</summary>
    public static void SetInputMax(InputElement input, object? value)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (TryParseFloat(value, out var max)) input.Max = max;
    }

    /// <summary>
    /// <c>&lt;select value&gt;</c>. The options are the select's children, which are built after
    /// its attributes, so the value is recorded here and applied when the element closes
    /// (<see cref="CloseElement"/>). Without this slot <c>&lt;select @bind&gt;</c> never showed
    /// the bound value. A null value declares nothing, as for inputs (ISSUE-121).
    /// </summary>
    public static void SetSelectValue(SelectElement select, object? value)
    {
        ArgumentNullException.ThrowIfNull(select);
        if (FormatAttributeValue(value) is { } text) select.DeclaredValue = text;
    }

    private static bool TryParseInteger(object? value, out int result)
    {
        switch (value)
        {
            case int i: result = i; return true;
            case long l when l is >= int.MinValue and <= int.MaxValue: result = (int)l; return true;
            default:
                return int.TryParse(FormatAttributeValue(value)?.Trim(), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out result);
        }
    }

    private static bool TryParseFloat(object? value, out float result)
    {
        switch (value)
        {
            case float f: result = f; return true;
            case double d: result = (float)d; return true;
            case int i: result = i; return true;
            default:
                return float.TryParse(FormatAttributeValue(value)?.Trim(), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out result);
        }
    }

    /// <summary>
    /// Parses an HTML <c>input type</c> attribute. Kept here (rather than inlined by the
    /// compiler) so a runtime-valued <c>type</c> expression resolves identically to a literal.
    /// </summary>
    public static InputType ParseInputType(string? value) => value?.ToLowerInvariant() switch
    {
        "checkbox" => InputType.Checkbox,
        "radio" => InputType.Radio,
        "password" => InputType.Password,
        "range" => InputType.Range,
        "search" => InputType.Search,
        // number/tel ask the platform for a numeric keypad; both edit as plain text.
        "number" or "tel" => InputType.Number,
        _ => InputType.Text,
    };

    /// <summary>
    /// HTML boolean attribute semantics, for generated code that has a runtime-valued
    /// expression rather than a literal. See <see cref="ParseHtmlBool"/>.
    /// </summary>
    public static bool ParseBooleanAttribute(string? value) => ParseHtmlBool(value);

    /// <summary>
    /// Boolean overload. <c>@bind</c> on <c>&lt;input type="checkbox"&gt;</c> lowers to
    /// <c>BindConverter.FormatValue(bool)</c>, which returns a <c>bool</c> rather than a string —
    /// so the generated assignment must accept one directly (ISSUE-115).
    /// </summary>
    public static bool ParseBooleanAttribute(bool value) => value;

    /// <summary>
    /// Nullable boolean overload (<c>BindConverter.FormatValue(bool?)</c>). A null value means
    /// the attribute is absent, which in HTML boolean terms is false.
    /// </summary>
    public static bool ParseBooleanAttribute(bool? value) => value ?? false;

    /// <summary>
    /// Assigns an inline style, accepting the <see cref="Styling.Style"/> objects Razor authors
    /// normally pass (<c>style="@SomeStyle"</c>).
    /// </summary>
    public static void SetStyle(Element element, Styling.Style? style)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.Style = style;
    }

    /// <summary>
    /// Overload for a <c>style</c> expression whose value is not a <see cref="Styling.Style"/>
    /// (most often a CSS string). Miko has no CSS-string parser for inline styles, so such a
    /// value is ignored — exactly as the legacy attribute switch did, where only a
    /// <see cref="Styling.Style"/> matched and anything else fell through unhandled.
    /// Kept as an overload rather than a compile error so existing components keep building.
    /// </summary>
    public static void SetStyle(Element element, object? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (value is Styling.Style style) element.Style = style;
    }

    /// <summary>
    /// Binds an <see cref="EventCallback{T}"/> to a strongly-typed handler slot on an element.
    /// The wrapper invokes the callback (fires InvokeAsync → user delegate → StateHasChanged on
    /// the receiver component when the Task completes, marshaled back to the render thread).
    ///
    /// <para>Returns <c>null</c> for a callback with no delegate, so an absent handler leaves
    /// the slot untouched rather than installing a wrapper that does nothing.</para>
    /// </summary>
    public static MikoEventHandler<T>? ToHandler<T>(EventCallback<T> callback)
        where T : MikoEventArgs
        => callback.HasDelegate ? arg => _ = callback.InvokeAsync(arg) : null;

    /// <summary>
    /// Appends literal or expression text to the open element. Strongly-typed counterpart of
    /// <see cref="AddContent(int, object?)"/> that avoids boxing the value.
    /// </summary>
    public void AddContent(string? text)
    {
        if (string.IsNullOrEmpty(text)) return;
        AppendText(WebUtility.HtmlDecode(text));
    }

    /// <summary>Renders a child-content fragment. See <see cref="AddContent(int, object?)"/>.</summary>
    public void AddContent(RenderFragment? fragment)
    {
        // A RenderFragment may emit top-level elements (e.g. a transparent CascadingValue whose
        // ChildContent is rendered with no open element on the stack), so invoke it regardless
        // of stack depth.
        if (fragment is not null) RenderFragmentInScope(-1, fragment);
    }

    /// <summary>
    /// Invokes <paramref name="fragment"/> inside an identity scope for its call site. The components a
    /// fragment renders carry sequence numbers from the file that defined it, so without the scope
    /// they could collide with the rendering component's own (ISSUE-154).
    /// </summary>
    private void RenderFragmentInScope(int sequence, RenderFragment fragment)
    {
        var outer = _scope;
        _scope = new RenderScope(outer, sequence, null);
        try
        {
            fragment(this);
        }
        finally
        {
            _scope = outer;
        }
    }

    public void CloseElement()
    {
        var element = _stack.Pop();
        if (_keyedElements is { Count: > 0 } keyed && ReferenceEquals(keyed.Peek().Element, element))
            _scope = keyed.Pop().Scope;
        NormalizeTextArea(element);
        ApplyDeclaredSelectValue(element);
        if (_stack.Count > 0)
            _stack.Peek().AddChild(element);
        else
            // 顶层元素：经 AttachToTree 处理多根（多个顶层元素并入一个透明片段），
            // 避免后一个根覆盖前一个（如 <video/> 之后再跟条件块时丢失 video）。
            AttachToTree(element);
    }

    /// <summary>
    /// HTML 中 textarea 的初始文本写在标签内容里（<c>&lt;textarea&gt;初始值&lt;/textarea&gt;</c>），
    /// 而非 value 属性。这里把其子文本节点回收进 <see cref="TextAreaElement.Value"/> 并移除，
    /// 使这些文本作为可编辑内容由元素统一渲染，而不是作为普通子节点参与布局。
    /// 若 value 属性已显式提供，则以属性为准，仅清理子文本节点。
    /// </summary>
    private static void NormalizeTextArea(Element element)
    {
        if (element is not TextAreaElement textArea) return;
        var childText = textArea.TextContent;
        if (textArea.Value is null && !string.IsNullOrEmpty(childText))
        {
            textArea.Value = childText;
        }
        textArea.Children.RemoveAll(c => c is TextNode);
    }

    /// <summary>See <see cref="SetSelectValue"/>: the options exist once the select closes.</summary>
    private static void ApplyDeclaredSelectValue(Element element)
    {
        if (element is SelectElement { DeclaredValue: { } value } select)
            select.Value = value;
    }

    // ---------------------------------------------------------------------
    // 名字驱动的回退路径
    //
    // 编译器现在为「标签与属性名在编译期已知」的绝大多数情况发射强类型调用（见上文
    // ISSUE-136 一节）。下面这些重载留给名字只有运行时才知道的场合：
    //
    //   * AddMarkupContent —— 用正则解析 HTML 字符串，标签与属性都是运行时值；
    //   * 元素上的槽位表未覆盖的属性（role、aria-* 等）—— 与此前一样被静默忽略；
    //   * 表达式内容（AddContent(seq, object?)）—— 需要 object? 重载做 ToString；
    //   * 泛型类型推断的组件 —— 属性名在发射点尚不可知。
    //
    // 因此它们**不是** [Obsolete]：生成代码仍在正常使用这条路径。修改上面的强类型表时，
    // 必须与此处的 switch 保持一致——两者是同一份语义的两种表达。
    // ---------------------------------------------------------------------

    public void AddAttribute(int seq, string name, string? value)
    {
        if (_componentStack.Count > 0)
        {
            AddComponentParameter(seq, name, value);
            return;
        }
        if (_stack.Count == 0) return;
        var element = _stack.Peek();
        switch (name)
        {
            case "class": element.Class = value; break;
            case "id": element.Id = value; break;
            case "dir": element.Dir = value; break;
            case "href" when element is AnchorElement anchor:
                anchor.Href = value; break;
            case "target" when element is AnchorElement anchor:
                anchor.Target = value; break;
            case "rel" when element is AnchorElement anchor:
                anchor.Rel = value; break;
            case "type" when element is InputElement input:
                input.Type = value?.ToLowerInvariant() switch
                {
                    "checkbox" => InputType.Checkbox,
                    "radio" => InputType.Radio,
                    "password" => InputType.Password,
                    "range" => InputType.Range,
                    "search" => InputType.Search,
                    // number/tel ask the platform for a numeric keypad; both edit as plain text.
                    "number" or "tel" => InputType.Number,
                    _ => InputType.Text,
                };
                break;
            case "checked" when element is InputElement checkableInput:
                checkableInput.Checked = ParseHtmlBool(value); break;
            case "value" when element is InputElement valueInput:
                // A null value means the Razor expression evaluated to null, i.e. no value is being
                // declared. Leave the element's own text alone (an absent `value` attribute does not
                // clear an input in the browser) so an ancestor re-render cannot wipe what the user
                // typed — see InputElement.DeclaredValue (ISSUE-121).
                if (value is null) break;
                valueInput.Value = value;
                // Range inputs carry their position in NumericValue; keep the two in step so
                // `<input type="range" @bind="_v" />` reflects the bound number.
                if (valueInput.Type == InputType.Range &&
                    float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var numeric))
                {
                    valueInput.NumericValue = numeric;
                }
                break;
            case "src" when element is ImageElement img:
                img.Source = value; break;
            case "placeholder" when element is ImageElement placeholderImg:
                placeholderImg.Placeholder = value; break;
            case "placeholder" when element is TextAreaElement textArea:
                textArea.Placeholder = value; break;
            case "value" when element is TextAreaElement valueTextArea:
                // See the InputElement case above (ISSUE-121).
                if (value is not null) valueTextArea.Value = value;
                break;
            case "rows" when element is TextAreaElement rowsTextArea:
                if (int.TryParse(value, out var rows)) rowsTextArea.Rows = rows;
                break;
            case "cols" when element is TextAreaElement colsTextArea:
                if (int.TryParse(value, out var cols)) colsTextArea.Cols = cols;
                break;
            case "src" when element is VideoElement video:
                video.Source = value; break;
            case "poster" when element is VideoElement video:
                video.Poster = value; break;
            case "autoplay" when element is VideoElement video:
                video.AutoPlay = ParseHtmlBool(value); break;
            case "loop" when element is VideoElement video:
                video.Loop = ParseHtmlBool(value); break;
            case "muted" when element is VideoElement video:
                video.Muted = ParseHtmlBool(value); break;
            case "controls" when element is VideoElement video:
                video.Controls = ParseHtmlBool(value); break;
            case "language" when element is CodeElement code:
                code.Language = value; break;
            case "highlight" when element is CodeElement code:
                code.Highlight = ParseHtmlBool(value); break;
            // ISSUE-153 问题 3：与 MikoElements 新增的槽位一一对应。
            case "disabled":
                SetDisabled(element, value); break;
            case "placeholder" when element is InputElement placeholderInput:
                placeholderInput.Placeholder = value; break;
            case "readonly" when element is InputElement readOnlyInput:
                readOnlyInput.ReadOnly = ParseHtmlBool(value); break;
            case "maxlength" when element is InputElement maxLengthInput:
                maxLengthInput.MaxLength = ParseMaxLength(value); break;
            case "readonly" when element is TextAreaElement readOnlyTextArea:
                readOnlyTextArea.ReadOnly = ParseHtmlBool(value); break;
            case "maxlength" when element is TextAreaElement maxLengthTextArea:
                maxLengthTextArea.MaxLength = ParseMaxLength(value); break;
            case "min" when element is InputElement minInput:
                SetInputMin(minInput, value); break;
            case "max" when element is InputElement maxInput:
                SetInputMax(maxInput, value); break;
            case "for" when element is LabelElement label:
                label.For = value; break;
            case "value" when element is OptionElement option:
                option.Value = value; break;
            case "selected" when element is OptionElement selectedOption:
                selectedOption.Selected = ParseHtmlBool(value); break;
            case "label" when element is OptGroupElement group:
                group.Label = value; break;
            case "value" when element is SelectElement select:
                SetSelectValue(select, value); break;
            case "multiple" when element is SelectElement multiSelect:
                multiSelect.Multiple = ParseHtmlBool(value); break;
            case "colspan" when element is TdElement td:
                td.ColSpan = ParseSpanAttribute(value); break;
            case "rowspan" when element is TdElement td:
                td.RowSpan = ParseSpanAttribute(value); break;
            case "colspan" when element is ThElement th:
                th.ColSpan = ParseSpanAttribute(value); break;
            case "rowspan" when element is ThElement th:
                th.RowSpan = ParseSpanAttribute(value); break;
            case "span" when element is ColElement col:
                col.Span = ParseSpanAttribute(value); break;
            case "span" when element is ColgroupElement colgroup:
                colgroup.Span = ParseSpanAttribute(value); break;
            case "start" when element is OlElement ol:
                SetListStart(ol, value); break;
            case "value" when element is LiElement li:
                SetListItemValue(li, value); break;
        }
    }

    /// <summary>
    /// HTML 布尔属性：存在即为真。Razor 通常以 <c>autoplay="true"</c>/<c>="false"</c> 传值，
    /// 因此显式的 "false" 视为假，其余（含空串、"true"、属性名本身）视为真。
    /// </summary>
    private static bool ParseHtmlBool(string? value)
        => !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);

    public void AddAttribute(int seq, string name, object? value)
    {
        if (_componentStack.Count > 0)
        {
            AddComponentParameter(seq, name, value);
            return;
        }
        if (_stack.Count == 0) return;
        var element = _stack.Peek();

        if (name is "style" or "Style" && value is Styling.Style style)
        {
            element.Style = style;
            return;
        }

        AddAttribute(seq, name, value?.ToString());
    }

    /// <summary>
    /// 布尔属性。<c>@bind</c> 生成的 <c>BindConverter.FormatValue(bool)</c> 返回 <c>bool</c>，
    /// 会落到本重载上（如 <c>&lt;input type="checkbox" @bind="_flag" /&gt;</c> 的 checked），
    /// 因此不能是空实现——否则绑定值无法投影到元素，复选框永远显示未选中。
    /// 按 HTML 布尔属性语义：false 表示"属性不存在"，直接跳过。
    /// </summary>
    public void AddAttribute(int seq, string name, bool value)
    {
        if (_componentStack.Count > 0)
        {
            AddComponentParameter(seq, name, value);
            return;
        }

        AddAttribute(seq, name, value ? "true" : "false");
    }

    /// <summary>
    /// 无值布尔属性（HTML 中如 <c>&lt;video autoplay loop muted&gt;</c>，Razor 生成 2 参重载）。
    /// 按 HTML 语义"出现即为真"，等价于值 "true"。
    /// </summary>
    public void AddAttribute(int seq, string name) => AddAttribute(seq, name, "true");

    public void AddAttribute<T>(int seq, string name, EventCallback<T> callback)
        where T : MikoEventArgs
    {
        if (_stack.Count == 0 || !callback.HasDelegate) return;
        var element = _stack.Peek();

        // Bind the EventCallback to the element's strongly-typed handler slot. The wrapper
        // invokes the callback (fires InvokeAsync → user delegate → StateHasChanged on the
        // receiver component when the Task completes, marshaled back to the render thread).
        switch (name)
        {
            case "onclick" when callback is EventCallback<MouseEventArgs> mc:
                element.OnClick = arg => _ = mc.InvokeAsync(arg); break;
            case "onmouseenter" when callback is EventCallback<MouseEventArgs> mc:
                element.OnMouseEnter = arg => _ = mc.InvokeAsync(arg); break;
            case "onmouseleave" when callback is EventCallback<MouseEventArgs> mc:
                element.OnMouseLeave = arg => _ = mc.InvokeAsync(arg); break;
            case "onmousedown" when callback is EventCallback<MouseEventArgs> mc:
                element.OnMouseDown = arg => _ = mc.InvokeAsync(arg); break;
            case "onmouseup" when callback is EventCallback<MouseEventArgs> mc:
                element.OnMouseUp = arg => _ = mc.InvokeAsync(arg); break;
            case "onmousemove" when callback is EventCallback<MouseEventArgs> mc:
                element.OnMouseMove = arg => _ = mc.InvokeAsync(arg); break;
            case "onpointerdown" when callback is EventCallback<PointerEventArgs> pc:
                element.OnPointerDown = arg => _ = pc.InvokeAsync(arg); break;
            case "onpointerup" when callback is EventCallback<PointerEventArgs> pc:
                element.OnPointerUp = arg => _ = pc.InvokeAsync(arg); break;
            case "onpointermove" when callback is EventCallback<PointerEventArgs> pc:
                element.OnPointerMove = arg => _ = pc.InvokeAsync(arg); break;
            case "onpointercancel" when callback is EventCallback<PointerEventArgs> pc:
                element.OnPointerCancel = arg => _ = pc.InvokeAsync(arg); break;
            case "onlongpress" when callback is EventCallback<PointerEventArgs> pc:
                element.OnLongPress = arg => _ = pc.InvokeAsync(arg); break;
            case "onfocus" when callback is EventCallback<FocusEventArgs> fc:
                element.OnFocus = arg => _ = fc.InvokeAsync(arg); break;
            case "onblur" when callback is EventCallback<FocusEventArgs> fc:
                element.OnBlur = arg => _ = fc.InvokeAsync(arg); break;
            case "onchange" when callback is EventCallback<ChangeEventArgs> cc:
                element.OnChange = arg => _ = cc.InvokeAsync(arg); break;
            case "onscroll" when callback is EventCallback<ScrollEventArgs> sc:
                element.OnScroll = arg => _ = sc.InvokeAsync(arg); break;
            case "onkeydown" when callback is EventCallback<KeyboardEventArgs> kc:
                element.OnKeyDown = arg => _ = kc.InvokeAsync(arg); break;
            case "oninput" when callback is EventCallback<InputEventArgs> ic:
                element.OnInput = arg => _ = ic.InvokeAsync(arg); break;
        }
    }

    /// <summary>Attach a retained native element, for components with custom drawing or media state.</summary>
    public void AddElement(int seq, Element element)
    {
        ArgumentNullException.ThrowIfNull(element);
        AttachToTree(element);
    }

    public void AddContent(int seq, object? text)
    {
        if (text is null) return;
        // A RenderFragment may emit top-level elements (e.g. a transparent CascadingValue whose
        // ChildContent is rendered with no open element on the stack), so invoke it regardless of
        // stack depth. Only literal/text content requires an open element to attach to.
        if (text is RenderFragment fragment)
        {
            RenderFragmentInScope(seq, fragment);
            return;
        }
        var str = text.ToString();
        if (string.IsNullOrEmpty(str)) return;
        AppendText(WebUtility.HtmlDecode(str));
    }

    /// <summary>
    /// 文本以有序 TextNode 子节点形式追加，保留与已打开的子元素的交错顺序（见 ISSUE-086）。
    /// Razor 会为每段内容（字面文本与表达式）发射一次 AddContent。相邻的纯文本片段合并到
    /// 同一末尾 TextNode（如 "Clicked " + _count + " times" 拼接为一段），但被子元素分隔的
    /// 文本会形成各自独立的 TextNode，从而正确表达 text1 &lt;span/&gt; text3。
    /// </summary>
    private void AppendText(string decoded)
    {
        if (_stack.Count == 0) return;
        var element = _stack.Peek();
        if (element.Children.Count > 0 && element.Children[^1] is TextNode lastText)
        {
            lastText.Text += decoded;
        }
        else
        {
            element.AddChild(new TextNode(decoded));
        }
    }

    public void AddMarkupContent(int seq, string? markup)
    {
        if (string.IsNullOrEmpty(markup)) return;
        ParseMarkup(markup);
    }

    /// <summary>
    /// Opens a component whose parameters are then set by name (<see cref="AddComponentParameter"/>),
    /// as the compiler does for type-inferred generic components. <paramref name="seq"/> identifies the
    /// call site; a key may follow through <see cref="SetKey"/>. The instance — reused from the previous
    /// render or new — is chosen when the component closes.
    /// </summary>
    public void OpenComponent<[DynamicallyAccessedMembers(ComponentTypeMembers.Activation)] T>(int seq) where T : ComponentBase, new()
    {
        _componentStack.Push(new ComponentFrame(new DeferredComponent(typeof(T), seq)));
    }

    /// <summary>
    /// Opens a component of a statically known type and returns the instance so the caller can assign
    /// its <see cref="ParameterAttribute"/> properties directly (ISSUE-136). Hand-written code that has
    /// no call-site number uses this overload; same-typed components opened by it match the previous
    /// render's instances by position. See <see cref="OpenComponent{T}(int, object?)"/>.
    /// </summary>
    public T OpenComponent<[DynamicallyAccessedMembers(ComponentTypeMembers.Activation)] T>() where T : ComponentBase, new()
        => OpenComponent<T>(-1, null);

    /// <summary>
    /// Opens a component of a statically known type and returns the instance so the caller
    /// (normally generated code) can assign its <see cref="ParameterAttribute"/> properties
    /// directly, instead of going through reflection by parameter name (ISSUE-136).
    ///
    /// <para>When the previous render of the component being built produced a component with the same
    /// identity — this call site (<paramref name="seq"/>), the enclosing fragment and keyed elements,
    /// and <paramref name="key"/> (<c>@key</c>), or for unkeyed components the same position — that
    /// instance is returned and keeps its state; only its parameters are assigned again
    /// (ISSUE-154 BUG-01). Parameters not assigned keep their previous values. Two components of one
    /// call site with equal keys throw <see cref="InvalidOperationException"/>.</para>
    ///
    /// <para>Must be paired with <see cref="CloseComponent"/>, which builds the component and
    /// attaches its produced element.</para>
    ///
    /// <para>This is the overload the Razor compiler emits for <b>every nested component</b>, so it
    /// is where the trimmer learns that component parameters must survive: without the annotation
    /// ILC strips the <c>[Inject]</c>/<c>[CascadingParameter]</c> properties of every nested
    /// component type and <see cref="ComponentParameterCache"/>'s <c>GetProperties</c> comes back
    /// empty at run time — injections stay null and cascading contexts never arrive (ISSUE-140).</para>
    /// </summary>
    public T OpenComponent<[DynamicallyAccessedMembers(ComponentTypeMembers.Activation)] T>(int seq, object? key) where T : ComponentBase, new()
    {
        var identity = CreateIdentity(typeof(T), seq, key);
        var component = ClaimPrevious(identity) as T ?? new T();
        Register(identity, component);
        _componentStack.Push(new ComponentFrame(component));
        return component;
    }

    /// <summary>The identity of the next component opened at this call site in this render.</summary>
    private ComponentIdentity CreateIdentity(Type type, int sequence, object? key)
    {
        var components = _components ??= _recycledComponents ?? new ComponentRegistry();
        return key is null
            ? new ComponentIdentity(_scope, type, sequence, null, components.NextOrdinal(_scope, type, sequence))
            : new ComponentIdentity(_scope, type, sequence, key, 0);
    }

    /// <summary>
    /// Takes the previous render's instance with this identity, if it is still alive, and prepares it
    /// to receive a new set of parameters from this render.
    /// </summary>
    private ComponentBase? ClaimPrevious(in ComponentIdentity identity)
    {
        if (identity.Key is not null && _components!.Contains(identity))
            throw DuplicateKey(identity);
        if (_previousComponents is null || !_previousComponents.TryClaim(identity, out var previous))
            return null;
        if (!previous.CanBeReused) return null;
        previous.BeginParentRender();
        return previous;
    }

    private void Register(in ComponentIdentity identity, ComponentBase component)
    {
        if (!_components!.TryAdd(identity, component))
            throw DuplicateKey(identity);
    }

    private static InvalidOperationException DuplicateKey(in ComponentIdentity identity)
        => new($"More than one {identity.Type.Name} component rendered at the same place uses the @key " +
               $"'{identity.Key}'. Keys must be unique among the components of a list.");

    /// <summary>
    /// Opens a component whose type is only known at runtime.
    ///
    /// <para>The annotation is the caller's obligation: whoever supplies
    /// <paramref name="componentType"/> must carry the same requirement all the way back to a
    /// <c>typeof(T)</c> the trimmer can see (e.g.
    /// <c>IonModalController.CreateAsync&lt;TComponent&gt;</c> →
    /// <c>IonModalOptions.Component</c> → here). There is deliberately <b>no</b> suppression on
    /// this method: an earlier one claimed the requirement was satisfied and thereby hid the
    /// unannotated <c>IonModalOptions.Component</c> hand-off, which would have let AOT trim a
    /// dynamically presented modal's parameters (ISSUE-140).</para>
    /// </summary>
    public void OpenComponent(int seq,
        [DynamicallyAccessedMembers(ComponentTypeMembers.Activation)] Type componentType)
    {
        if (!typeof(ComponentBase).IsAssignableFrom(componentType))
            throw new ArgumentException($"{componentType} is not a Miko component.", nameof(componentType));

        _componentStack.Push(new ComponentFrame(new DeferredComponent(componentType, seq)));
    }

    /// <summary>
    /// Chooses the instance for a name-based component frame now that its key is known, then applies
    /// the parameters and <c>@ref</c> captures recorded while it was open.
    /// </summary>
    private ComponentBase ResolveDeferred(DeferredComponent deferred)
    {
        var identity = CreateIdentity(deferred.Type, deferred.Sequence, deferred.Key);
        var component = ClaimPrevious(identity)
            ?? Activator.CreateInstance(deferred.Type) as ComponentBase
            ?? throw new InvalidOperationException($"Unable to create component {deferred.Type}.");
        Register(identity, component);

        if (deferred.Parameters is { } parameters)
        {
            foreach (var (name, value) in parameters)
                ApplyComponentParameter(component, name, value);
        }
        if (deferred.Captures is { } captures)
        {
            foreach (var capture in captures)
                capture(component);
        }
        return component;
    }

    /// <summary>
    /// Sets a component parameter by name, for the two cases the compiler cannot resolve
    /// statically: a weakly-typed attribute, and a component opened through the runtime-typed
    /// <see cref="OpenComponent(int, Type)"/>.
    /// </summary>
    public void AddComponentParameter(int seq, string name, object? value)
    {
        if (_componentStack.Count == 0) return;
        var frame = _componentStack.Peek();
        if (frame.Deferred is { } deferred)
        {
            (deferred.Parameters ??= new()).Add((name, value));
            return;
        }
        ApplyComponentParameter(frame.Component!, name, value);
    }

    private static void ApplyComponentParameter(ComponentBase component, string name, object? value)
    {
        var prop = FindParameterProperty(component.GetType(), name);
        if (prop is null)
        {
            // 组件上没有同名参数的元素事件（<IonChip @onclick="…">）挂到组件渲染出的元素上，
            // 如同 Ionic 宿主元素上的监听器。此前它被静默丢弃（ISSUE-153 问题 5）。
            component.TryAddHostEventListener(name, value);
            return;
        }

        // @onclick 写在声明了无参 EventCallback OnClick 的组件上（IonButton 等）：
        // 编译器按事件类型生成 EventCallback<MouseEventArgs>，参数类型对不上时转换过去。
        if (value is IEventCallback callback && prop.PropertyType == typeof(EventCallback))
            value = new EventCallback(callback.Receiver, callback.Delegate);

        prop.SetValue(component, value);
    }

    /// <summary>
    /// 按名字找组件参数：先精确匹配，再不区分大小写——与 Blazor 一致。<c>@onclick</c> 在组件上
    /// 生成名为 <c>"onclick"</c> 的属性，编译器也按不区分大小写把它与 <c>OnClick</c> 参数对应
    /// （同时写两者会报 RZ10010），区分大小写的查找让它永远落空（ISSUE-153 问题 5）。
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "The component reached this builder through OpenComponent<T> or " +
                        "OpenComponent(int, Type); both declare ComponentTypeMembers.Activation, " +
                        "which includes PublicProperties, the exact binding this lookup uses. The " +
                        "caller passes GetType() of that instance, which is merely statically " +
                        "unannotated (ISSUE-140).")]
    [UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "PublicProperties on the component type keeps the public properties its base " +
                        "types declare, so walking BaseType finds the same members the lookup above saw.")]
    private static PropertyInfo? FindParameterProperty(Type componentType, string name)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        try
        {
            return componentType.GetProperty(name, flags)
                ?? componentType.GetProperty(name, flags | BindingFlags.IgnoreCase);
        }
        catch (AmbiguousMatchException)
        {
            // 派生组件用 new 重新声明了基类的参数（Virtualize<TItem> 把 ChildContent 声明为
            // RenderFragment<TItem>，与 Blazor 相同）：取最派生的那一个，与 C# 名字查找一致。
            // 此前直接返回 null，参数被静默丢弃。
            for (var type = componentType; type != null; type = type.BaseType)
            {
                var declared = type.GetProperty(name, flags | BindingFlags.DeclaredOnly)
                    ?? type.GetProperty(name, flags | BindingFlags.DeclaredOnly | BindingFlags.IgnoreCase);
                if (declared != null) return declared;
            }
            return null;
        }
    }

    public void CloseComponent()
    {
        if (_componentStack.Count == 0) return;
        var frame = _componentStack.Pop();
        var component = frame.Component ?? ResolveDeferred(frame.Deferred!);
        // Build() also links the produced element back to the component (ComponentBase.StampRoot) so
        // the component is disposed when this element subtree is later discarded — and only then: a
        // component a later render reuses has moved on to a new element by that time. When the
        // component produced several top-level elements, `element` is a transparent FragmentElement
        // that stays in the tree (and is skipped by layout).
        var element = component.Build();
        component.AdoptProducedElement(element);
        if (_stack.Count > 0)
        {
            _stack.Peek().AddChild(element);
        }
        else
        {
            AttachToTree(element);
            if (ReferenceEquals(_root, element)) _rootComponent = (component, element);
        }
    }

    /// <summary>
    /// <c>@key</c>. On an element it scopes the identity of the components inside it — so a keyed list
    /// item keeps its components when items are inserted, removed or reordered — and pairs the element
    /// with its previous counterpart by key rather than by position when runtime state (focus, caret,
    /// layout) is carried over (ISSUE-154). Equal keys among the children of one element throw.
    ///
    /// <para>On a component opened by name (<see cref="OpenComponent{T}(int)"/>) it decides which
    /// previous instance is reused. Components opened with a strongly typed overload take their key as
    /// an <see cref="OpenComponent{T}(int, object?)"/> argument instead, because their parameters are
    /// assigned to the returned instance before a later <c>SetKey</c> could choose it.</para>
    /// </summary>
    public void SetKey(object? key)
    {
        if (key is null) return;

        if (_componentStack.Count > 0)
        {
            if (_componentStack.Peek().Deferred is { } deferred) deferred.Key = key;
            return;
        }

        if (_stack.Count == 0) return;
        var element = _stack.Peek();
        object parent = this;
        foreach (var open in _stack)
        {
            if (ReferenceEquals(open, element)) continue;
            parent = open;
            break;
        }
        if (!(_elementKeys ??= new()).Add((parent, key)))
            throw new InvalidOperationException(
                $"More than one <{element.TagName}> element rendered at the same place uses the @key '{key}'. " +
                "Keys must be unique among the children of an element.");

        element.Key = key;
        (_keyedElements ??= new()).Push((element, _scope));
        _scope = new RenderScope(_scope, -1, key);
    }

    // ---------------------------------------------------------------------
    // @ref（ISSUE-153 问题 4）
    //
    // 引用在构建时就地赋值：元素与组件实例此刻已经存在，不必像 Blazor 那样等到渲染之后。
    // 每次重渲染都产出新的元素实例，字段也随之更新到在场的那一个；组件实例身份不变时被复用
    // （ISSUE-154），引用随之保持不变。
    // ---------------------------------------------------------------------

    /// <summary>
    /// <c>@ref</c> on an element whose type is known at compile time. The capture is typed by the
    /// element (<c>CanvasElement</c>), so the field may be that type, <see cref="Element"/>, or
    /// <see cref="ElementReference"/>.
    /// </summary>
    public void AddElementReferenceCapture<TElement>(TElement element, Action<TElement> capture)
        where TElement : Element
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(capture);
        capture(element);

        // 顶层元素可能成为组件的根。无父组件的重渲染（StateHasChanged 的无父分支）会把新内容
        // 搬进旧根、丢弃新根，此时字段要改指向留下的旧根——见 RebindRootCaptures。
        if (_stack.Count == 1 && ReferenceEquals(_stack.Peek(), element))
            (_topLevelCaptures ??= new()).Add((element, e => { if (e is TElement typed) capture(typed); }));
    }

    /// <summary><c>@ref</c> on the open element when its type is only known at run time.</summary>
    public void AddElementReferenceCapture(int seq, Action<Element> capture)
    {
        ArgumentNullException.ThrowIfNull(capture);
        if (_stack.Count == 0) return;
        AddElementReferenceCapture(_stack.Peek(), capture);
    }

    /// <summary><c>@ref</c> on a component: captures the open component instance.</summary>
    public void AddComponentReferenceCapture(int seq, Action<object> capture)
    {
        ArgumentNullException.ThrowIfNull(capture);
        if (_componentStack.Count == 0) return;
        var frame = _componentStack.Peek();
        if (frame.Deferred is { } deferred)
            (deferred.Captures ??= new()).Add(capture);
        else
            capture(frame.Component!);
    }

    private List<(Element Element, Action<Element> Capture)>? _topLevelCaptures;

    /// <summary>
    /// Re-points the <c>@ref</c> fields that captured <paramref name="builtRoot"/> to
    /// <paramref name="liveRoot"/>, the element that keeps the content when a component without
    /// a parent element re-renders itself.
    /// </summary>
    internal void RebindRootCaptures(Element builtRoot, Element liveRoot)
    {
        if (_topLevelCaptures is null) return;
        foreach (var (element, capture) in _topLevelCaptures)
        {
            if (ReferenceEquals(element, builtRoot)) capture(liveRoot);
        }
    }

    /// <summary>
    /// Emitted after an <c>@bind</c>-generated change handler to record which value attribute that
    /// event writes back to. Blazor uses this so its diffing renderer can avoid clobbering the DOM
    /// value the user is currently typing into; Miko rebuilds the element tree outright and reads
    /// state from the elements themselves, so there is nothing to reconcile — this is a no-op kept
    /// for compatibility with the generated code.
    /// </summary>
    public void SetUpdatesAttributeName(string? updatesAttributeName) { }

    public void AttachElement(Element element)
    {
        if (_stack.Count > 0)
            _stack.Peek().AddChild(element);
        else
            AttachToTree(element);
    }

    public Element Build()
    {
        if (_stack.Count > 0)
            throw new InvalidOperationException("Unclosed elements remain in the render tree.");
        // A component that rendered nothing (e.g. a transparent CascadingValue with null
        // ChildContent) yields an empty transparent FragmentElement rather than throwing. The
        // empty fragment carries no layout box, so it represents "rendered nothing" faithfully.
        return _root ??= new FragmentElement();
    }

    private static readonly Regex _tokenRegex = new(
        @"<(?<close>/)?(?<tag>[a-zA-Z][a-zA-Z0-9]*)(?<attrs>[^>]*?)(?<self>/)?>|(?<text>[^<]+)",
        RegexOptions.Compiled);

    private static readonly Regex _attrRegex = new(
        @"(?<name>[a-zA-Z][a-zA-Z0-9\-]*)=""(?<value>[^""]*)""|(?<name2>[a-zA-Z][a-zA-Z0-9\-]*)='(?<value2>[^']*)'",
        RegexOptions.Compiled);

    private static readonly HashSet<string> _voidElements = new(StringComparer.OrdinalIgnoreCase)
        { "input", "img", "br", "hr", "meta", "link", "area", "base", "col", "embed", "param", "source", "track", "wbr" };

    private void ParseMarkup(string markup)
    {
        var localStack = new Stack<Element>();

        foreach (Match m in _tokenRegex.Matches(markup))
        {
            if (m.Groups["text"].Success)
            {
                // pre 子树内保留原始空白（预格式化语义）；其它场景裁剪标签间的格式空白。
                var rawText = m.Groups["text"].Value;
                bool inPre = false;
                foreach (var open in localStack)
                {
                    if (open is PreElement) { inPre = true; break; }
                }
                var text = inPre ? rawText : rawText.Trim();
                // 文本以有序 TextNode 追加，保留与标签的交错顺序（见 ISSUE-086）。
                if (text.Length > 0 && localStack.Count > 0)
                    localStack.Peek().AddChild(new TextNode(text));
                continue;
            }

            var tag = m.Groups["tag"].Value;
            var isClose = m.Groups["close"].Success;
            var isSelf = m.Groups["self"].Success || _voidElements.Contains(tag);

            if (isClose)
            {
                if (localStack.Count > 0)
                {
                    var el = localStack.Pop();
                    NormalizeTextArea(el);
                    if (localStack.Count > 0)
                        localStack.Peek().AddChild(el);
                    else
                        AttachToTree(el);
                }
                continue;
            }

            if (!_tagMap.TryGetValue(tag, out var factory)) continue;
            var element = factory();

            foreach (Match a in _attrRegex.Matches(m.Groups["attrs"].Value))
            {
                var name = a.Groups["name"].Success ? a.Groups["name"].Value : a.Groups["name2"].Value;
                var value = a.Groups["value"].Success ? a.Groups["value"].Value : a.Groups["value2"].Value;
                if (name == "class") element.Class = value;
                else if (name == "id") element.Id = value;
            }

            if (isSelf)
            {
                if (localStack.Count > 0)
                    localStack.Peek().AddChild(element);
                else
                    AttachToTree(element);
            }
            else
            {
                localStack.Push(element);
            }
        }

        while (localStack.Count > 0)
        {
            var el = localStack.Pop();
            NormalizeTextArea(el);
            if (localStack.Count > 0)
                localStack.Peek().AddChild(el);
            else
                AttachToTree(el);
        }
    }

    // 由多根自动生成的透明片段容器（非用户书写）。用于把全部顶层元素平铺承载。
    // 它留在 DOM 树中作为组件的稳定根（供 StateHasChanged 原地重渲染），但对布局透明
    // ——LayoutEngine 不为其建盒，而是把其子节点的盒子摊平进父级（等价 display:contents）。
    // 见 FragmentElement 与 LayoutEngine.AppendChildLayoutBoxes。
    private FragmentElement? _syntheticRoot;

    private void AttachToTree(Element element)
    {
        if (_stack.Count > 0)
        {
            _stack.Peek().AddChild(element);
            return;
        }

        if (_root is null)
        {
            _root = element;
            return;
        }

        // 出现第二个及以上顶层元素：用一个透明片段平铺承载全部顶层元素（不套不透明 div，
        // 避免破坏样式布局）。
        //
        // 承载片段必须是本构建器自己新建的，绝不能复用已有的根——即便它本身就是片段。
        // 那个片段是别的组件的稳定根（多根页面、透明 CascadingValue 的内容……），属于它的
        // 产出者：产出者重渲染时会把它整个换掉（StateHasChanged 的有父分支），于是此处并进去
        // 的兄弟节点随之丢失。ISSUE-153 问题 2：多根页面直接放在 IonApp 下，IonOverlayHost
        // 被并进了页面片段，页面第一次重渲染后弹层宿主就没了。片段套片段对布局无影响
        // （LayoutEngine.AppendChildLayoutBoxes 递归摊平）。
        if (_syntheticRoot is null)
        {
            _syntheticRoot = new FragmentElement();
            _syntheticRoot.AddChild(_root);
            _root = _syntheticRoot;
        }
        _syntheticRoot.AddChild(element);
    }
}
