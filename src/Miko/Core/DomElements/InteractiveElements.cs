using Miko.Common;

namespace Miko.Core.DomElements;

/// <summary>
/// 锚点（超链接）元素
/// </summary>
public class AnchorElement : Element
{
    public override string TagName => "a";

    /// <summary>
    /// 链接目标 URL
    /// </summary>
    public string? Href { get; set; }

    /// <summary>
    /// 链接打开方式（_self, _blank, _parent, _top）
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// 链接与目标之间的关系（如 noopener, noreferrer）
    /// </summary>
    public string? Rel { get; set; }
}

/// <summary>
/// 按钮元素
/// </summary>
public class ButtonElement : Element
{
    public override string TagName => "button";
}

/// <summary>
/// 输入框元素
/// </summary>
public class InputElement : Element
{
    public override string TagName => "input";

    /// <summary>
    /// 输入框类型，默认为 Text
    /// </summary>
    public InputType Type { get; set; } = InputType.Text;

    /// <summary>
    /// 输入框的值
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// 占位符文本
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// 是否选中（用于 Checkbox 和 Radio）
    /// </summary>
    public bool Checked { get; set; }

    /// <summary>
    /// 最小值（用于 Range）
    /// </summary>
    public float Min { get; set; } = 0;

    /// <summary>
    /// 最大值（用于 Range）
    /// </summary>
    public float Max { get; set; } = 100;

    /// <summary>
    /// 当前数值（用于 Range）
    /// </summary>
    public float NumericValue { get; set; } = 50;

    /// <summary>
    /// 光标位置（字符索引）
    /// </summary>
    public int CursorPosition { get; set; }

    /// <summary>
    /// 插入文本到当前光标位置
    /// </summary>
    public void InsertText(string text)
    {
        var current = Value ?? string.Empty;
        var pos = Math.Clamp(CursorPosition, 0, current.Length);
        Value = current.Insert(pos, text);
        CursorPosition = pos + text.Length;
        IsDirty = true;
    }

    /// <summary>
    /// 删除光标前一个字符
    /// </summary>
    public bool Backspace()
    {
        var current = Value ?? string.Empty;
        if (CursorPosition > 0 && current.Length > 0)
        {
            var pos = Math.Clamp(CursorPosition, 0, current.Length);
            Value = current.Remove(pos - 1, 1);
            CursorPosition = pos - 1;
            IsDirty = true;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 删除光标后一个字符
    /// </summary>
    public bool Delete()
    {
        var current = Value ?? string.Empty;
        if (CursorPosition < current.Length)
        {
            Value = current.Remove(CursorPosition, 1);
            IsDirty = true;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 将光标移动到文本末尾
    /// </summary>
    public void MoveCursorToEnd()
    {
        CursorPosition = (Value ?? string.Empty).Length;
    }
}

/// <summary>
/// 下拉选择框元素
/// </summary>
public class SelectElement : Element
{
    public override string TagName => "select";

    private int _selectedIndex = -1;
    private bool _isOpen;

    /// <summary>
    /// 当前选中的选项索引
    /// </summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (_selectedIndex != value)
            {
                _selectedIndex = value;
                IsDirty = true;
            }
        }
    }

    /// <summary>
    /// 当前选中的值
    /// </summary>
    public string? Value
    {
        get
        {
            var option = GetSelectedOption();
            return option?.Value ?? option?.TextContent;
        }
        set
        {
            var options = GetAllOptions();
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].Value == value || (options[i].Value == null && options[i].TextContent == value))
                {
                    SelectedIndex = i;
                    return;
                }
            }
            SelectedIndex = -1;
        }
    }

    /// <summary>
    /// 是否允许多选
    /// </summary>
    public bool Multiple { get; set; }

    /// <summary>
    /// 可见选项数量（用于多选模式）
    /// </summary>
    public int Size { get; set; } = 1;

    /// <summary>
    /// 下拉框是否展开
    /// </summary>
    public bool IsOpen
    {
        get => _isOpen;
        set
        {
            if (_isOpen != value)
            {
                _isOpen = value;
                IsDirty = true;
            }
        }
    }

    /// <summary>
    /// 切换下拉框展开状态
    /// </summary>
    public void Toggle()
    {
        if (!IsDisabled)
        {
            IsOpen = !IsOpen;
        }
    }

    /// <summary>
    /// 选择指定索引的选项并关闭下拉框
    /// </summary>
    /// <param name="index">选项索引</param>
    /// <returns>如果选择成功且值发生变化返回 true</returns>
    public bool SelectOption(int index)
    {
        var options = GetAllOptions();
        if (index >= 0 && index < options.Count && !options[index].IsDisabled)
        {
            var oldValue = Value;
            SelectedIndex = index;
            IsOpen = false;

            // Return true if value changed (for OnChange event)
            return oldValue != Value;
        }
        return false;
    }

    /// <summary>
    /// 关闭下拉框
    /// </summary>
    public void Close()
    {
        IsOpen = false;
    }

    /// <summary>
    /// 处理失去焦点事件的默认行为
    /// </summary>
    public void HandleBlur()
    {
        Close();
    }

    /// <summary>
    /// 获取所有选项（包括optgroup中的选项）
    /// </summary>
    public List<OptionElement> GetAllOptions()
    {
        var options = new List<OptionElement>();
        CollectOptions(this, options);
        return options;
    }

    /// <summary>
    /// 获取当前选中的选项
    /// </summary>
    public OptionElement? GetSelectedOption()
    {
        var options = GetAllOptions();
        if (SelectedIndex >= 0 && SelectedIndex < options.Count)
        {
            return options[SelectedIndex];
        }
        return options.FirstOrDefault(o => o.Selected);
    }

    /// <summary>
    /// 获取显示文本
    /// </summary>
    public string GetDisplayText()
    {
        var option = GetSelectedOption();
        return option?.TextContent ?? string.Empty;
    }

    private void CollectOptions(Element parent, List<OptionElement> options)
    {
        foreach (var child in parent.Children)
        {
            if (child is OptionElement option)
            {
                options.Add(option);
            }
            else if (child is OptGroupElement)
            {
                CollectOptions(child, options);
            }
        }
    }
}

/// <summary>
/// 选项分组元素
/// </summary>
public class OptGroupElement : Element
{
    public override string TagName => "optgroup";

    /// <summary>
    /// 分组标签
    /// </summary>
    public string? Label { get; set; }
}

/// <summary>
/// 选项元素
/// </summary>
public class OptionElement : Element
{
    public override string TagName => "option";

    /// <summary>
    /// 选项值（如果未设置，则使用 TextContent）
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// 是否选中
    /// </summary>
    public bool Selected { get; set; }
}

/// <summary>
/// 标签元素，用于关联表单控件
/// </summary>
public class LabelElement : Element
{
    public override string TagName => "label";

    /// <summary>
    /// 关联的表单控件 ID
    /// </summary>
    public string? For { get; set; }
}
