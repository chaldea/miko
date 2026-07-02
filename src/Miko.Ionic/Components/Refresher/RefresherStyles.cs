using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>Styles for <c>ion-refresher</c> and <c>ion-refresher-content</c>.</summary>
internal static class RefresherStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        return new CssObject
        {
            [$".ion-refresher.{mode}"] = new()
            {
                Display = Display.None,
                Position = Position.Absolute,
                Top = Length.Px(0),
                Left = Length.Px(0),
                Width = Length.Percent(100),
                Height = Length.Px(t.RefresherHeight),
                PointerEvents = PointerEvents.None,
                ZIndex = 10,
            },

            [$".ion-refresher.{mode}.refresher-active"] = new()
            {
                Display = Display.Block,
            },

            [$".ion-refresher-content.{mode}"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Center,
                Height = Length.Percent(100),
            },

            [$".ion-refresher-content.{mode} .refresher-pulling"] = new()
            {
                Display = Display.None,
                Width = Length.Percent(100),
            },

            [$".ion-refresher-content.{mode} .refresher-refreshing"] = new()
            {
                Display = Display.None,
                Width = Length.Percent(100),
            },

            [$".ion-refresher.{mode}.refresher-pulling .refresher-pulling"] = new()
            {
                Display = Display.Block,
            },

            [$".ion-refresher.{mode}.refresher-ready .refresher-pulling"] = new()
            {
                Display = Display.Block,
            },

            [$".ion-refresher.{mode}.refresher-refreshing .refresher-refreshing"] = new()
            {
                Display = Display.Block,
            },

            [$".ion-refresher.{mode}.refresher-cancelling .refresher-pulling"] = new()
            {
                Display = Display.Block,
            },

            [$".ion-refresher.{mode}.refresher-completing .refresher-refreshing"] = new()
            {
                Display = Display.Block,
            },

            [$".ion-refresher-content.{mode} .refresher-pulling-icon"] = new()
            {
                FontSize = Length.Px(t.RefresherIconFontSize),
                TextAlign = TextAlign.Center,
            },

            [$".ion-refresher-content.{mode} .refresher-refreshing-icon"] = new()
            {
                FontSize = Length.Px(t.RefresherIconFontSize),
                TextAlign = TextAlign.Center,
            },

            [$".ion-refresher-content.{mode} .refresher-pulling-text"] = new()
            {
                FontSize = Length.Px(t.RefresherTextFontSize),
                TextAlign = TextAlign.Center,
            },

            [$".ion-refresher-content.{mode} .refresher-refreshing-text"] = new()
            {
                FontSize = Length.Px(t.RefresherTextFontSize),
                TextAlign = TextAlign.Center,
            },
        };
    }
}
