using Miko.Common;
using Miko.Styling;

namespace MikoApp2;

internal static class GlobalStyles
{
    public static StyleSheet Create()
    {
        var styleSheet = new StyleSheet();

        styleSheet.Add(new CssObject
        {
            [".demo-content"] = new()
            {
                Padding = new Padding(20),
            },
        });

        return styleSheet;
    }
}
