using Miko.Animation;
using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>
/// Styles for the slides components (<c>ion-slides</c> / <c>ion-slide</c>). Ports the relevant
/// parts of Ionic's bundled Swiper vendor stylesheet (<c>slides-vendor.scss</c>), the slides
/// theming (<c>slides.scss</c> + the per-mode <c>slides.md/ios.scss</c> bullet/scrollbar vars) and
/// the slide layout (<c>slide.scss</c>).
/// <para>
/// Only the resting layout is ported — the container clips its overflow and lays the wrapper out as
/// a horizontal flex row; the wrapper is translated to the active slide (stamped by
/// <c>IonSlides.Build</c>). The full Swiper gesture/3D-effect machinery is out of scope. Rules are
/// scoped by the active mode class (<c>md</c> / <c>ios</c>); see <see cref="PageStyles"/> for the
/// mode-scoping rationale. The structural swiper-* classes are not mode-scoped (they match the
/// vendor stylesheet, which applies them regardless of mode), but they are reachable only under a
/// mode-stamped <c>ion-slides</c> host.
/// </para>
/// </summary>
internal static class SlidesStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            // ion-slides host (slides.scss `ion-slides` + slides-vendor.scss `.swiper-container`):
            // a block container that clips its overflow so off-screen slides stay hidden. Centered
            // horizontally, position:relative anchors the absolute pager/scrollbar, z-index:1 is the
            // vendor "fix of Webkit flickering". user-select:none keeps drags from selecting text.
            [$".{mode}.slides-{mode}"] = new()
            {
                Display = Display.Block,
                Position = Position.Relative,
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                MarginLeft = Length.Auto,
                MarginRight = Length.Auto,
                OverflowX = Overflow.Hidden,
                OverflowY = Overflow.Hidden,
                ZIndex = 1,
                UserSelect = UserSelect.None,
            },

            // swiper-wrapper (slides-vendor.scss `.swiper-wrapper`): the flex row holding the
            // slides. Fills the viewport; box-sizing:content-box matches the vendor. Transitions its
            // transform so ActiveIndex changes animate the slide into view.
            [$".{mode}.slides-{mode} .swiper-wrapper"] = new()
            {
                Position = Position.Relative,
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                ZIndex = 1,
                Display = Display.Flex,
                BoxSizing = BoxSizing.ContentBox,
                Transitions = new List<Transition>
                {
                    new Transition(nameof(Style.Transform), 0.3f, TimingFunction.EaseOut),
                },
            },

            // swiper-slide (slide.scss `.swiper-slide` + slides-vendor.scss `.swiper-slide`): each
            // slide is non-shrinking and fills the viewport, centering its content both ways. 18px
            // base font, centered text, border-box.
            [$".{mode}.swiper-slide"] = new()
            {
                Display = Display.Flex,
                Position = Position.Relative,
                FlexShrink = 0,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                FontSize = Length.Px(18),
                TextAlign = TextAlign.Center,
                BoxSizing = BoxSizing.BorderBox,
            },

            // swiper-zoom-container (slides-vendor.scss): centers the (optional) zoomable content.
            // Kept for DOM/structure parity even though the port has no zoom gesture.
            [$".{mode}.swiper-slide.swiper-zoom-container"] = new()
            {
                Display = Display.Flex,
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                TextAlign = TextAlign.Center,
            },

            // Pagination container (slides-vendor.scss `.swiper-pagination`): absolutely centered
            // along the bottom of the host.
            [$".{mode}.slides-{mode} .swiper-pagination"] = new()
            {
                Position = Position.Absolute,
                Bottom = Length.Px(10),
                Left = Length.Px(0),
                Width = Length.Percent(100),
                TextAlign = TextAlign.Center,
                ZIndex = 10,
            },

            // Pagination bullet (slides-vendor.scss + slides.scss): a small dot using the themed
            // bullet background. Active bullet (below) is fully opaque on the active color.
            [$".{mode}.slides-{mode} .swiper-pagination-bullet"] = new()
            {
                Display = Display.InlineBlock,
                Width = Length.Px(8),
                Height = Length.Px(8),
                BorderRadius = Radius(50),
                BackgroundColor = t.SlidesBulletBackground,
                Opacity = 0.2f,
            },
            [$".{mode}.slides-{mode} .swiper-pagination-bullet-active"] = new()
            {
                Opacity = 1f,
                BackgroundColor = t.SlidesBulletBackgroundActive,
            },

            // Scrollbar (slides-vendor.scss `.swiper-scrollbar` + slides.scss): rounded track pinned
            // along the bottom of the host, using the themed scroll-bar background.
            [$".{mode}.slides-{mode} .swiper-scrollbar"] = new()
            {
                Position = Position.Absolute,
                Left = Length.Percent(1),
                Bottom = Length.Px(3),
                ZIndex = 50,
                Height = Length.Px(5),
                Width = Length.Percent(98),
                BorderRadius = Radius(10),
                BackgroundColor = t.SlidesScrollBarBackground,
            },
            [$".{mode}.slides-{mode} .swiper-scrollbar-drag"] = new()
            {
                Position = Position.Relative,
                Width = Length.Percent(100),
                Height = Length.Percent(100),
                BorderRadius = Radius(10),
                BackgroundColor = t.SlidesScrollBarBackgroundActive,
            },
        };

        return css;
    }

    private static BorderRadius Radius(float px) =>
        new BorderRadius(Length.Px(px), Length.Px(px), Length.Px(px), Length.Px(px));
}
