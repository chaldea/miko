namespace Miko.Core.DomElements;

/// <summary>
/// 表格元素 (table)
/// </summary>
public class TableElement : Element
{
    public override string TagName => "table";
}

/// <summary>
/// 表格标题元素 (caption)
/// </summary>
public class CaptionElement : Element
{
    public override string TagName => "caption";
}

/// <summary>
/// 表格列组元素 (colgroup)
/// </summary>
public class ColgroupElement : Element
{
    public override string TagName => "colgroup";

    /// <summary>
    /// 跨越的列数
    /// </summary>
    public int Span { get; set; } = 1;
}

/// <summary>
/// 表格列元素 (col)
/// </summary>
public class ColElement : Element
{
    public override string TagName => "col";

    /// <summary>
    /// 跨越的列数
    /// </summary>
    public int Span { get; set; } = 1;
}

/// <summary>
/// 表格头部元素 (thead)
/// </summary>
public class TheadElement : Element
{
    public override string TagName => "thead";
}

/// <summary>
/// 表格主体元素 (tbody)
/// </summary>
public class TbodyElement : Element
{
    public override string TagName => "tbody";
}

/// <summary>
/// 表格底部元素 (tfoot)
/// </summary>
public class TfootElement : Element
{
    public override string TagName => "tfoot";
}

/// <summary>
/// 表格行元素 (tr)
/// </summary>
public class TrElement : Element
{
    public override string TagName => "tr";
}

/// <summary>
/// 表格表头单元格元素 (th)
/// </summary>
public class ThElement : Element
{
    public override string TagName => "th";

    /// <summary>
    /// 跨越的列数
    /// </summary>
    public int ColSpan { get; set; } = 1;

    /// <summary>
    /// 跨越的行数
    /// </summary>
    public int RowSpan { get; set; } = 1;

    /// <summary>
    /// 关联的表头单元格 ID（用于无障碍）
    /// </summary>
    public string? Headers { get; set; }

    /// <summary>
    /// 表头作用域
    /// </summary>
    public TableHeaderScope Scope { get; set; } = TableHeaderScope.Auto;
}

/// <summary>
/// 表格数据单元格元素 (td)
/// </summary>
public class TdElement : Element
{
    public override string TagName => "td";

    /// <summary>
    /// 跨越的列数
    /// </summary>
    public int ColSpan { get; set; } = 1;

    /// <summary>
    /// 跨越的行数
    /// </summary>
    public int RowSpan { get; set; } = 1;

    /// <summary>
    /// 关联的表头单元格 ID（用于无障碍）
    /// </summary>
    public string? Headers { get; set; }
}

/// <summary>
/// 表头作用域
/// </summary>
public enum TableHeaderScope
{
    Auto,
    Row,
    Col,
    RowGroup,
    ColGroup
}
