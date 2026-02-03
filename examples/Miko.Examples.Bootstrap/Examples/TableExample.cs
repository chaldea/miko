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

                // Hover Table
                new H2Element { TextContent = "Hover Table" },
                CreateHoverTable(),

                // Small Table
                new H2Element { TextContent = "Small Table" },
                CreateSmallTable(),

                // Contextual Table
                new H2Element { TextContent = "Contextual Classes" },
                CreateContextualTable(),

                // Dark Table
                new H2Element { TextContent = "Dark Table" },
                CreateDarkTable()
            }
        };
    }

    private static TableElement CreateBasicTable()
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
                            Children =
                            {
                                new ThElement { TextContent = "#", Scope = TableHeaderScope.Col },
                                new ThElement { TextContent = "First", Scope = TableHeaderScope.Col },
                                new ThElement { TextContent = "Last", Scope = TableHeaderScope.Col },
                                new ThElement { TextContent = "Handle", Scope = TableHeaderScope.Col }
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
                            Children =
                            {
                                new ThElement { TextContent = "1", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Mark" },
                                new TdElement { TextContent = "Otto" },
                                new TdElement { TextContent = "@mdo" }
                            }
                        },
                        new TrElement
                        {
                            Children =
                            {
                                new ThElement { TextContent = "2", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Jacob" },
                                new TdElement { TextContent = "Thornton" },
                                new TdElement { TextContent = "@fat" }
                            }
                        },
                        new TrElement
                        {
                            Children =
                            {
                                new ThElement { TextContent = "3", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Larry" },
                                new TdElement { TextContent = "Bird" },
                                new TdElement { TextContent = "@twitter" }
                            }
                        }
                    }
                }
            }
        };
    }

    private static TableElement CreateStripedTable()
    {
        return new TableElement
        {
            Class = "table table-striped",
            Children =
            {
                new TheadElement
                {
                    Children =
                    {
                        new TrElement
                        {
                            Children =
                            {
                                new ThElement { TextContent = "#", Scope = TableHeaderScope.Col },
                                new ThElement { TextContent = "First", Scope = TableHeaderScope.Col },
                                new ThElement { TextContent = "Last", Scope = TableHeaderScope.Col },
                                new ThElement { TextContent = "Handle", Scope = TableHeaderScope.Col }
                            }
                        }
                    }
                },
                new TbodyElement
                {
                    Class = "table-striped-body",
                    Children =
                    {
                        new TrElement
                        {
                            Children =
                            {
                                new ThElement { TextContent = "1", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Mark" },
                                new TdElement { TextContent = "Otto" },
                                new TdElement { TextContent = "@mdo" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-row-striped",
                            Children =
                            {
                                new ThElement { TextContent = "2", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Jacob" },
                                new TdElement { TextContent = "Thornton" },
                                new TdElement { TextContent = "@fat" }
                            }
                        },
                        new TrElement
                        {
                            Children =
                            {
                                new ThElement { TextContent = "3", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Larry" },
                                new TdElement { TextContent = "Bird" },
                                new TdElement { TextContent = "@twitter" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-row-striped",
                            Children =
                            {
                                new ThElement { TextContent = "4", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "John" },
                                new TdElement { TextContent = "Doe" },
                                new TdElement { TextContent = "@jdoe" }
                            }
                        }
                    }
                }
            }
        };
    }

    private static TableElement CreateBorderedTable()
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
                            Children =
                            {
                                new ThElement { TextContent = "#", Scope = TableHeaderScope.Col },
                                new ThElement { TextContent = "First", Scope = TableHeaderScope.Col },
                                new ThElement { TextContent = "Last", Scope = TableHeaderScope.Col },
                                new ThElement { TextContent = "Handle", Scope = TableHeaderScope.Col }
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
                            Children =
                            {
                                new ThElement { TextContent = "1", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Mark" },
                                new TdElement { TextContent = "Otto" },
                                new TdElement { TextContent = "@mdo" }
                            }
                        },
                        new TrElement
                        {
                            Children =
                            {
                                new ThElement { TextContent = "2", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Jacob" },
                                new TdElement { TextContent = "Thornton" },
                                new TdElement { TextContent = "@fat" }
                            }
                        },
                        new TrElement
                        {
                            Children =
                            {
                                new ThElement { TextContent = "3", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Larry" },
                                new TdElement { TextContent = "Bird" },
                                new TdElement { TextContent = "@twitter" }
                            }
                        }
                    }
                }
            }
        };
    }

    private static TableElement CreateHoverTable()
    {
        return new TableElement
        {
            Class = "table table-hover",
            Children =
            {
                new TheadElement
                {
                    Children =
                    {
                        new TrElement
                        {
                            Children =
                            {
                                new ThElement { TextContent = "#", Scope = TableHeaderScope.Col },
                                new ThElement { TextContent = "First", Scope = TableHeaderScope.Col },
                                new ThElement { TextContent = "Last", Scope = TableHeaderScope.Col },
                                new ThElement { TextContent = "Handle", Scope = TableHeaderScope.Col }
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
                            Children =
                            {
                                new ThElement { TextContent = "1", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Mark" },
                                new TdElement { TextContent = "Otto" },
                                new TdElement { TextContent = "@mdo" }
                            }
                        },
                        new TrElement
                        {
                            Children =
                            {
                                new ThElement { TextContent = "2", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Jacob" },
                                new TdElement { TextContent = "Thornton" },
                                new TdElement { TextContent = "@fat" }
                            }
                        },
                        new TrElement
                        {
                            Children =
                            {
                                new ThElement { TextContent = "3", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Larry" },
                                new TdElement { TextContent = "Bird" },
                                new TdElement { TextContent = "@twitter" }
                            }
                        }
                    }
                }
            }
        };
    }

    private static TableElement CreateSmallTable()
    {
        return new TableElement
        {
            Class = "table table-sm",
            Children =
            {
                new TheadElement
                {
                    Children =
                    {
                        new TrElement
                        {
                            Children =
                            {
                                new ThElement { TextContent = "#", Scope = TableHeaderScope.Col },
                                new ThElement { TextContent = "First", Scope = TableHeaderScope.Col },
                                new ThElement { TextContent = "Last", Scope = TableHeaderScope.Col },
                                new ThElement { TextContent = "Handle", Scope = TableHeaderScope.Col }
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
                            Children =
                            {
                                new ThElement { TextContent = "1", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Mark" },
                                new TdElement { TextContent = "Otto" },
                                new TdElement { TextContent = "@mdo" }
                            }
                        },
                        new TrElement
                        {
                            Children =
                            {
                                new ThElement { TextContent = "2", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Jacob" },
                                new TdElement { TextContent = "Thornton" },
                                new TdElement { TextContent = "@fat" }
                            }
                        },
                        new TrElement
                        {
                            Children =
                            {
                                new ThElement { TextContent = "3", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Larry" },
                                new TdElement { TextContent = "Bird" },
                                new TdElement { TextContent = "@twitter" }
                            }
                        }
                    }
                }
            }
        };
    }

    private static TableElement CreateContextualTable()
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
                            Children =
                            {
                                new ThElement { TextContent = "Class", Scope = TableHeaderScope.Col },
                                new ThElement { TextContent = "Heading", Scope = TableHeaderScope.Col },
                                new ThElement { TextContent = "Heading", Scope = TableHeaderScope.Col }
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
                            Class = "table-default",
                            Children =
                            {
                                new ThElement { TextContent = "Default", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Cell" },
                                new TdElement { TextContent = "Cell" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-primary",
                            Children =
                            {
                                new ThElement { TextContent = "Primary", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Cell" },
                                new TdElement { TextContent = "Cell" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-secondary",
                            Children =
                            {
                                new ThElement { TextContent = "Secondary", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Cell" },
                                new TdElement { TextContent = "Cell" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-success",
                            Children =
                            {
                                new ThElement { TextContent = "Success", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Cell" },
                                new TdElement { TextContent = "Cell" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-danger",
                            Children =
                            {
                                new ThElement { TextContent = "Danger", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Cell" },
                                new TdElement { TextContent = "Cell" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-warning",
                            Children =
                            {
                                new ThElement { TextContent = "Warning", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Cell" },
                                new TdElement { TextContent = "Cell" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-info",
                            Children =
                            {
                                new ThElement { TextContent = "Info", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Cell" },
                                new TdElement { TextContent = "Cell" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-light",
                            Children =
                            {
                                new ThElement { TextContent = "Light", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Cell" },
                                new TdElement { TextContent = "Cell" }
                            }
                        },
                        new TrElement
                        {
                            Class = "table-dark",
                            Children =
                            {
                                new ThElement { TextContent = "Dark", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Cell" },
                                new TdElement { TextContent = "Cell" }
                            }
                        }
                    }
                }
            }
        };
    }

    private static TableElement CreateDarkTable()
    {
        return new TableElement
        {
            Class = "table table-dark",
            Children =
            {
                new TheadElement
                {
                    Children =
                    {
                        new TrElement
                        {
                            Children =
                            {
                                new ThElement { TextContent = "#", Scope = TableHeaderScope.Col },
                                new ThElement { TextContent = "First", Scope = TableHeaderScope.Col },
                                new ThElement { TextContent = "Last", Scope = TableHeaderScope.Col },
                                new ThElement { TextContent = "Handle", Scope = TableHeaderScope.Col }
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
                            Children =
                            {
                                new ThElement { TextContent = "1", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Mark" },
                                new TdElement { TextContent = "Otto" },
                                new TdElement { TextContent = "@mdo" }
                            }
                        },
                        new TrElement
                        {
                            Children =
                            {
                                new ThElement { TextContent = "2", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Jacob" },
                                new TdElement { TextContent = "Thornton" },
                                new TdElement { TextContent = "@fat" }
                            }
                        },
                        new TrElement
                        {
                            Children =
                            {
                                new ThElement { TextContent = "3", Scope = TableHeaderScope.Row },
                                new TdElement { TextContent = "Larry" },
                                new TdElement { TextContent = "Bird" },
                                new TdElement { TextContent = "@twitter" }
                            }
                        }
                    }
                }
            }
        };
    }
}
