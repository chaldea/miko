using Miko.Ionic.Components;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>
/// Builds the Ionic component stylesheet by aggregating each component's generated styles.
/// Mirrors <c>BootstrapStyleSheetFactory</c>.
/// <para>
/// The stylesheet carries BOTH the <c>md</c> and <c>ios</c> mode rule sets, each scoped by the
/// matching mode class that every component stamps onto its root element. This lets the active
/// mode switch at runtime (e.g. the simulator swapping the selected device's platform) by simply
/// changing the root class — no stylesheet rebuild is required.
/// </para>
/// </summary>
public static class IonicStyleSheetFactory
{
    /// <summary>
    /// Builds a stylesheet containing both Material Design and iOS rule sets, each from its own
    /// per-mode theme. Use this so the active mode can switch at runtime via the component mode
    /// class alone.
    /// </summary>
    public static StyleSheet CreateAllModes()
    {
        var sheet = new StyleSheet();
        AddMode(sheet, "md", IonicTheme.CreateMd());
        AddMode(sheet, "ios", IonicTheme.CreateIos());
        return sheet;
    }

    /// <summary>
    /// Builds a stylesheet for a single mode from the given theme. Both mode rule sets are still
    /// emitted when possible so a runtime switch keeps working, but the supplied theme's mode is
    /// authored from <paramref name="theme"/> (e.g. a customized theme). The other mode falls back
    /// to its default values.
    /// </summary>
    public static StyleSheet Create(IonicTheme theme)
    {
        var sheet = new StyleSheet();
        if (theme.Mode == IonicMode.Ios)
        {
            AddMode(sheet, "md", IonicTheme.CreateMd());
            AddMode(sheet, "ios", theme);
        }
        else
        {
            AddMode(sheet, "md", theme);
            AddMode(sheet, "ios", IonicTheme.CreateIos());
        }
        return sheet;
    }

    private static void AddMode(StyleSheet sheet, string mode, IonicTheme t)
    {
        sheet.Add(PageStyles.GenStyle(mode, t));
        sheet.Add(TabStyles.GenStyle(mode, t));
        sheet.Add(IconStyles.GenStyle(mode, t));
        sheet.Add(LabelStyles.GenStyle(mode, t));
        sheet.Add(MenuStyles.GenStyle(mode, t));
        sheet.Add(ListStyles.GenStyle(mode, t));
        sheet.Add(SegmentStyles.GenStyle(mode, t));
    }
}
