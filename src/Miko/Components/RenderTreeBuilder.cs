using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using System.Diagnostics.CodeAnalysis;
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
        ["select"] = () => new SelectElement(),
        ["option"] = () => new OptionElement(),
        ["optgroup"] = () => new OptGroupElement(),
        ["label"] = () => new LabelElement(),
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
        ["thead"] = () => new TheadElement(),
        ["tbody"] = () => new TbodyElement(),
        ["tfoot"] = () => new TfootElement(),
        ["tr"] = () => new TrElement(),
        ["th"] = () => new ThElement(),
        ["td"] = () => new TdElement(),
        ["nav"] = () => new NavElement(),
        ["strong"] = () => new StrongElement(),
    };

    public void OpenElement(int seq, string tagName)
    {
        if (!_tagMap.TryGetValue(tagName, out var factory))
            throw new InvalidOperationException($"Unknown tag: {tagName}");
        _stack.Push(factory());
    }

    public void CloseElement()
    {
        var element = _stack.Pop();
        if (_stack.Count > 0)
            _stack.Peek().AddChild(element);
        else
            _root = element;
    }

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
                    _ => InputType.Text,
                };
                break;
            case "src" when element is ImageElement img:
                img.Source = value; break;
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

    public void AddAttribute(int seq, string name, bool value) { }

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

    public void AddContent(int seq, object? text)
    {
        if (_stack.Count == 0 || text is null) return;
        if (text is RenderFragment fragment)
        {
            fragment(this);
            return;
        }
        var str = text.ToString();
        if (string.IsNullOrEmpty(str)) return;
        var decoded = WebUtility.HtmlDecode(str);
        var element = _stack.Peek();
        // Razor emits one AddContent call per content fragment (literal text and
        // expressions). Append so consecutive fragments concatenate instead of
        // overwriting each other (e.g. "Clicked " + _count + " times").
        element.TextContent = element.TextContent is null ? decoded : element.TextContent + decoded;
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
        // (e.g. unsubscribe from events) when this element subtree is later discarded.
        element.DisposeCallback = component.DisposeInternal;
        if (_stack.Count > 0)
            _stack.Peek().AddChild(element);
        else if (_componentStack.Count > 0)
        {
            // nested component scenario — shouldn't happen normally
            AttachToTree(element);
        }
        else
            AttachToTree(element);
    }

    public void SetKey(object? key) { }

    public void AttachElement(Element element)
    {
        if (_stack.Count > 0)
            _stack.Peek().AddChild(element);
        else
            _root = element;
    }

    public Element Build()
    {
        if (_stack.Count > 0)
            throw new InvalidOperationException("Unclosed elements remain in the render tree.");
        if (_root is null)
            throw new InvalidOperationException("No elements were added to the render tree.");
        return _root;
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
                var text = m.Groups["text"].Value.Trim();
                if (text.Length > 0 && localStack.Count > 0)
                    localStack.Peek().TextContent = text;
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
            if (localStack.Count > 0)
                localStack.Peek().AddChild(el);
            else
                AttachToTree(el);
        }
    }

    private void AttachToTree(Element element)
    {
        if (_stack.Count > 0)
            _stack.Peek().AddChild(element);
        else if (_root is null)
            _root = element;
        else
        {
            // Multiple root elements: wrap in a div
            var wrapper = new DivElement();
            wrapper.AddChild(_root);
            wrapper.AddChild(element);
            _root = wrapper;
        }
    }
}
