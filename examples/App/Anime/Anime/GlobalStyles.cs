// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Miko.Common;
using Miko.Styling;

namespace Anime;

internal static class GlobalStyles
{
    public static StyleSheet Create()
    {
        var styleSheet = new StyleSheet();

        styleSheet.Add(new CssObject
        {
            [".ion-card .card-native"] = new()
            {
                TextAlign = TextAlign.Left,
                BackgroundColor = Color.Transparent,
                Color = Color.FromHex("#292b30")
            },
            [".library-item .ion-slot-start"] = new()
            {
                MarginRight = Length.Px(12)
            },
            [".anime-app"] = new()
            {
                FontFamily = "Microsoft YaHei",
                FontSize = Length.Px(13),
                Color = Color.FromHex("#292b30"),
                BackgroundColor = Color.White
            },
            [".page-padding"] = new()
            {
                Padding = new Padding(12, 10, 20)
            },
            [".ion-header"] = new()
            {
                BoxShadow = new List<BoxShadow>()
            },
            [".home-toolbar .toolbar-container"] = new()
            {
                Padding = new Padding(3, 0)
            },
            [".search-avatar"] = new()
            {
                Width = Length.Px(32),
                Height = Length.Px(32)
            },
            [".home-search"] = new()
            {
                Padding = 0,
                MinWidth = Length.Px(0)
            },
            [".home-segment"] = new()
            {
                JustifyContent = JustifyContent.FlexStart
            },
            [".ion-segment-button.ios.segment-button-checked .button-native"] = new()
            {
                Color = Color.White
            },
            [".home-segment .ion-segment-button"] = new()
            {
                FlexGrow = 0,
                Width = Length.Px(76)
            },
            [".poster-grid, .quick-links, .reward-grid"] = new()
            {
                Padding = 0
            },
            [".poster-grid .ion-col"] = new()
            {
                Padding = new Padding(0, 5, 12)
            },
            [".poster-card"] = new()
            {
                Margin = 0,
                BorderRadius = 0,
                BoxShadow = new List<BoxShadow>(),
                Color = Color.FromHex("#292b30")
            },
            [".poster-image"] = new()
            {
                Width = Length.Percent(100),
                Height = Length.Px(154),
                BorderRadius = 6
            },
            [".poster-title"] = new()
            {
                FontSize = Length.Px(12),
                LineHeight = Length.Px(19),
                WhiteSpace = WhiteSpace.Nowrap,
                TextOverflow = TextOverflow.Ellipsis,
                Overflow = Overflow.Hidden,
                MarginTop = Length.Px(3)
            },
            [".poster-note"] = new()
            {
                Display = Display.Block,
                FontSize = Length.Px(10),
                WhiteSpace = WhiteSpace.Nowrap,
                Overflow = Overflow.Hidden,
                Color = Color.FromHex("#999ba0")
            },
            [".banner-card"] = new()
            {
                Margin = 0,
                BoxShadow = new List<BoxShadow>(),
                BorderRadius = 6
            },
            [".banner-image"] = new()
            {
                Width = Length.Percent(100),
                Height = Length.Px(180)
            },
            [".quick-links .ion-button"] = new()
            {
                BackgroundColor = Color.FromHex("#f7f7f8"),
                Color = Color.FromHex("#51535a"),
                Height = Length.Px(35),
                FontSize = Length.Px(12),
                Margin = 0
            },
            [".section-title"] = new()
            {
                FontSize = Length.Px(14),
                FontWeight = FontWeight.Bold,
                Margin = new Margin(12, 4, 10)
            },
            [".filter-row"] = new()
            {
                Display = Display.Flex,
                OverflowX = Overflow.Auto,
                Gap = Length.Px(3),
                MarginBottom = Length.Px(6)
            },
            [".filter-button"] = new()
            {
                FlexShrink = 0,
                FontSize = Length.Px(11),
                Height = Length.Px(30),
                Margin = 0
            },
            [".filter-button.button-solid"] = new()
            {
                Color = Color.FromHex("#00afd9"),
                BackgroundColor = Color.FromHex("#ddf5fc")
            },
            [".filter-button.button-solid .button-native"] = new()
            {
                BackgroundColor = Color.FromHex("#ddf5fc"),
                Color = Color.FromHex("#00afd9"),
                BoxShadow = new List<BoxShadow>()
            },
            [".empty-state"] = new()
            {
                Padding = new Padding(40, 24),
                TextAlign = TextAlign.Center,
                Color = Color.FromHex("#94979e"),
                LineHeight = Length.Px(24)
            },
            [".ranking-card"] = new()
            {
                Margin = new Margin(0, 0, 10),
                BoxShadow = new List<BoxShadow>(),
                BackgroundColor = Color.FromHex("#fafafa"),
                BorderRadius = 6
            },
            [".ranking-row"] = new()
            {
                Display = Display.Flex,
                Padding = 8,
                Gap = Length.Px(10)
            },
            [".ranking-image"] = new()
            {
                Width = Length.Px(96),
                Height = Length.Px(132),
                FlexShrink = 0,
                BorderRadius = 5
            },
            [".ranking-copy, .comment-copy"] = new()
            {
                FlexGrow = 1,
                MinWidth = Length.Px(0)
            },
            [".row-title"] = new()
            {
                FontSize = Length.Px(13),
                LineHeight = Length.Px(21),
                Color = Color.FromHex("#292b30"),
                WhiteSpace = WhiteSpace.Nowrap,
                TextOverflow = TextOverflow.Ellipsis,
                Overflow = Overflow.Hidden
            },
            [".description"] = new()
            {
                FontSize = Length.Px(11),
                LineHeight = Length.Px(18),
                Color = Color.FromHex("#a0a2a6"),
                Margin = new Margin(8, 0),
                MaxHeight = Length.Px(55),
                Overflow = Overflow.Hidden
            },
            [".ion-note"] = new()
            {
                FontSize = Length.Px(11),
                Color = Color.FromHex("#999ba0")
            },
            [".week-segment .ion-segment-button"] = new()
            {
                MinWidth = Length.Px(0),
                PaddingLeft = Length.Px(0),
                PaddingRight = Length.Px(0)
            },
            [".soft-content, .soft-content .background-content"] = new()
            {
                BackgroundColor = Color.FromHex("#fafafa")
            },
            [".user-page"] = new()
            {
                PaddingTop = Length.Px(24)
            },
            [".profile-summary"] = new()
            {
                MarginBottom = Length.Px(16)
            },
            [".support-card"] = new()
            {
                Margin = new Margin(12, 0),
                BackgroundColor = Color.FromHex("#e1f5fe"),
                Color = Color.FromHex("#199bd0"),
                BoxShadow = new List<BoxShadow>(),
                BorderRadius = 6
            },
            [".support-card .ion-card-content"] = new()
            {
                Padding = 12,
                FontSize = Length.Px(12)
            },
            [".flat-card, .coins-card, .task-card, .reward-card, .notice-card"] = new()
            {
                Margin = 0,
                BoxShadow = new List<BoxShadow>(),
                BorderRadius = 10
            },
            [".flat-card"] = new()
            {
                MarginBottom = Length.Px(16)
            },
            [".user-shortcut"] = new()
            {
                Height = Length.Px(64),
                Margin = 0,
                Color = Color.FromHex("#43464b"),
                FontSize = Length.Px(11)
            },
            [".user-shortcut .button-inner"] = new()
            {
                FlexDirection = FlexDirection.Column,
                Gap = Length.Px(8)
            },
            [".user-shortcut .ion-icon"] = new()
            {
                Width = Length.Px(23),
                Height = Length.Px(23)
            },
            [".user-menu"] = new()
            {
                BorderRadius = 8
            },
            [".save-profile"] = new()
            {
                MarginTop = Length.Px(20)
            },
            [".library-item"] = new()
            {
                MarginBottom = Length.Px(9)
            },
            [".library-image"] = new()
            {
                Width = Length.Px(130),
                Height = Length.Px(78),
                BorderRadius = 5
            },
            [".library-genre"] = new()
            {
                FontSize = Length.Px(11),
                Color = Color.FromHex("#999ba0"),
                MarginTop = Length.Px(12)
            },
            [".section-heading"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.SpaceBetween
            },
            [".coins-card"] = new()
            {
                BackgroundColor = Color.FromHex("#22b7e1"),
                Color = Color.White
            },
            [".coins-card .ion-card-content"] = new()
            {
                Padding = 16
            },
            [".coins-card p"] = new()
            {
                FontSize = Length.Px(12),
                Margin = new Margin(14, 0, 0)
            },
            [".coin-count"] = new()
            {
                FontSize = Length.Px(28)
            },
            [".task-card"] = new()
            {
                MarginBottom = Length.Px(12),
                BackgroundColor = Color.FromHex("#f7f7f8")
            },
            [".task-card .item-native"] = new()
            {
                BackgroundColor = Color.FromHex("#f7f7f8"),
                MinHeight = Length.Px(70)
            },
            [".task-card .ion-slot-end"] = new()
            {
                FlexShrink = 0
            },
            [".task-card .ion-button"] = new()
            {
                MinWidth = Length.Px(88),
                WhiteSpace = WhiteSpace.Nowrap
            },
            [".continue-watching .ion-label"] = new()
            {
                WhiteSpace = WhiteSpace.Nowrap,
                Overflow = Overflow.Hidden,
                TextOverflow = TextOverflow.Ellipsis
            },
            [".continue-watching .ion-slot-end"] = new()
            {
                FlexShrink = 0,
                WhiteSpace = WhiteSpace.Nowrap
            },
            [".reward-card"] = new()
            {
                BackgroundColor = Color.FromHex("#f7f7f8"),
                MinHeight = Length.Px(126)
            },
            [".reward-card .ion-card-content"] = new()
            {
                Padding = 12
            },
            [".notice-card"] = new()
            {
                BackgroundColor = Color.FromHex("#eaf8fd"),
                Color = Color.FromHex("#2684a7"),
                Margin = new Margin(10, 0)
            },
            [".detail-tabs"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                FlexShrink = 0
            },
            [".detail-segment"] = new()
            {
                Width = Length.Px(190)
            },
            [".open-danmaku"] = new()
            {
                MarginLeft = Length.Auto
            },
            [".synopsis"] = new()
            {
                FontSize = Length.Px(12),
                LineHeight = Length.Px(20),
                Color = Color.FromHex("#8b8e94"),
                Margin = new Margin(8, 0)
            },
            [".episode-list"] = new()
            {
                Display = Display.Flex,
                OverflowX = Overflow.Auto,
                Height = Length.Px(48),
                AlignItems = AlignItems.FlexStart,
                Gap = Length.Px(6)
            },
            [".episode-button"] = new()
            {
                MinWidth = Length.Px(76),
                Height = Length.Px(32),
                FlexShrink = 0,
                Margin = 0
            },
            [".detail-actions, .comment-form"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.SpaceAround
            },
            [".comment-input"] = new()
            {
                FlexGrow = 1,
                FlexBasis = Length.Px(0),
                MinWidth = Length.Px(0)
            },
            [".send-comment"] = new()
            {
                Width = Length.Px(64),
                FlexShrink = 0
            },
            [".comment-row"] = new()
            {
                Display = Display.Flex,
                Gap = Length.Px(12),
                Padding = new Padding(14, 4),
                BorderBottom = new BorderSide(1, BorderStyle.Solid, Color.FromHex("#f4f4f4"))
            },
            [".comment-avatar"] = new()
            {
                Width = Length.Px(34),
                Height = Length.Px(34),
                FlexShrink = 0
            },
            [".comment-author"] = new()
            {
                FontSize = Length.Px(12),
                Color = Color.FromHex("#df7272")
            },
            [".comment-text"] = new()
            {
                FontSize = Length.Px(13),
                LineHeight = Length.Px(23),
                Margin = new Margin(10, 0)
            },
            [".ion-overlay-host, .danmaku-modal"] = new()
            {
                ZIndex = 20000
            },
            [".floating-player-actions"] = new()
            {
                Position = Position.Fixed,
                Top = Length.Px(55),
                Right = Length.Px(10),
                Display = Display.Flex,
                ZIndex = 10001
            },
            [".danmaku-modal .modal-wrapper"] = new()
            {
                Position = Position.Absolute,
                Bottom = Length.Px(0),
                Left = Length.Px(0),
                Right = Length.Px(0),
                Height = Length.Px(260),
                MaxHeight = Length.Percent(90)
            },
            [".color-label"] = new()
            {
                MarginTop = Length.Px(14),
                FontSize = Length.Px(12)
            },
            [".danmaku-modal.validation-visible .modal-wrapper"] = new()
            {
                Height = Length.Px(320)
            },
            [".color-options"] = new()
            {
                Display = Display.Flex,
                JustifyContent = JustifyContent.SpaceBetween
            },
            [".color-options .ion-button"] = new()
            {
                MinWidth = Length.Px(28),
                Width = Length.Px(34),
                Margin = 0
            },
            [".color-swatch"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Px(22),
                Height = Length.Px(22),
                FlexShrink = 0,
                BorderRadius = 11,
                BorderWidth = Length.Px(1),
                BorderStyle = BorderStyle.Solid,
                BorderColor = Color.FromHex("#b8bec4"),
                FontSize = Length.Px(14),
                Color = Color.FromHex("#20252b")
            },
            [".color-options .button-native"] = new()
            {
                Padding = 0
            }
        });

        return styleSheet;
    }
}
