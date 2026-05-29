using Miko.Common;
using Miko.Core.DomElements;
using Miko.Styling;

namespace Miko.DevTools.Panels;

internal static class ElementsPanel
{
    public static DivElement Build(DevToolsBridge bridge, bool visible)
    {
        var panel = new DivElement { Class = "elements-panel" };
        if (!visible)
        {
            panel.Style = new Style { Display = Display.None };
        }

        var treePanel = DomTreeBuilder.Build(bridge);
        var stylePanel = StyleInspector.Build(bridge.SelectedElement, bridge.MainEngine);

        panel.AddChild(treePanel);
        panel.AddChild(stylePanel);

        return panel;
    }
}
