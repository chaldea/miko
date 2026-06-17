using Miko.Common;
using Miko.Styling;

namespace MikoApp.AsyncDemo;

internal static class GlobalStyles
{
    public static StyleSheet Create()
    {
        var styleSheet = new StyleSheet();

        styleSheet.Add(new CssObject
        {
            [".app-root"] = new()
            {
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                Padding = new Padding(20),
            },

            [".search-box"] = new()
            {
                MarginBottom = Length.Px(20),
            },

            [".search-input"] = new()
            {
                Width = Length.Px(300),
                MarginRight = Length.Px(10),
            },

            [".loading"] = new()
            {
                Color = new Color(108, 117, 125),
                Padding = new Padding(20),
            },

            [".product-table"] = new()
            {
                Width = Length.Percent(100),
            },
        });

        return styleSheet;
    }
}
