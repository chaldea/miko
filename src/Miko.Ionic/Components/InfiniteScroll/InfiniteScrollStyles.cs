using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>Styles for <c>ion-infinite-scroll</c> and <c>ion-infinite-scroll-content</c>.</summary>
internal static class InfiniteScrollStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        return new CssObject
        {
            [$".ion-infinite-scroll.{mode}"] = new()
            {
                Display = Display.None,
                Width = Length.Percent(100),
            },

            [$".ion-infinite-scroll.{mode}.infinite-scroll-enabled"] = new()
            {
                Display = Display.Block,
            },

            [$".ion-infinite-scroll-content.{mode}"] = new()
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Center,
                MinHeight = Length.Px(t.InfiniteScrollContentMinHeight),
                TextAlign = TextAlign.Center,
                UserSelect = UserSelect.None,
            },

            [$".ion-infinite-scroll-content.{mode} .infinite-loading"] = new()
            {
                Display = Display.None,
                Width = Length.Percent(100),
                MarginBottom = Length.Px(32),
            },

            [$".ion-infinite-scroll.{mode}.infinite-scroll-loading .infinite-loading"] = new()
            {
                Display = Display.Block,
            },

            [$".ion-infinite-scroll-content.{mode} .infinite-loading-text"] = new()
            {
                MarginTop = Length.Px(4),
                MarginRight = Length.Px(32),
                MarginLeft = Length.Px(32),
            },

            [$".ion-infinite-scroll-content.{mode} .infinite-loading-spinner-icon"] = new()
            {
                Width = Length.Px(24),
                Height = Length.Px(24),
            },
        };
    }
}
