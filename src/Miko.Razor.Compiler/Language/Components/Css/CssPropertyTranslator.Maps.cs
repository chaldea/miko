// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Components.Css;

/// <summary>
/// CSS keywords → members of Miko's style enums (<c>Miko.Common.Enums</c>). An entry is
/// <c>keyword:Member</c>, or <c>keyword:Member:as</c> when the keyword is approximated (reported).
/// Kept in step with the enums by <c>CssTranslatorEnumTests</c>.
/// </summary>
internal sealed partial class CssPropertyTranslator
{
    internal static readonly Dictionary<string, string> DisplayMap = Map(@"
        block:Block inline:Inline inline-block:InlineBlock flex:Flex none:None grid:Grid inline-flex:InlineFlex
        table:Table table-row:TableRow table-cell:TableCell
        flow-root:Block:block list-item:Block:block contents:Block:block inline-grid:InlineBlock:inline-block
        inline-table:Table:table -webkit-box:Flex:flex -webkit-flex:Flex:flex -ms-flexbox:Flex:flex");

    internal static readonly Dictionary<string, string> PositionMap = Map(@"
        static:Static relative:Relative absolute:Absolute fixed:Fixed sticky:Sticky -webkit-sticky:Sticky");

    internal static readonly Dictionary<string, string> FlexDirectionMap = Map(@"
        row:Row row-reverse:RowReverse column:Column column-reverse:ColumnReverse");

    internal static readonly Dictionary<string, string> FlexWrapMap = Map(@"
        nowrap:Nowrap wrap:Wrap wrap-reverse:WrapReverse");

    internal static readonly Dictionary<string, string> JustifyContentMap = Map(@"
        normal:Normal flex-start:FlexStart flex-end:FlexEnd center:Center space-between:SpaceBetween
        space-around:SpaceAround space-evenly:SpaceEvenly start:Start end:End
        left:Start:start right:End:end stretch:Normal:normal");

    internal static readonly Dictionary<string, string> AlignItemsMap = Map(@"
        normal:Normal flex-start:FlexStart flex-end:FlexEnd center:Center stretch:Stretch baseline:Baseline
        start:FlexStart end:FlexEnd self-start:FlexStart self-end:FlexEnd");

    internal static readonly Dictionary<string, string> AlignSelfMap = Map(@"
        auto:Auto normal:Auto flex-start:FlexStart flex-end:FlexEnd center:Center stretch:Stretch baseline:Baseline
        start:FlexStart end:FlexEnd self-start:FlexStart self-end:FlexEnd");

    internal static readonly Dictionary<string, string> AlignContentMap = Map(@"
        normal:Normal flex-start:FlexStart flex-end:FlexEnd center:Center space-between:SpaceBetween
        space-around:SpaceAround stretch:Stretch start:FlexStart end:FlexEnd
        space-evenly:SpaceAround:space-around baseline:FlexStart:start");

    internal static readonly Dictionary<string, string> JustifyItemsMap = Map(@"
        normal:Normal flex-start:FlexStart flex-end:FlexEnd center:Center stretch:Stretch
        start:FlexStart end:FlexEnd self-start:FlexStart self-end:FlexEnd left:FlexStart:start right:FlexEnd:end
        legacy:Normal:normal baseline:FlexStart:start");

    internal static readonly Dictionary<string, string> JustifySelfMap = Map(@"
        auto:Auto normal:Auto flex-start:FlexStart flex-end:FlexEnd center:Center stretch:Stretch
        start:FlexStart end:FlexEnd self-start:FlexStart self-end:FlexEnd left:FlexStart:start right:FlexEnd:end
        baseline:FlexStart:start");

    internal static readonly Dictionary<string, string> BoxSizingMap = Map(@"content-box:ContentBox border-box:BorderBox");

    internal static readonly Dictionary<string, string> BorderStyleMap = Map(@"
        none:None hidden:None solid:Solid dotted:Dotted dashed:Dashed double:Double
        groove:Solid:solid ridge:Solid:solid inset:Solid:solid outset:Solid:solid");

    internal static readonly Dictionary<string, string> FontStyleMap = Map(@"normal:Normal italic:Italic oblique:Oblique");

    internal static readonly Dictionary<string, string> TextAlignMap = Map(@"
        left:Left right:Right center:Center justify:Justify start:Start end:End
        -webkit-center:Center -moz-center:Center -webkit-left:Left -webkit-right:Right
        match-parent:Start:start justify-all:Justify:justify");

    internal static readonly Dictionary<string, string> TextDecorationMap = Map(@"
        none:None underline:Underline overline:Overline line-through:LineThrough");

    internal static readonly Dictionary<string, string> TextTransformMap = Map(@"
        none:None uppercase:Uppercase lowercase:Lowercase capitalize:Capitalize");

    internal static readonly Dictionary<string, string> WhiteSpaceMap = Map(@"
        normal:Normal nowrap:Nowrap pre:Pre pre-wrap:PreWrap pre-line:PreLine break-spaces:PreWrap:pre-wrap");

    internal static readonly Dictionary<string, string> WordBreakMap = Map(@"normal:Normal break-all:BreakAll keep-all:KeepAll");

    internal static readonly Dictionary<string, string> OverflowWrapMap = Map(@"normal:Normal break-word:BreakWord anywhere:Anywhere");

    internal static readonly Dictionary<string, string> TextOverflowMap = Map(@"clip:Clip ellipsis:Ellipsis");

    internal static readonly Dictionary<string, string> VisibilityMap = Map(@"visible:Visible hidden:Hidden collapse:Collapse");

    internal static readonly Dictionary<string, string> CursorMap = Map(@"
        auto:Default default:Default pointer:Pointer text:Text wait:Wait not-allowed:NotAllowed move:Move help:Help
        none:Default:default context-menu:Default:default progress:Wait:wait cell:Default:default
        crosshair:Default:default vertical-text:Text:text alias:Default:default copy:Default:default
        no-drop:NotAllowed:not-allowed grab:Move:move grabbing:Move:move all-scroll:Move:move
        col-resize:Move:move row-resize:Move:move n-resize:Move:move e-resize:Move:move s-resize:Move:move
        w-resize:Move:move ne-resize:Move:move nw-resize:Move:move se-resize:Move:move sw-resize:Move:move
        ew-resize:Move:move ns-resize:Move:move nesw-resize:Move:move nwse-resize:Move:move
        zoom-in:Default:default zoom-out:Default:default");

    internal static readonly Dictionary<string, string> PointerEventsMap = Map(@"
        auto:Auto none:None visiblepainted:Auto visiblefill:Auto visiblestroke:Auto visible:Auto
        painted:Auto fill:Auto stroke:Auto all:Auto");

    internal static readonly Dictionary<string, string> UserSelectMap = Map(@"
        auto:Auto none:None text:Text all:All contain:Auto:auto");

    internal static readonly Dictionary<string, string> VerticalAlignMap = Map(@"
        baseline:Baseline top:Top middle:Middle bottom:Bottom text-top:TextTop text-bottom:TextBottom
        sub:Baseline:baseline super:Baseline:baseline");

    internal static readonly Dictionary<string, string> OverflowMap = Map(@"
        visible:Visible hidden:Hidden scroll:Scroll auto:Auto clip:Hidden:hidden overlay:Auto:auto");

    internal static readonly Dictionary<string, string> DirectionMap = Map(@"ltr:Ltr rtl:Rtl");

    internal static readonly Dictionary<string, string> WritingModeMap = Map(@"
        horizontal-tb:HorizontalTb vertical-rl:VerticalRl vertical-lr:VerticalLr
        sideways-rl:VerticalRl:vertical-rl sideways-lr:VerticalLr:vertical-lr");

    internal static readonly Dictionary<string, string> TableLayoutMap = Map(@"auto:Auto fixed:Fixed");

    internal static readonly Dictionary<string, string> ScrollbarWidthMap = Map(@"auto:Auto thin:Thin none:None");

    internal static readonly Dictionary<string, string> TimingFunctionMap = Map(@"
        linear:Linear ease:Ease ease-in:EaseIn ease-out:EaseOut ease-in-out:EaseInOut step-start:StepStart step-end:StepEnd");

    internal static readonly Dictionary<string, string> AnimationDirectionMap = Map(@"
        normal:Normal reverse:Reverse alternate:Alternate alternate-reverse:AlternateReverse");

    internal static readonly Dictionary<string, string> FillModeMap = Map(@"none:None forwards:Forwards backwards:Backwards both:Both");

    internal static readonly Dictionary<string, string> PlayStateMap = Map(@"running:Running paused:Paused");

    /// <summary>The C# enum type each map's members belong to, for the enum consistency test.</summary>
    internal static IEnumerable<(string EnumType, Dictionary<string, string> Map)> EnumMaps()
    {
        yield return ("Miko.Common.Display", DisplayMap);
        yield return ("Miko.Common.Position", PositionMap);
        yield return ("Miko.Common.FlexDirection", FlexDirectionMap);
        yield return ("Miko.Common.FlexWrap", FlexWrapMap);
        yield return ("Miko.Common.JustifyContent", JustifyContentMap);
        yield return ("Miko.Common.AlignItems", AlignItemsMap);
        yield return ("Miko.Common.AlignSelf", AlignSelfMap);
        yield return ("Miko.Common.AlignContent", AlignContentMap);
        yield return ("Miko.Common.JustifyItems", JustifyItemsMap);
        yield return ("Miko.Common.JustifySelf", JustifySelfMap);
        yield return ("Miko.Common.BoxSizing", BoxSizingMap);
        yield return ("Miko.Common.BorderStyle", BorderStyleMap);
        yield return ("Miko.Common.FontStyle", FontStyleMap);
        yield return ("Miko.Common.TextAlign", TextAlignMap);
        yield return ("Miko.Common.TextDecoration", TextDecorationMap);
        yield return ("Miko.Common.TextTransform", TextTransformMap);
        yield return ("Miko.Common.WhiteSpace", WhiteSpaceMap);
        yield return ("Miko.Common.WordBreak", WordBreakMap);
        yield return ("Miko.Common.OverflowWrap", OverflowWrapMap);
        yield return ("Miko.Common.TextOverflow", TextOverflowMap);
        yield return ("Miko.Common.Visibility", VisibilityMap);
        yield return ("Miko.Common.Cursor", CursorMap);
        yield return ("Miko.Common.PointerEvents", PointerEventsMap);
        yield return ("Miko.Common.UserSelect", UserSelectMap);
        yield return ("Miko.Common.VerticalAlign", VerticalAlignMap);
        yield return ("Miko.Common.Overflow", OverflowMap);
        yield return ("Miko.Common.Direction", DirectionMap);
        yield return ("Miko.Common.WritingMode", WritingModeMap);
        yield return ("Miko.Common.TableLayoutAlgorithm", TableLayoutMap);
        yield return ("Miko.Common.ScrollbarWidth", ScrollbarWidthMap);
        yield return ("Miko.Animation.TimingFunction", TimingFunctionMap);
        yield return ("Miko.Animation.AnimationDirection", AnimationDirectionMap);
        yield return ("Miko.Animation.AnimationFillMode", FillModeMap);
        yield return ("Miko.Animation.AnimationPlayState", PlayStateMap);
    }
}
