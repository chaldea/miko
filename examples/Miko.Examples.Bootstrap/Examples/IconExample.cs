using Miko.Core;
using Miko.Core.DomElements;
using Miko.Fonts;

namespace Miko.Examples.Bootstrap.Examples;

/// <summary>
/// Bootstrap Icons demonstration using WOFF2 font.
/// </summary>
public static class IconExample
{
    public const string OutputFileName = "bootstrap-icons.png";
    public const string Title = "Bootstrap Icons Examples";

    // Bootstrap Icons Unicode codepoints
    public static class Icons
    {
        // Navigation & Actions
        public const string House = "\uf425";
        public const string Search = "\uf52a";
        public const string Gear = "\uf3e5";
        public const string Person = "\uf4e1";
        public const string Bell = "\uf18a";
        public const string Envelope = "\uf32f";

        // Common Actions
        public const string Plus = "\uf4fe";
        public const string Dash = "\uf2ea";
        public const string Check = "\uf26e";
        public const string X = "\uf62a";
        public const string Pencil = "\uf4cb";
        public const string Trash = "\uf5de";

        // Files & Folders
        public const string Download = "\uf30a";
        public const string Upload = "\uf603";
        public const string Folder = "\uf3d9";
        public const string File = "\uf3c2";

        // Status & Info
        public const string InfoCircle = "\uf431";
        public const string QuestionCircle = "\uf505";
        public const string Exclamation = "\uf33c";
        public const string Eye = "\uf341";
        public const string Lock = "\uf47b";
        public const string Clock = "\uf293";

        // Social & Media
        public const string Heart = "\uf417";
        public const string Star = "\uf588";
        public const string Cart = "\uf242";
        public const string Phone = "\uf4e7";
        public const string Camera = "\uf221";
        public const string Image = "\uf42b";

        // Arrows
        public const string ArrowUp = "\uf148";
        public const string ArrowDown = "\uf128";
        public const string ArrowLeft = "\uf12f";
        public const string ArrowRight = "\uf138";

        // Misc
        public const string Calendar = "\uf1f6";
        public const string Bookmark = "\uf1a2";
        public const string Alarm = "\uf102";
    }

    /// <summary>
    /// Register Bootstrap Icons font before creating DOM
    /// </summary>
    public static void RegisterFont()
    {
        var fontPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fonts", "bootstrap-icons.woff2");
        if (!File.Exists(fontPath))
        {
            // Try alternative path
            fontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "bootstrap-icons.woff2");
        }

