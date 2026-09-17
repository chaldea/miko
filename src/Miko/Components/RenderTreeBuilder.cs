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
    private readonly Stack<ComponentBase> _componentStack = new();
    private Element? _root;

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
        fragment?.Invoke(this);
    }

    public void CloseElement()
    {
        var element = _stack.Pop();
        NormalizeTextArea(element);
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
            fragment(this);
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

    public void OpenComponent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(int seq) where T : ComponentBase, new()
    {
        _componentStack.Push(new T());
    }

    /// <summary>
    /// Opens a component of a statically known type and returns the instance so the caller
    /// (normally generated code) can assign its <see cref="ParameterAttribute"/> properties
    /// directly, instead of going through reflection by parameter name (ISSUE-136).
    ///
    /// <para>Must be paired with <see cref="CloseComponent"/>, which builds the component and
    /// attaches its produced element.</para>
    /// </summary>
    public T OpenComponent<T>() where T : ComponentBase, new()
    {
        var component = new T();
        _componentStack.Push(component);
        return component;
    }

    /// <summary>Opens a component whose type is only known at runtime.</summary>
    [UnconditionalSuppressMessage("Trimming", "IL2072",
        Justification = "Runtime component types supplied by the application must preserve their public constructors and properties.")]
    public void OpenComponent(int seq,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type componentType)
    {
        if (!typeof(ComponentBase).IsAssignableFrom(componentType))
            throw new ArgumentException($"{componentType} is not a Miko component.", nameof(componentType));

        var component = Activator.CreateInstance(componentType) as ComponentBase
            ?? throw new InvalidOperationException($"Unable to create component {componentType}.");
        _componentStack.Push(component);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Component types are preserved via DynamicallyAccessedMembers on OpenComponent<T>")]
    public void AddComponentParameter(int seq, string name, object? value)
    {
        if (_componentStack.Count == 0) return;
        var component = _componentStack.Peek();
        var prop = component.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        prop?.SetValue(component, value);
    }

    public void CloseComponent()
    {
        if (_componentStack.Count == 0) return;
        var component = _componentStack.Pop();
        var element = component.Build();
        // Link the produced element back to its component so the component can be disposed
        // (e.g. unsubscribe from events) when this element subtree is later discarded. When the
        // component produced several top-level elements, `element` is a transparent FragmentElement
        // that stays in the tree (and is skipped by layout); the callback lives on it as usual.
        var childDispose = element.DisposeCallback;
        element.DisposeCallback = () =>
        {
            childDispose?.Invoke();
            component.DisposeInternal();
        };
        if (_stack.Count > 0)
            _stack.Peek().AddChild(element);
        else
            AttachToTree(element);
    }

    public void SetKey(object? key) { }

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

        // 出现第二个及以上顶层元素：用一个透明片段平铺承载全部顶层元素（而非逐层嵌套，
        // 也不套不透明 div，避免破坏样式布局）。若已有根本身就是片段，则直接复用为承载容器。
        if (_syntheticRoot is null)
        {
            _syntheticRoot = _root as FragmentElement ?? new FragmentElement();
            if (!ReferenceEquals(_syntheticRoot, _root))
                _syntheticRoot.AddChild(_root);
            _root = _syntheticRoot;
        }
        _syntheticRoot.AddChild(element);
    }
}
