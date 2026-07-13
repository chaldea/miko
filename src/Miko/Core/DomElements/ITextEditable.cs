namespace Miko.Core.DomElements;

/// <summary>
/// 可编辑文本控件的公共抽象：由 <see cref="InputElement"/>（单行文本类）与
/// <see cref="TextAreaElement"/>（多行）实现，供
/// <see cref="Miko.Platform.MikoInteractionController"/> 以统一方式处理键盘编辑与文本输入
/// （插入、退格、删除、光标移动），避免为每种控件重复实现相同的编辑逻辑。
/// </summary>
public interface ITextEditable
{
    /// <summary>当前文本值</summary>
    string? Value { get; set; }

    /// <summary>光标位置（字符索引）</summary>
    int CursorPosition { get; set; }

    /// <summary>是否接受编辑（如 InputElement 仅在文本/密码类型时可编辑）</summary>
    bool IsEditable { get; }

    /// <summary>是否为多行控件（多行控件的回车键应插入换行而非提交）</summary>
    bool IsMultiline { get; }

    /// <summary>在光标处插入文本</summary>
    void InsertText(string text);

    /// <summary>删除光标前一个字符，成功返回 true</summary>
    bool Backspace();

    /// <summary>删除光标后一个字符，成功返回 true</summary>
    bool Delete();

    /// <summary>将光标移动到文本末尾</summary>
    void MoveCursorToEnd();
}
