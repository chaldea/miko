using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Components;

internal static class IconStyles
{
    internal static CssObject GenStyle(Theme t)
    {
        return new CssObject
        {
            [".bi"] = new()
            {
                Display = Display.InlineBlock,
                FontFamily = "bootstrap-icons",
                FontStyle = FontStyle.Normal,
                FontWeight = FontWeight.Normal,
                FontSize = Length.Px(16),
                LineHeight = Number(1)
            },
            [".icon-sm"] = new() { FontSize = Length.Px(16) },
            [".icon-md"] = new() { FontSize = Length.Px(24) },
            [".icon-lg"] = new() { FontSize = Length.Px(32) },
            [".icon-xl"] = new() { FontSize = Length.Px(48) },
            [".icon-primary"] = new() { Color = t.Primary },
            [".icon-secondary"] = new() { Color = t.Secondary },
            [".icon-success"] = new() { Color = t.Success },
            [".icon-danger"] = new() { Color = t.Danger },
            [".icon-warning"] = new() { Color = t.Warning },
            [".icon-info"] = new() { Color = t.Info },
            [".btn-icon"] = new() { FontSize = Length.Px(16), MarginRight = Length.Px(6) }
        };
    }
}
