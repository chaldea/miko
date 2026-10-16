using Miko.Animation;
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
                // Not in refresher.scss. Ionic reveals the refresher by painting it underneath the
                // content (z-index: -1); this port paints it on top and slides its content down
                // with the pull (IonRefresher.ApplyOffset), so the clip keeps the part not yet
                // uncovered from drawing over the content.
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
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

            // refresher.scss: `transition: 200ms` on both icons, which the state rules below turn
            // into the arrow flipping when a release would refresh and the icons shrinking away on
            // cancel / complete.
            [$".ion-refresher-content.{mode} .refresher-pulling-icon"] = new()
            {
                FontSize = Length.Px(t.RefresherIconFontSize),
                TextAlign = TextAlign.Center,
                Transitions = new List<Transition> { Transition.For(x => x.Transform).Duration(0.2f) },
            },

            [$".ion-refresher-content.{mode} .refresher-refreshing-icon"] = new()
            {
                FontSize = Length.Px(t.RefresherIconFontSize),
                TextAlign = TextAlign.Center,
                Transitions = new List<Transition> { Transition.For(x => x.Transform).Duration(0.2f) },
            },

            [$".ion-refresher.{mode}.refresher-ready .refresher-pulling-icon"] = new()
            {
                Transform = Transform.FromRotate(180),
            },

            [$".ion-refresher.{mode}.refresher-cancelling .refresher-pulling-icon"] = new()
            {
                Transform = Transform.FromScale(0, 0),
            },

            [$".ion-refresher.{mode}.refresher-completing .refresher-refreshing-icon"] = new()
            {
                Transform = Transform.FromScale(0, 0),
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
