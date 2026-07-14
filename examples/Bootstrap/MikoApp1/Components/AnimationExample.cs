using Miko.Animation;
using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;

namespace Miko.Examples.Bootstrap.Examples;

[Route("/animation")]
public partial class AnimationExample : ComponentBase
{
    public const string Title = "Animation Examples";

    public override Element Build()
    {
        return new DivElement
        {
            Class = "container",
            Children =
            {
                new H1Element { TextContent = "Animation Examples" },

                new ParagraphElement
                {
                    TextContent = "Miko 动画系统支持 CSS 风格的 Keyframe 动画，包括多关键帧、循环、方向控制和填充模式。",
                    Style = new Style { MarginBottom = Length.Px(20) }
                },

                // Keyframe Animation Section
                new H2Element { TextContent = "基础动画" },
                CreateBasicAnimations(),

                // Easing Functions Section
                new H2Element { TextContent = "缓动函数" },
                new ParagraphElement { TextContent = "内置多种缓动函数：linear、ease、ease-in、ease-out、ease-in-out。" },
                CreateEasingDemo(),

                // Combined Animation Section
                new H2Element { TextContent = "组合动画" },
                new ParagraphElement { TextContent = "多属性、多关键帧的复杂动画效果。" },
                CreateCombinedDemo(),
            }
        };
    }

    private Element CreateBasicAnimations()
    {
        var fadeInBox = new DivElement
        {
            Class = "anim-box",
            Style = new Style
            {
                Width = Length.Px(100),
                Height = Length.Px(100),
                BackgroundColor = Color.FromHex("#0d6efd"),
                Animations = new List<KeyframeAnimation>
                {
                    new KeyframeAnimation("fadeIn", 2f,
                        new Keyframe(0f, new Style { Opacity = 0f }),
                        new Keyframe(1f, new Style { Opacity = 1f })
                    )
                    {
                        TimingFunction = TimingFunction.EaseIn,
                        FillMode = AnimationFillMode.Forwards
                    }
                }
            },
            Children = { new SpanElement { TextContent = "Fade In", Style = new Style { Color = Color.White } } }
        };

        var pulseBox = new DivElement
        {
            Class = "anim-box",
            Style = new Style
            {
                Width = Length.Px(100),
                Height = Length.Px(100),
                BackgroundColor = Color.FromHex("#198754"),
                Animations = new List<KeyframeAnimation>
                {
                    new KeyframeAnimation("pulse", 1.5f,
                        new Keyframe(0f, new Style { Opacity = 1f }),
                        new Keyframe(0.5f, new Style { Opacity = 0.4f }),
                        new Keyframe(1f, new Style { Opacity = 1f })
                    )
                    {
                        Infinite = true,
                        TimingFunction = TimingFunction.EaseInOut
                    }
                }
            },
            Children = { new SpanElement { TextContent = "Pulse", Style = new Style { Color = Color.White } } }
        };

        var slideBox = new DivElement
        {
            Class = "anim-box",
            Style = new Style
            {
                Width = Length.Px(100),
                Height = Length.Px(100),
                BackgroundColor = Color.FromHex("#dc3545"),
                Animations = new List<KeyframeAnimation>
                {
                    new KeyframeAnimation("slideRight", 2f,
                        new Keyframe(0f, new Style { MarginLeft = Length.Px(0) }),
                        new Keyframe(1f, new Style { MarginLeft = Length.Px(100) })
                    )
                    {
                        Infinite = true,
                        Direction = AnimationDirection.Alternate,
                        TimingFunction = TimingFunction.EaseInOut
                    }
                }
            },
            Children = { new SpanElement { TextContent = "Slide", Style = new Style { Color = Color.White } } }
        };

        var colorShiftBox = new DivElement
        {
            Class = "anim-box",
            Style = new Style
            {
                Width = Length.Px(100),
                Height = Length.Px(100),
                BackgroundColor = Color.FromHex("#6f42c1"),
                Animations = new List<KeyframeAnimation>
                {
                    new KeyframeAnimation("colorShift", 3f,
                        new Keyframe(0f, new Style { BackgroundColor = Color.FromHex("#6f42c1") }),
                        new Keyframe(0.33f, new Style { BackgroundColor = Color.FromHex("#0dcaf0") }),
                        new Keyframe(0.66f, new Style { BackgroundColor = Color.FromHex("#ffc107") }),
                        new Keyframe(1f, new Style { BackgroundColor = Color.FromHex("#6f42c1") })
                    )
                    {
                        Infinite = true,
                        TimingFunction = TimingFunction.Linear
                    }
                }
            },
            Children = { new SpanElement { TextContent = "Color", Style = new Style { Color = Color.White } } }
        };

        return new DivElement
        {
            Class = "row",
            Children = { fadeInBox, pulseBox, slideBox, colorShiftBox }
        };
    }

