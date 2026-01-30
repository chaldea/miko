using Miko.Core;
using Miko.Core.DomElements;

namespace Miko.Examples.Bootstrap.Examples;

/// <summary>
/// Bootstrap-style table demonstration.
/// </summary>
public static class TableExample
{
    public const string OutputFileName = "bootstrap-tables.png";
    public const string Title = "Bootstrap Table Examples";

    public static Element CreateDOM()
    {
        return new DivElement
        {
            Class = "container",
            Children =
            {
                new H1Element { TextContent = "Bootstrap Table Examples" },

                // Basic Table
                new H2Element { TextContent = "Basic Table" },
                CreateBasicTable(),

                // Striped Rows
                new H2Element { TextContent = "Striped Rows" },
                CreateStripedTable(),

                // Bordered Table
                new H2Element { TextContent = "Bordered Table" },
                CreateBorderedTable(),

                // Borderless Table
                new H2Element { TextContent = "Borderless Table" },
                CreateBorderlessTable(),

                // Small Table
                new H2Element { TextContent = "Small Table" },
                CreateSmallTable(),

                // Dark Header Table
                new H2Element { TextContent = "Dark Header" },
                CreateDarkHeaderTable(),

                // Contextual Rows
                new H2Element { TextContent = "Contextual Rows" },
                CreateContextualTable(),

                // Table with Caption
                new H2Element { TextContent = "Table with Caption" },
                CreateCaptionTable()
            }
        };
    }

