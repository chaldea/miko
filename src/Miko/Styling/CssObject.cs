namespace Miko.Styling;

/// <summary>
/// CSS 对象，支持嵌套选择器的样式声明
/// </summary>
public class CssObject : Style
{
    private readonly Dictionary<string, CssObject> _children = new();

    public CssObject this[string selector]
    {
        get => _children[selector];
        set => _children[selector] = value;
    }

    internal IReadOnlyDictionary<string, CssObject> Children => _children;
}