    private Element CreateEasingDemo()
    {
        var easings = new (string name, TimingFunction func)[]
        {
            ("Linear", TimingFunction.Linear),
            ("Ease", TimingFunction.Ease),
            ("Ease-In", TimingFunction.EaseIn),
            ("Ease-Out", TimingFunction.EaseOut),
            ("Ease-In-Out", TimingFunction.EaseInOut),
        };

        var container = new DivElement { Class = "row", Style = new Style { FlexDirection = FlexDirection.Column, Gap = Length.Px(8) } };

        foreach (var (name, func) in easings)
        {
            var row = new DivElement
            {
                Style = new Style { Display = Display.Flex, AlignItems = AlignItems.Center, Gap = Length.Px(12) }
            };

            var label = new SpanElement
            {
                TextContent = name,
                Style = new Style { Width = Length.Px(100) }
            };

            var bar = new DivElement
            {
                Style = new Style
                {
                    Width = Length.Px(200),
                    Height = Length.Px(30),
                    BackgroundColor = Color.FromHex("#e9ecef"),
                }
            };

            var indicator = new DivElement
            {
                Style = new Style
                {
                    Width = Length.Px(30),
                    Height = Length.Px(30),
                    BackgroundColor = Color.FromHex("#0d6efd"),
                    Animations = new List<KeyframeAnimation>
                    {
                        new KeyframeAnimation($"easing-{name}", 2f,
                            new Keyframe(0f, new Style { MarginLeft = Length.Px(0) }),
                            new Keyframe(1f, new Style { MarginLeft = Length.Px(170) })
                        )
                        {
                            Infinite = true,
                            Direction = AnimationDirection.Alternate,
                            TimingFunction = func
                        }
                    }
                }
            };

            bar.AddChild(indicator);
            row.AddChild(label);
            row.AddChild(bar);
            container.AddChild(row);
        }

        return container;
    }

    private Element CreateCombinedDemo()
    {
        var spinGrowBox = new DivElement
        {
            Class = "anim-box",
            Style = new Style
            {
                Width = Length.Px(80),
                Height = Length.Px(80),
                BackgroundColor = Color.FromHex("#0d6efd"),
                Transform = Transform.FromRotate(0f),
                Animations = new List<KeyframeAnimation>
                {
                    new KeyframeAnimation("spinGrow", 3f,
                        new Keyframe(0f, new Style { Width = Length.Px(60), Height = Length.Px(60), Opacity = 0.5f }),
                        new Keyframe(0.5f, new Style { Width = Length.Px(100), Height = Length.Px(100), Opacity = 1f }),
                        new Keyframe(1f, new Style { Width = Length.Px(60), Height = Length.Px(60), Opacity = 0.5f })
                    )
                    {
                        Infinite = true,
                        TimingFunction = TimingFunction.EaseInOut
                    }
                }
            },
            Children = { new SpanElement { TextContent = "Breathe", Style = new Style { Color = Color.White } } }
        };

        var bounceBox = new DivElement
        {
            Class = "anim-box",
            Style = new Style
            {
                Width = Length.Px(80),
                Height = Length.Px(80),
                BackgroundColor = Color.FromHex("#198754"),
                Animations = new List<KeyframeAnimation>
                {
                    new KeyframeAnimation("bounce", 1f,
                        new Keyframe(0f, new Style { MarginTop = Length.Px(0) }),
                        new Keyframe(0.5f, new Style { MarginTop = Length.Px(40) }),
                        new Keyframe(1f, new Style { MarginTop = Length.Px(0) })
                    )
                    {
                        Infinite = true,
                        TimingFunction = TimingFunction.EaseOut
                    }
                }
            },
            Children = { new SpanElement { TextContent = "Bounce", Style = new Style { Color = Color.White } } }
        };

        var fadeSlideBox = new DivElement
        {
            Class = "anim-box",
            Style = new Style
            {
                Width = Length.Px(80),
                Height = Length.Px(80),
                BackgroundColor = Color.FromHex("#dc3545"),
                Animations = new List<KeyframeAnimation>
                {
                    new KeyframeAnimation("fadeSlide", 2.5f,
                        new Keyframe(0f, new Style { Opacity = 0f, MarginLeft = Length.Px(-50) }),
                        new Keyframe(0.4f, new Style { Opacity = 1f, MarginLeft = Length.Px(0) }),
                        new Keyframe(0.6f, new Style { Opacity = 1f, MarginLeft = Length.Px(0) }),
                        new Keyframe(1f, new Style { Opacity = 0f, MarginLeft = Length.Px(50) })
                    )
                    {
                        Infinite = true,
                        TimingFunction = TimingFunction.EaseInOut
                    }
                }
            },
            Children = { new SpanElement { TextContent = "Fade Slide", Style = new Style { Color = Color.White } } }
        };

        var progressBar = new DivElement
        {
            Style = new Style
            {
                Width = Length.Px(300),
                Height = Length.Px(20),
                BackgroundColor = Color.FromHex("#e9ecef"),
                BorderTopLeftRadius = Length.Px(4),
                BorderTopRightRadius = Length.Px(4),
                BorderBottomLeftRadius = Length.Px(4),
                BorderBottomRightRadius = Length.Px(4),
            }
        };

        var progressFill = new DivElement
        {
            Style = new Style
            {
                Height = Length.Px(20),
                BackgroundColor = Color.FromHex("#0d6efd"),
                BorderTopLeftRadius = Length.Px(4),
                BorderTopRightRadius = Length.Px(4),
                BorderBottomLeftRadius = Length.Px(4),
                BorderBottomRightRadius = Length.Px(4),
                Animations = new List<KeyframeAnimation>
                {
                    new KeyframeAnimation("progress", 3f,
                        new Keyframe(0f, new Style { Width = Length.Px(0) }),
                        new Keyframe(1f, new Style { Width = Length.Px(300) })
                    )
                    {
                        Infinite = true,
                        TimingFunction = TimingFunction.EaseInOut,
                        Direction = AnimationDirection.Alternate
                    }
                }
            }
        };

        progressBar.AddChild(progressFill);

        return new DivElement
        {
            Style = new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Length.Px(16) },
            Children =
            {
                new DivElement
                {
                    Class = "row",
                    Children = { spinGrowBox, bounceBox, fadeSlideBox }
                },
                new DivElement
                {
                    Style = new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Length.Px(4) },
                    Children =
                    {
                        new SpanElement { TextContent = "Progress Bar Animation" },
                        progressBar
                    }
                }
            }
        };
    }
}