    private static Element CreateBasicTable()
    {
        return new TableElement
        {
            Class = "table",
            Children =
            {
                // Header
                new TheadElement
                {
                    Children =
                    {
                        new TrElement
                        {
                            Class = "table-header",
                            Children =
                            {
                                new ThElement { TextContent = "#", Class = "table-header-cell" },
                                new ThElement { TextContent = "First", Class = "table-header-cell" },
                                new ThElement { TextContent = "Last", Class = "table-header-cell" },
                                new ThElement { TextContent = "Handle", Class = "table-header-cell" }
                            }
                        }
                    }
                },
                // Body
                new TbodyElement
                {
                    Children =
                    {
                        new TrElement
                        {
                            Class = "table-row",
                            Children =
                            {
                                new TdElement { TextContent = "1", Class = "table-cell" },
                                new TdElement { TextContent = "Mark", Class = "table-cell" },
                                new TdElement { TextContent = "Otto", Class = "table-cell" },
                                new TdElement { TextContent = "@mdo", Class = "table-cell" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-row",
                            Children =
                            {
                                new TdElement { TextContent = "2", Class = "table-cell" },
                                new TdElement { TextContent = "Jacob", Class = "table-cell" },
                                new TdElement { TextContent = "Thornton", Class = "table-cell" },
                                new TdElement { TextContent = "@fat", Class = "table-cell" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-row",
                            Children =
                            {
                                new TdElement { TextContent = "3", Class = "table-cell" },
                                new TdElement { TextContent = "Larry", Class = "table-cell" },
                                new TdElement { TextContent = "Bird", Class = "table-cell" },
                                new TdElement { TextContent = "@twitter", Class = "table-cell" }
                            }
                        }
                    }
                }
            }
        };
    }

    private static Element CreateStripedTable()
    {
        return new TableElement
        {
            Class = "table",
            Children =
            {
                new TheadElement
                {
                    Children =
                    {
                        new TrElement
                        {
                            Class = "table-header",
                            Children =
                            {
                                new ThElement { TextContent = "#", Class = "table-header-cell" },
                                new ThElement { TextContent = "First", Class = "table-header-cell" },
                                new ThElement { TextContent = "Last", Class = "table-header-cell" },
                                new ThElement { TextContent = "Handle", Class = "table-header-cell" }
                            }
                        }
                    }
                },
                new TbodyElement
                {
                    Children =
                    {
                        new TrElement
                        {
                            Class = "table-row",
                            Children =
                            {
                                new TdElement { TextContent = "1", Class = "table-cell" },
                                new TdElement { TextContent = "Mark", Class = "table-cell" },
                                new TdElement { TextContent = "Otto", Class = "table-cell" },
                                new TdElement { TextContent = "@mdo", Class = "table-cell" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-row table-striped",
                            Children =
                            {
                                new TdElement { TextContent = "2", Class = "table-cell" },
                                new TdElement { TextContent = "Jacob", Class = "table-cell" },
                                new TdElement { TextContent = "Thornton", Class = "table-cell" },
                                new TdElement { TextContent = "@fat", Class = "table-cell" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-row",
                            Children =
                            {
                                new TdElement { TextContent = "3", Class = "table-cell" },
                                new TdElement { TextContent = "Larry", Class = "table-cell" },
                                new TdElement { TextContent = "Bird", Class = "table-cell" },
                                new TdElement { TextContent = "@twitter", Class = "table-cell" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-row table-striped",
                            Children =
                            {
                                new TdElement { TextContent = "4", Class = "table-cell" },
                                new TdElement { TextContent = "John", Class = "table-cell" },
                                new TdElement { TextContent = "Doe", Class = "table-cell" },
                                new TdElement { TextContent = "@johnd", Class = "table-cell" }
                            }
                        }
                    }
                }
            }
        };
    }

    private static Element CreateBorderedTable()
    {
        return new TableElement
        {
            Class = "table table-bordered",
            Children =
            {
                new TheadElement
                {
                    Children =
                    {
                        new TrElement
                        {
                            Class = "table-header",
                            Children =
                            {
                                new ThElement { TextContent = "#", Class = "table-header-cell table-bordered-cell" },
                                new ThElement { TextContent = "First", Class = "table-header-cell table-bordered-cell" },
                                new ThElement { TextContent = "Last", Class = "table-header-cell table-bordered-cell" },
                                new ThElement { TextContent = "Handle", Class = "table-header-cell table-bordered-cell" }
                            }
                        }
                    }
                },
                new TbodyElement
                {
                    Children =
                    {
                        new TrElement
                        {
                            Class = "table-row",
                            Children =
                            {
                                new TdElement { TextContent = "1", Class = "table-cell table-bordered-cell" },
                                new TdElement { TextContent = "Mark", Class = "table-cell table-bordered-cell" },
                                new TdElement { TextContent = "Otto", Class = "table-cell table-bordered-cell" },
                                new TdElement { TextContent = "@mdo", Class = "table-cell table-bordered-cell" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-row",
                            Children =
                            {
                                new TdElement { TextContent = "2", Class = "table-cell table-bordered-cell" },
                                new TdElement { TextContent = "Jacob", Class = "table-cell table-bordered-cell" },
                                new TdElement { TextContent = "Thornton", Class = "table-cell table-bordered-cell" },
                                new TdElement { TextContent = "@fat", Class = "table-cell table-bordered-cell" }
                            }
                        }
                    }
                }
            }
        };
    }

    private static Element CreateBorderlessTable()
    {
        return new TableElement
        {
            Class = "table table-borderless",
            Children =
            {
                new TheadElement
                {
                    Children =
                    {
                        new TrElement
                        {
                            Class = "table-header table-borderless-row",
                            Children =
                            {
                                new ThElement { TextContent = "#", Class = "table-header-cell" },
                                new ThElement { TextContent = "First", Class = "table-header-cell" },
                                new ThElement { TextContent = "Last", Class = "table-header-cell" },
                                new ThElement { TextContent = "Handle", Class = "table-header-cell" }
                            }
                        }
                    }
                },
                new TbodyElement
                {
                    Children =
                    {
                        new TrElement
                        {
                            Class = "table-row table-borderless-row",
                            Children =
                            {
                                new TdElement { TextContent = "1", Class = "table-cell" },
                                new TdElement { TextContent = "Mark", Class = "table-cell" },
                                new TdElement { TextContent = "Otto", Class = "table-cell" },
                                new TdElement { TextContent = "@mdo", Class = "table-cell" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-row table-borderless-row",
                            Children =
                            {
                                new TdElement { TextContent = "2", Class = "table-cell" },
                                new TdElement { TextContent = "Jacob", Class = "table-cell" },
                                new TdElement { TextContent = "Thornton", Class = "table-cell" },
                                new TdElement { TextContent = "@fat", Class = "table-cell" }
                            }
                        }
                    }
                }
            }
        };
    }

    private static Element CreateSmallTable()
    {
        return new TableElement
        {
            Class = "table",
            Children =
            {
                new TheadElement
                {
                    Children =
                    {
                        new TrElement
                        {
                            Class = "table-header",
                            Children =
                            {
                                new ThElement { TextContent = "#", Class = "table-header-cell table-sm" },
                                new ThElement { TextContent = "First", Class = "table-header-cell table-sm" },
                                new ThElement { TextContent = "Last", Class = "table-header-cell table-sm" },
                                new ThElement { TextContent = "Handle", Class = "table-header-cell table-sm" }
                            }
                        }
                    }
                },
                new TbodyElement
                {
                    Children =
                    {
                        new TrElement
                        {
                            Class = "table-row",
                            Children =
                            {
                                new TdElement { TextContent = "1", Class = "table-cell table-sm" },
                                new TdElement { TextContent = "Mark", Class = "table-cell table-sm" },
                                new TdElement { TextContent = "Otto", Class = "table-cell table-sm" },
                                new TdElement { TextContent = "@mdo", Class = "table-cell table-sm" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-row",
                            Children =
                            {
                                new TdElement { TextContent = "2", Class = "table-cell table-sm" },
                                new TdElement { TextContent = "Jacob", Class = "table-cell table-sm" },
                                new TdElement { TextContent = "Thornton", Class = "table-cell table-sm" },
                                new TdElement { TextContent = "@fat", Class = "table-cell table-sm" }
                            }
                        }
                    }
                }
            }
        };
    }

    private static Element CreateDarkHeaderTable()
    {
        return new TableElement
        {
            Class = "table",
            Children =
            {
                new TheadElement
                {
                    Children =
                    {
                        new TrElement
                        {
                            Class = "table-header table-dark-header",
                            Children =
                            {
                                new ThElement { TextContent = "#", Class = "table-header-cell" },
                                new ThElement { TextContent = "First", Class = "table-header-cell" },
                                new ThElement { TextContent = "Last", Class = "table-header-cell" },
                                new ThElement { TextContent = "Handle", Class = "table-header-cell" }
                            }
                        }
                    }
                },
                new TbodyElement
                {
                    Children =
                    {
                        new TrElement
                        {
                            Class = "table-row",
                            Children =
                            {
                                new TdElement { TextContent = "1", Class = "table-cell" },
                                new TdElement { TextContent = "Mark", Class = "table-cell" },
                                new TdElement { TextContent = "Otto", Class = "table-cell" },
                                new TdElement { TextContent = "@mdo", Class = "table-cell" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-row",
                            Children =
                            {
                                new TdElement { TextContent = "2", Class = "table-cell" },
                                new TdElement { TextContent = "Jacob", Class = "table-cell" },
                                new TdElement { TextContent = "Thornton", Class = "table-cell" },
                                new TdElement { TextContent = "@fat", Class = "table-cell" }
                            }
                        }
                    }
                }
            }
        };
    }

    private static Element CreateContextualTable()
    {
        return new TableElement
        {
            Class = "table",
            Children =
            {
                new TheadElement
                {
                    Children =
                    {
                        new TrElement
                        {
                            Class = "table-header",
                            Children =
                            {
                                new ThElement { TextContent = "Class", Class = "table-header-cell" },
                                new ThElement { TextContent = "Heading", Class = "table-header-cell" },
                                new ThElement { TextContent = "Heading", Class = "table-header-cell" }
                            }
                        }
                    }
                },
                new TbodyElement
                {
                    Children =
                    {
                        new TrElement
                        {
                            Class = "table-row",
                            Children =
                            {
                                new TdElement { TextContent = "Default", Class = "table-cell" },
                                new TdElement { TextContent = "Cell", Class = "table-cell" },
                                new TdElement { TextContent = "Cell", Class = "table-cell" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-row table-primary",
                            Children =
                            {
                                new TdElement { TextContent = "Primary", Class = "table-cell" },
                                new TdElement { TextContent = "Cell", Class = "table-cell" },
                                new TdElement { TextContent = "Cell", Class = "table-cell" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-row table-secondary",
                            Children =
                            {
                                new TdElement { TextContent = "Secondary", Class = "table-cell" },
                                new TdElement { TextContent = "Cell", Class = "table-cell" },
                                new TdElement { TextContent = "Cell", Class = "table-cell" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-row table-success",
                            Children =
                            {
                                new TdElement { TextContent = "Success", Class = "table-cell" },
                                new TdElement { TextContent = "Cell", Class = "table-cell" },
                                new TdElement { TextContent = "Cell", Class = "table-cell" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-row table-danger",
                            Children =
                            {
                                new TdElement { TextContent = "Danger", Class = "table-cell" },
                                new TdElement { TextContent = "Cell", Class = "table-cell" },
                                new TdElement { TextContent = "Cell", Class = "table-cell" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-row table-warning",
                            Children =
                            {
                                new TdElement { TextContent = "Warning", Class = "table-cell" },
                                new TdElement { TextContent = "Cell", Class = "table-cell" },
                                new TdElement { TextContent = "Cell", Class = "table-cell" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-row table-info",
                            Children =
                            {
                                new TdElement { TextContent = "Info", Class = "table-cell" },
                                new TdElement { TextContent = "Cell", Class = "table-cell" },
                                new TdElement { TextContent = "Cell", Class = "table-cell" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-row table-light",
                            Children =
                            {
                                new TdElement { TextContent = "Light", Class = "table-cell" },
                                new TdElement { TextContent = "Cell", Class = "table-cell" },
                                new TdElement { TextContent = "Cell", Class = "table-cell" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-row table-dark",
                            Children =
                            {
                                new TdElement { TextContent = "Dark", Class = "table-cell" },
                                new TdElement { TextContent = "Cell", Class = "table-cell" },
                                new TdElement { TextContent = "Cell", Class = "table-cell" }
                            }
                        }
                    }
                }
            }
        };
    }

    private static Element CreateCaptionTable()
    {
        return new DivElement
        {
            Children =
            {
                new CaptionElement
                {
                    TextContent = "List of users",
                    Class = "table-caption caption-top"
                },
                new TableElement
                {
                    Class = "table",
                    Children =
                    {
                        new TheadElement
                        {
                            Children =
                            {
                                new TrElement
                                {
                                    Class = "table-header",
                                    Children =
                                    {
                                        new ThElement { TextContent = "#", Class = "table-header-cell" },
                                        new ThElement { TextContent = "First", Class = "table-header-cell" },
                                        new ThElement { TextContent = "Last", Class = "table-header-cell" },
                                        new ThElement { TextContent = "Handle", Class = "table-header-cell" }
                                    }
                                }
                            }
                        },
                        new TbodyElement
                        {
                            Children =
                            {
                                new TrElement
                                {
                                    Class = "table-row",
                                    Children =
                                    {
                                        new TdElement { TextContent = "1", Class = "table-cell" },
                                        new TdElement { TextContent = "Mark", Class = "table-cell" },
                                        new TdElement { TextContent = "Otto", Class = "table-cell" },
                                        new TdElement { TextContent = "@mdo", Class = "table-cell" }
                                    }
                                },
                                new TrElement
                                {
                                    Class = "table-row",
                                    Children =
                                    {
                                        new TdElement { TextContent = "2", Class = "table-cell" },
                                        new TdElement { TextContent = "Jacob", Class = "table-cell" },
                                        new TdElement { TextContent = "Thornton", Class = "table-cell" },
                                        new TdElement { TextContent = "@fat", Class = "table-cell" }
                                    }
                                },
                                new TrElement
                                {
                                    Class = "table-row",
                                    Children =
                                    {
                                        new TdElement { TextContent = "3", Class = "table-cell" },
                                        new TdElement { TextContent = "Larry", Class = "table-cell" },
                                        new TdElement { TextContent = "Bird", Class = "table-cell" },
                                        new TdElement { TextContent = "@twitter", Class = "table-cell" }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
    }
}
