using Miko.Common;
using Miko.Styling;

namespace MikoRazorApp;

internal static class GlobalStyles
{
    public static StyleSheet Create()
    {
        var styleSheet = new StyleSheet();

        styleSheet.Add(new CssObject
        {
            // Full-viewport host for the routed page so the IonApp inside (height:100%)
            // resolves against a definite height.
            [".app-root"] = new()
            {
                Width = Length.Percent(100),
                Height = Length.Percent(100),
            },

            [".demo-content"] = new()
            {
                Padding = new Padding(20),
            },
        });

        return styleSheet;
    }
}
