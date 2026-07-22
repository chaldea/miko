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
        ["pre"] = () => new PreElement(),
        ["code"] = () => new CodeElement(),
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
                    _ => InputType.Text,
                };
                break;
            case "src" when element is ImageElement img:
                img.Source = value; break;
            case "placeholder" when element is ImageElement placeholderImg:
                placeholderImg.Placeholder = value; break;
            case "placeholder" when element is TextAreaElement textArea:
                textArea.Placeholder = value; break;
            case "value" when element is TextAreaElement valueTextArea:
                valueTextArea.Value = value; break;
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

    public void AddAttribute(int seq, string name, bool value) { }

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
        if (text is null) return;
        // A RenderFragment may emit top-level elements (e.g. a transparent CascadingValue whose
        // ChildContent is rendered with no open element on the stack), so invoke it regardless of
        // stack depth. Only literal/text content requires an open element to attach to.
        if (text is RenderFragment fragment)
        {
            fragment(this);
            return;
        }
        if (_stack.Count == 0) return;
        var str = text.ToString();
        if (string.IsNullOrEmpty(str)) return;
        var decoded = WebUtility.HtmlDecode(str);
        var element = _stack.Peek();
        // 文本以有序 TextNode 子节点形式追加，保留与已打开的子元素的交错顺序（见 ISSUE-086）。
        // Razor 会为每段内容（字面文本与表达式）发射一次 AddContent。相邻的纯文本片段合并到
        // 同一末尾 TextNode（如 "Clicked " + _count + " times" 拼接为一段），但被子元素分隔的
        // 文本会形成各自独立的 TextNode，从而正确表达 text1 <span/> text3。
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
        element.DisposeCallback = component.DisposeInternal;
        if (_stack.Count > 0)
            _stack.Peek().AddChild(element);
        else
            AttachToTree(element);
    }

    public void SetKey(object? key) { }

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