        if (File.Exists(fontPath))
        {
            FontManager.Instance.RegisterFont("bootstrap-icons", fontPath);
            Console.WriteLine($"  -> Registered bootstrap-icons font from: {fontPath}");
        }
        else
        {
            Console.WriteLine($"  -> Warning: bootstrap-icons.woff2 not found");
        }
    }

    public static Element CreateDOM()
    {
        return new DivElement
        {
            Class = "container",
            Children =
            {
                new H1Element { TextContent = "Bootstrap Icons Examples" },

                // Navigation Icons Section
                new H2Element { TextContent = "Navigation Icons" },
                new DivElement
                {
                    Class = "icon-row",
                    Children =
                    {
                        CreateIconItem(Icons.House, "House"),
                        CreateIconItem(Icons.Search, "Search"),
                        CreateIconItem(Icons.Gear, "Gear"),
                        CreateIconItem(Icons.Person, "Person"),
                        CreateIconItem(Icons.Bell, "Bell"),
                        CreateIconItem(Icons.Envelope, "Envelope"),
                    }
                },

                // Action Icons Section
                new H2Element { TextContent = "Action Icons" },
                new DivElement
                {
                    Class = "icon-row",
                    Children =
                    {
                        CreateIconItem(Icons.Plus, "Plus"),
                        CreateIconItem(Icons.Dash, "Dash"),
                        CreateIconItem(Icons.Check, "Check"),
                        CreateIconItem(Icons.X, "X"),
                        CreateIconItem(Icons.Pencil, "Pencil"),
                        CreateIconItem(Icons.Trash, "Trash"),
                    }
                },

                // File Icons Section
                new H2Element { TextContent = "File Icons" },
                new DivElement
                {
                    Class = "icon-row",
                    Children =
                    {
                        CreateIconItem(Icons.Download, "Download"),
                        CreateIconItem(Icons.Upload, "Upload"),
                        CreateIconItem(Icons.Folder, "Folder"),
                        CreateIconItem(Icons.File, "File"),
                        CreateIconItem(Icons.Image, "Image"),
                        CreateIconItem(Icons.Camera, "Camera"),
                    }
                },

                // Status Icons Section
                new H2Element { TextContent = "Status Icons" },
                new DivElement
                {
                    Class = "icon-row",
                    Children =
                    {
                        CreateIconItem(Icons.InfoCircle, "Info"),
                        CreateIconItem(Icons.QuestionCircle, "Question"),
                        CreateIconItem(Icons.Exclamation, "Exclamation"),
                        CreateIconItem(Icons.Eye, "Eye"),
                        CreateIconItem(Icons.Lock, "Lock"),
                        CreateIconItem(Icons.Clock, "Clock"),
                    }
                },

                // Social Icons Section
                new H2Element { TextContent = "Social & Media Icons" },
                new DivElement
                {
                    Class = "icon-row",
                    Children =
                    {
                        CreateIconItem(Icons.Heart, "Heart"),
                        CreateIconItem(Icons.Star, "Star"),
                        CreateIconItem(Icons.Cart, "Cart"),
                        CreateIconItem(Icons.Phone, "Phone"),
                        CreateIconItem(Icons.Calendar, "Calendar"),
                        CreateIconItem(Icons.Bookmark, "Bookmark"),
                    }
                },

                // Arrow Icons Section
                new H2Element { TextContent = "Arrow Icons" },
                new DivElement
                {
                    Class = "icon-row",
                    Children =
                    {
                        CreateIconItem(Icons.ArrowUp, "Up"),
                        CreateIconItem(Icons.ArrowDown, "Down"),
                        CreateIconItem(Icons.ArrowLeft, "Left"),
                        CreateIconItem(Icons.ArrowRight, "Right"),
                        CreateIconItem(Icons.Alarm, "Alarm"),
                    }
                },

                // Icon Sizes Section
                new H2Element { TextContent = "Icon Sizes" },
                new DivElement
                {
                    Class = "icon-row",
                    Children =
                    {
                        CreateSizedIcon(Icons.Heart, "icon-sm", "Small"),
                        CreateSizedIcon(Icons.Heart, "icon-md", "Medium"),
                        CreateSizedIcon(Icons.Heart, "icon-lg", "Large"),
                        CreateSizedIcon(Icons.Heart, "icon-xl", "X-Large"),
                    }
                },

                // Icons with Text Section
                new H2Element { TextContent = "Icons with Text" },
                new DivElement
                {
                    Class = "icon-row",
                    Children =
                    {
                        CreateIconButton(Icons.Download, "Download"),
                        CreateIconButton(Icons.Upload, "Upload"),
                        CreateIconButton(Icons.Trash, "Delete"),
                        CreateIconButton(Icons.Pencil, "Edit"),
                    }
                },

                // Colored Icons Section
                new H2Element { TextContent = "Colored Icons" },
                new DivElement
                {
                    Class = "icon-row",
                    Children =
                    {
                        CreateColoredIcon(Icons.Heart, "icon-danger", "Danger"),
                        CreateColoredIcon(Icons.Check, "icon-success", "Success"),
                        CreateColoredIcon(Icons.InfoCircle, "icon-info", "Info"),
                        CreateColoredIcon(Icons.Exclamation, "icon-warning", "Warning"),
                        CreateColoredIcon(Icons.Star, "icon-primary", "Primary"),
                    }
                },
            }
        };
    }

    private static Element CreateIconItem(string icon, string label)
    {
        return new DivElement
        {
            Class = "icon-item",
            Children =
            {
                new SpanElement { TextContent = icon, Class = "bi" },
                new SpanElement { TextContent = label, Class = "icon-label" }
            }
        };
    }

    private static Element CreateSizedIcon(string icon, string sizeClass, string label)
    {
        return new DivElement
        {
            Class = "icon-item",
            Children =
            {
                new SpanElement { TextContent = icon, Class = $"bi {sizeClass}" },
                new SpanElement { TextContent = label, Class = "icon-label" }
            }
        };
    }

    private static Element CreateIconButton(string icon, string label)
    {
        return new ButtonElement
        {
            Class = "btn-primary icon-btn",
            Children =
            {
                new SpanElement { TextContent = icon, Class = "bi btn-icon" },
                new SpanElement { TextContent = label }
            }
        };
    }

    private static Element CreateColoredIcon(string icon, string colorClass, string label)
    {
        return new DivElement
        {
            Class = "icon-item",
            Children =
            {
                new SpanElement { TextContent = icon, Class = $"bi icon-lg {colorClass}" },
                new SpanElement { TextContent = label, Class = "icon-label" }
            }
        };
    }
}
