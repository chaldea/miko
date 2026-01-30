using Miko.Core;
using Miko.Core.DomElements;

namespace Miko.Examples.Bootstrap.Examples;

/// <summary>
/// Bootstrap-style list demonstration.
/// </summary>
public static class ListExample
{
    public const string OutputFileName = "bootstrap-lists.png";
    public const string Title = "Bootstrap List Examples";

    public static Element CreateDOM()
    {
        return new DivElement
        {
            Class = "container",
            Children =
            {
                new H1Element { TextContent = "Bootstrap List Examples" },

                // Basic Lists Section
                new H2Element { TextContent = "Basic Lists" },
                new DivElement
                {
                    Class = "row",
                    Children =
                    {
                        // Unordered List
                        new DivElement
                        {
                            Class = "col",
                            Children =
                            {
                                new ParagraphElement { TextContent = "Unordered List:" },
                                new UlElement
                                {
                                    Children =
                                    {
                                        new LiElement { TextContent = "First item" },
                                        new LiElement { TextContent = "Second item" },
                                        new LiElement { TextContent = "Third item" }
                                    }
                                }
                            }
                        },
                        // Ordered List
                        new DivElement
                        {
                            Class = "col",
                            Children =
                            {
                                new ParagraphElement { TextContent = "Ordered List:" },
                                new OlElement
                                {
                                    Children =
                                    {
                                        new LiElement { TextContent = "First item" },
                                        new LiElement { TextContent = "Second item" },
                                        new LiElement { TextContent = "Third item" }
                                    }
                                }
                            }
                        }
                    }
                },

                // List Group Section
                new H2Element { TextContent = "List Group" },
                new UlElement
                {
                    Class = "list-group",
                    Children =
                    {
                        new LiElement { TextContent = "An item", Class = "list-group-item" },
                        new LiElement { TextContent = "A second item", Class = "list-group-item" },
                        new LiElement { TextContent = "A third item", Class = "list-group-item" },
                        new LiElement { TextContent = "A fourth item", Class = "list-group-item" },
                        new LiElement { TextContent = "And a fifth one", Class = "list-group-item" }
                    }
                },

                // Active and Disabled Items
                new H2Element { TextContent = "Active and Disabled Items" },
                new UlElement
                {
                    Class = "list-group",
                    Children =
                    {
                        new LiElement { TextContent = "An active item", Class = "list-group-item list-group-item-active" },
                        new LiElement { TextContent = "A second item", Class = "list-group-item" },
                        new LiElement { TextContent = "A third item", Class = "list-group-item" },
                        new LiElement { TextContent = "A disabled item", Class = "list-group-item list-group-item-disabled" },
                        new LiElement { TextContent = "And a fifth one", Class = "list-group-item" }
                    }
                },

                // Flush List Group
                new H2Element { TextContent = "Flush List Group" },
                new UlElement
                {
                    Class = "list-group list-group-flush",
                    Children =
                    {
                        new LiElement { TextContent = "An item", Class = "list-group-item" },
                        new LiElement { TextContent = "A second item", Class = "list-group-item" },
                        new LiElement { TextContent = "A third item", Class = "list-group-item" },
                        new LiElement { TextContent = "A fourth item", Class = "list-group-item" },
                        new LiElement { TextContent = "And a fifth one", Class = "list-group-item" }
                    }
                },

                // Contextual List Items
                new H2Element { TextContent = "Contextual List Items" },
                new UlElement
                {
                    Class = "list-group",
                    Children =
                    {
                        new LiElement { TextContent = "A simple default list group item", Class = "list-group-item" },
                        new LiElement { TextContent = "A simple primary list group item", Class = "list-group-item list-group-item-primary" },
                        new LiElement { TextContent = "A simple secondary list group item", Class = "list-group-item list-group-item-secondary" },
                        new LiElement { TextContent = "A simple success list group item", Class = "list-group-item list-group-item-success" },
                        new LiElement { TextContent = "A simple danger list group item", Class = "list-group-item list-group-item-danger" },
                        new LiElement { TextContent = "A simple warning list group item", Class = "list-group-item list-group-item-warning" },
                        new LiElement { TextContent = "A simple info list group item", Class = "list-group-item list-group-item-info" },
                        new LiElement { TextContent = "A simple light list group item", Class = "list-group-item list-group-item-light" },
                        new LiElement { TextContent = "A simple dark list group item", Class = "list-group-item list-group-item-dark" }
                    }
                },

                // Numbered List Group
                new H2Element { TextContent = "Numbered List Group" },
                new OlElement
                {
                    Class = "list-group list-group-numbered",
                    Children =
                    {
                        new LiElement { TextContent = "A list item", Class = "list-group-item" },
                        new LiElement { TextContent = "A list item", Class = "list-group-item" },
                        new LiElement { TextContent = "A list item", Class = "list-group-item" }
                    }
                }
            }
        };
    }
}
