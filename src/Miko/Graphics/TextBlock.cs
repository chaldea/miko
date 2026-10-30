using Miko.Text;

namespace Miko.Graphics;

/// <summary>
/// 排好版的一行文本（ISSUE-162）：字体回退、双向重排与整形都已完成，与引擎绘制普通文本走同一条管线，
/// 因此混排的中日文、阿拉伯文等都能正确显示。由 <see cref="IGraphicsFactory.CreateText"/> 创建，
/// 交给 <see cref="IDrawingContext.DrawText"/> 绘制。
///
/// <para>不可变、可跨帧复用；底层整形结果由引擎缓存，无需释放。</para>
/// </summary>
public sealed class TextBlock
{
    internal TextBlock(string text, TextStyle style, ShapedText shaped, float ascent, float descent)
    {
        Text = text;
        Style = style;
        Shaped = shaped;
        Ascent = ascent;
        Descent = descent;
    }

    public string Text { get; }

    public TextStyle Style { get; }

    /// <summary>前进宽度（含字间距），即绘制后下一段文本的起点偏移。</summary>
    public float Width => Shaped.Width;

    /// <summary>基线以上的高度（正值），取首个字体的度量。</summary>
    public float Ascent { get; }

    /// <summary>基线以下的深度（正值）。</summary>
    public float Descent { get; }

    /// <summary><see cref="Ascent"/> + <see cref="Descent"/>。</summary>
    public float Height => Ascent + Descent;

    internal ShapedText Shaped { get; }
}
