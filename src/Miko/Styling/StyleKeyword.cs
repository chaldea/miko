namespace Miko.Styling;

/// <summary>
/// CSS 全局关键词（CSS-wide keywords）。可赋给任意 <see cref="StyleProperty{T}"/>，
/// 在样式计算阶段按语义解析：
/// <list type="bullet">
/// <item><see cref="Initial"/>：重置为该属性的初始（默认）值。</item>
/// <item><see cref="Inherit"/>：取父元素该属性的计算值（对非继承属性同样生效）。</item>
/// <item><see cref="Unset"/>：可继承属性等价于 <see cref="Inherit"/>，否则等价于 <see cref="Initial"/>。</item>
/// <item><see cref="Revert"/>：回退到用户代理（UA）层。Miko 在 <c>Style.Merge</c> 后不保留
/// UA/作者分层，故等价于 <see cref="Unset"/>。</item>
/// <item><see cref="RevertLayer"/>：回退到上一级联层。Miko 无级联层，按规范在无上一层时回退为
/// <see cref="Unset"/>。</item>
/// </list>
/// </summary>
public enum StyleKeyword
{
    Initial,
    Inherit,
    Unset,
    Revert,
    RevertLayer,
}
