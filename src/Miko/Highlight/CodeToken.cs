namespace Miko.Highlight;

/// <summary>
/// 语法高亮的 token 类型。
/// </summary>
public enum CodeTokenType
{
    /// <summary>普通文本（使用元素自身的颜色绘制）。</summary>
    Plain,
    /// <summary>语言关键字（如 class、return、SELECT）。</summary>
    Keyword,
    /// <summary>字符串 / 字符字面量。</summary>
    String,
    /// <summary>注释（单行与块注释）。</summary>
    Comment,
    /// <summary>数字字面量（含十六进制、单位后缀等）。</summary>
    Number,
    /// <summary>类型名（C# 等语言中按 PascalCase 启发式识别）。</summary>
    Type,
    /// <summary>函数 / 方法调用名（标识符后紧跟左括号）。</summary>
    Function,
}

/// <summary>
/// 一个高亮 token：文本区间 <see cref="Start"/>..<see cref="Start"/>+<see cref="Length"/>
/// 及其类型。token 序列按起点升序、互不重叠；未覆盖的区间视为 <see cref="CodeTokenType.Plain"/>。
/// token 可能跨行（块注释、多行字符串），绘制方负责在行边界裁剪。
/// </summary>
public readonly record struct CodeToken(CodeTokenType Type, int Start, int Length);
