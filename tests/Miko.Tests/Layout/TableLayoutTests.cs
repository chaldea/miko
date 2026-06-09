using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// 表格布局测试 - 验证 Display.Table 的布局正确性
/// </summary>
public class TableLayoutTests
{
    private readonly LayoutEngine _layoutEngine = new();

    [Fact]
    public void TableLayout_SimpleTable_ShouldUseTableDisplay()
    {
        // Arrange
        var table = new TableElement();
        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("table"),
                        Style = new Style { Display = Display.Table }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(table, styleSheets, 800, 600);

        // Assert
        layoutRoot.ShouldNotBeNull();
        layoutRoot.ComputedStyle.Display.ShouldBe(Display.Table);
        layoutRoot.Type.ShouldBe(LayoutType.Table);
    }

    [Fact]
    public void TableLayout_WithSingleRow_ShouldLayoutRow()
    {
        // Arrange
        var table = new TableElement();
        var row = new TrElement();
        table.AddChild(row);

        var styleSheets = new List<StyleSheet>();

        // Act
        var layoutRoot = _layoutEngine.Layout(table, styleSheets, 800, 600);

        // Assert
        layoutRoot.ShouldNotBeNull();
        layoutRoot.ComputedStyle.Display.ShouldBe(Display.Table);
        layoutRoot.Children.Count.ShouldBe(1);
        layoutRoot.Children[0].ComputedStyle.Display.ShouldBe(Display.TableRow);
        layoutRoot.Children[0].Type.ShouldBe(LayoutType.TableRow);
    }

    [Fact]
    public void TableLayout_WithRowsAndCells_ShouldLayoutCells()
    {
        // Arrange
        var table = new TableElement();
        var row1 = new TrElement();
        var cell1 = new TdElement { TextContent = "Cell 1" };
        var cell2 = new TdElement { TextContent = "Cell 2" };
        row1.AddChild(cell1);
        row1.AddChild(cell2);
        table.AddChild(row1);

        var styleSheets = new List<StyleSheet>();

        // Act
        var layoutRoot = _layoutEngine.Layout(table, styleSheets, 800, 600);

        // Assert
        layoutRoot.ShouldNotBeNull();
        layoutRoot.Children.Count.ShouldBe(1);

        var layoutRow = layoutRoot.Children[0];
        layoutRow.Children.Count.ShouldBe(2);
        layoutRow.Children[0].ComputedStyle.Display.ShouldBe(Display.TableCell);
        layoutRow.Children[0].Type.ShouldBe(LayoutType.TableCell);
        layoutRow.Children[1].ComputedStyle.Display.ShouldBe(Display.TableCell);
        layoutRow.Children[1].Type.ShouldBe(LayoutType.TableCell);
    }

    [Fact]
    public void TableLayout_WithFixedWidth_ShouldUseSpecifiedWidth()
    {
        // Arrange
        var table = new TableElement();
        var row = new TrElement();
        var cell = new TdElement { TextContent = "Cell" };
        row.AddChild(cell);
        table.AddChild(row);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("table"),
                        Style = new Style
                        {
                            Display = Display.Table,
                            Width = Length.Px(600)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(table, styleSheets, 800, 600);

        // Assert
        layoutRoot.BoxModel.Content.Width.ShouldBe(600);
    }

    [Fact]
    public void TableLayout_MultipleRows_ShouldStackVertically()
    {
        // Arrange
        var table = new TableElement();
        var row1 = new TrElement();
        var cell1 = new TdElement { TextContent = "Row 1" };
        row1.AddChild(cell1);

        var row2 = new TrElement();
        var cell2 = new TdElement { TextContent = "Row 2" };
        row2.AddChild(cell2);

        table.AddChild(row1);
        table.AddChild(row2);

        var styleSheets = new List<StyleSheet>();

        // Act
        var layoutRoot = _layoutEngine.Layout(table, styleSheets, 800, 600);

        // Assert
        layoutRoot.Children.Count.ShouldBe(2);

        var layoutRow1 = layoutRoot.Children[0];
        var layoutRow2 = layoutRoot.Children[1];

        // 第二行应该在第一行下方
        layoutRow2.BoxModel.Content.Y.ShouldBeGreaterThan(layoutRow1.BoxModel.Content.Y);
    }

    [Fact]
    public void TableLayout_WithTheadTbody_ShouldCollectRows()
    {
        // Arrange
        var table = new TableElement();

        var thead = new TheadElement();
        var headerRow = new TrElement();
        var headerCell = new ThElement { TextContent = "Header" };
        headerRow.AddChild(headerCell);
        thead.AddChild(headerRow);

        var tbody = new TbodyElement();
        var bodyRow = new TrElement();
        var bodyCell = new TdElement { TextContent = "Body" };
        bodyRow.AddChild(bodyCell);
        tbody.AddChild(bodyRow);

        table.AddChild(thead);
        table.AddChild(tbody);

        var styleSheets = new List<StyleSheet>();

        // Act
        var layoutRoot = _layoutEngine.Layout(table, styleSheets, 800, 600);

        // Assert
        layoutRoot.ShouldNotBeNull();
        // thead 和 tbody 作为容器，行应该被递归收集
        layoutRoot.Children.Count.ShouldBe(2); // thead, tbody
    }

    [Fact]
    public void TableLayout_CellsInRow_ShouldBeHorizontallyAdjacent()
    {
        // Arrange
        var table = new TableElement();
        var row = new TrElement();
        var cell1 = new TdElement { TextContent = "Cell 1" };
        var cell2 = new TdElement { TextContent = "Cell 2" };
        var cell3 = new TdElement { TextContent = "Cell 3" };
        row.AddChild(cell1);
        row.AddChild(cell2);
        row.AddChild(cell3);
        table.AddChild(row);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("table"),
                        Style = new Style
                        {
                            Display = Display.Table,
                            Width = Length.Px(600)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(table, styleSheets, 800, 600);

        // Assert
        var layoutRow = layoutRoot.Children[0];
        layoutRow.Children.Count.ShouldBe(3);

        var layoutCell1 = layoutRow.Children[0];
        var layoutCell2 = layoutRow.Children[1];
        var layoutCell3 = layoutRow.Children[2];

        // 单元格应该水平排列
        layoutCell2.BoxModel.Content.X.ShouldBeGreaterThan(layoutCell1.BoxModel.Content.X);
        layoutCell3.BoxModel.Content.X.ShouldBeGreaterThan(layoutCell2.BoxModel.Content.X);

        // 单元格应该在同一行（Y 坐标相同）
        layoutCell1.BoxModel.Content.Y.ShouldBe(layoutCell2.BoxModel.Content.Y);
        layoutCell2.BoxModel.Content.Y.ShouldBe(layoutCell3.BoxModel.Content.Y);
    }

    [Fact]
    public void TableLayout_EqualColumnWidths_ShouldDistributeByWeight()
    {
        // Arrange
        // 三列内容相近（单字符），剩余空间按 preferred 权重分配后各列宽度近似相等。
        var table = new TableElement();
        var row = new TrElement();
        var cell1 = new TdElement { TextContent = "A" };
        var cell2 = new TdElement { TextContent = "B" };
        var cell3 = new TdElement { TextContent = "C" };
        row.AddChild(cell1);
        row.AddChild(cell2);
        row.AddChild(cell3);
        table.AddChild(row);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("table"),
                        Style = new Style
                        {
                            Display = Display.Table,
                            Width = Length.Px(600)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(table, styleSheets, 600, 600);

        // Assert
        var layoutRow = layoutRoot.Children[0];
        var layoutCell1 = layoutRow.Children[0];
        var layoutCell2 = layoutRow.Children[1];
        var layoutCell3 = layoutRow.Children[2];

        // 列宽之和应填满表格宽度 600
        float totalBorderBox =
            layoutCell1.BoxModel.BorderBox.Width +
            layoutCell2.BoxModel.BorderBox.Width +
            layoutCell3.BoxModel.BorderBox.Width;
        totalBorderBox.ShouldBe(600, 2.0f);

        // 内容相近时，各列宽度应近似相等（约 200，含较宽容差容纳字形差异）
        float expectedWidth = 200;
        layoutCell1.BoxModel.Content.Width.ShouldBe(expectedWidth, 10.0f);
        layoutCell2.BoxModel.Content.Width.ShouldBe(expectedWidth, 10.0f);
        layoutCell3.BoxModel.Content.Width.ShouldBe(expectedWidth, 10.0f);
    }

    [Fact]
    public void TableLayout_Auto_ExtraSpace_ShouldDistributeByPreferredWeight()
    {
        // Arrange
        // 验证剩余空间按各列 preferred(max-content) 权重分配：
        // preferred 更大的列应分到更多剩余空间。
        // 公式：extra_i = remaining * (preferred_i / sum(preferred)), width_i = preferred_i + extra_i
        var table = new TableElement();
        var row = new TrElement();
        var narrowCell = new TdElement { TextContent = "X" };
        var wideCell = new TdElement { TextContent = "Wide content here that is longer" };
        row.AddChild(narrowCell);
        row.AddChild(wideCell);
        table.AddChild(row);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("table"),
                        Style = new Style
                        {
                            Display = Display.Table,
                            Width = Length.Px(800)  // 远大于内容总宽，存在大量剩余空间
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(table, styleSheets, 800, 600);

        // Assert
        var layoutRow = layoutRoot.Children[0];
        var layoutNarrow = layoutRow.Children[0];
        var layoutWide = layoutRow.Children[1];

        // preferred 更大的列（wide）应获得更多宽度
        layoutWide.BoxModel.Content.Width.ShouldBeGreaterThan(layoutNarrow.BoxModel.Content.Width);

        // 总宽应填满表格 800
        float total = layoutNarrow.BoxModel.BorderBox.Width + layoutWide.BoxModel.BorderBox.Width;
        total.ShouldBe(800, 2.0f);

        // 验证权重分配方向：wide 列与 narrow 列的宽度比应显著大于 1
        // （若是均分，比例会接近 1；权重分配让差距被放大）
        float ratio = layoutWide.BoxModel.Content.Width / layoutNarrow.BoxModel.Content.Width;
        ratio.ShouldBeGreaterThan(1.5f);
    }

    [Fact]
    public void TableLayout_ThElement_ShouldHaveBoldAndCenterAlignment()
    {
        // Arrange
        var table = new TableElement();
        var row = new TrElement();
        var headerCell = new ThElement { TextContent = "Header" };
        row.AddChild(headerCell);
        table.AddChild(row);

        // 不提供任何用户样式表：验证 th 的默认 UA 样式行为（加粗、居中）
        var styleSheets = new List<StyleSheet>();

        // Act
        var layoutRoot = _layoutEngine.Layout(table, styleSheets, 800, 600);

        // Assert
        var layoutRow = layoutRoot.Children[0];
        var layoutCell = layoutRow.Children[0];

        // th 默认 UA 样式：font-weight: bold, text-align: center, display: table-cell
        layoutCell.ComputedStyle.FontWeight.ShouldBe(FontWeight.Bold);
        layoutCell.ComputedStyle.TextAlign.ShouldBe(TextAlign.Center);
        layoutCell.ComputedStyle.Display.ShouldBe(Display.TableCell);
    }

    [Fact]
    public void TableLayout_TdElement_ShouldHaveLeftAlignment()
    {
        // Arrange
        var table = new TableElement();
        var row = new TrElement();
        var dataCell = new TdElement { TextContent = "Data" };
        row.AddChild(dataCell);
        table.AddChild(row);

        var styleSheets = new List<StyleSheet>();

        // Act
        var layoutRoot = _layoutEngine.Layout(table, styleSheets, 800, 600);

        // Assert
        var layoutRow = layoutRoot.Children[0];
        var layoutCell = layoutRow.Children[0];

        layoutCell.ComputedStyle.TextAlign.ShouldBe(TextAlign.Left);
    }

    [Fact]
    public void TableLayout_WithColSpan_ShouldSpanMultipleColumns()
    {
        // Arrange
        var table = new TableElement();

        var row1 = new TrElement();
        var cell1 = new TdElement { TextContent = "A", ColSpan = 2 };
        var cell2 = new TdElement { TextContent = "B" };
        row1.AddChild(cell1);
        row1.AddChild(cell2);

        var row2 = new TrElement();
        var cell3 = new TdElement { TextContent = "C" };
        var cell4 = new TdElement { TextContent = "D" };
        var cell5 = new TdElement { TextContent = "E" };
        row2.AddChild(cell3);
        row2.AddChild(cell4);
        row2.AddChild(cell5);

        table.AddChild(row1);
        table.AddChild(row2);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("table"),
                        Style = new Style
                        {
                            Display = Display.Table,
                            Width = Length.Px(600)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(table, styleSheets, 800, 600);

        // Assert
        var layoutRow1 = layoutRoot.Children[0];
        var layoutRow2 = layoutRoot.Children[1];

        var colSpanCell = layoutRow1.Children[0];
        var normalCell = layoutRow2.Children[0];

        // ColSpan=2 的单元格应该是普通单元格的两倍宽
        // 由于单元格有 padding/border，使用较大的容差
        colSpanCell.BoxModel.Content.Width.ShouldBe(normalCell.BoxModel.Content.Width * 2, 5.0f);
    }

    [Fact]
    public void TableLayout_AutoWidth_ShouldShrinkToFitContent()
    {
        // Arrange
        var table = new TableElement();
        var row = new TrElement();
        var cell = new TdElement { TextContent = "Cell" };
        row.AddChild(cell);
        table.AddChild(row);

        var styleSheets = new List<StyleSheet>();

        // Act
        var layoutRoot = _layoutEngine.Layout(table, styleSheets, 800, 600);

        // Assert
        // 默认 table-layout: auto 下，无显式宽度的 <table> shrink-to-fit 到内容宽度，
        // 而不是填满父容器（与浏览器行为一致）。
        layoutRoot.BoxModel.Content.Width.ShouldBeLessThan(800);
        layoutRoot.BoxModel.Content.Width.ShouldBeGreaterThan(0);
    }

    #region table-layout: auto 算法测试

    [Fact]
    public void TableLayout_DefaultAlgorithm_ShouldBeAuto()
    {
        // Arrange
        var table = new TableElement();
        var styleSheets = new List<StyleSheet>();

        // Act
        var layoutRoot = _layoutEngine.Layout(table, styleSheets, 800, 600);

        // Assert
        // 默认 table-layout 应为 auto（与浏览器一致）
        layoutRoot.ComputedStyle.TableLayout.ShouldBe(TableLayoutAlgorithm.Auto);
    }

    [Fact]
    public void TableLayout_Auto_ColumnsWithDifferentContent_ShouldHaveDifferentWidths()
    {
        // Arrange
        // auto 算法应根据内容分配列宽：内容多的列更宽
        var table = new TableElement();
        var row = new TrElement();
        var shortCell = new TdElement { TextContent = "A" };
        var longCell = new TdElement { TextContent = "This is a much longer content cell" };
        row.AddChild(shortCell);
        row.AddChild(longCell);
        table.AddChild(row);

        // 不设置 width，使用 shrink-to-fit
        var styleSheets = new List<StyleSheet>();

        // Act
        var layoutRoot = _layoutEngine.Layout(table, styleSheets, 800, 600);

        // Assert
        var layoutRow = layoutRoot.Children[0];
        var layoutShort = layoutRow.Children[0];
        var layoutLong = layoutRow.Children[1];

        // 内容更多的单元格应该更宽（auto 算法的核心特性）
        layoutLong.BoxModel.Content.Width.ShouldBeGreaterThan(layoutShort.BoxModel.Content.Width);
    }

    [Fact]
    public void TableLayout_Auto_ExplicitWidth_ShouldDistributeExtraSpace()
    {
        // Arrange
        // 当 table 有显式宽度且大于内容总宽时，多余空间应分配给各列
        var table = new TableElement();
        var row = new TrElement();
        var cell1 = new TdElement { TextContent = "A" };
        var cell2 = new TdElement { TextContent = "B" };
        row.AddChild(cell1);
        row.AddChild(cell2);
        table.AddChild(row);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("table"),
                        Style = new Style
                        {
                            Display = Display.Table,
                            TableLayout = TableLayoutAlgorithm.Auto,
                            Width = Length.Px(400)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(table, styleSheets, 800, 600);

        // Assert
        // 表格宽度应接近显式设置的 400（列宽之和填满表格）
        layoutRoot.BoxModel.Content.Width.ShouldBe(400, 1.0f);

        // 两列应填满表格宽度
        var layoutRow = layoutRoot.Children[0];
        float totalCellWidth = 0;
        foreach (var cell in layoutRow.Children)
        {
            // 单元格 border-box 宽度 = content + padding(各1px)
            totalCellWidth += cell.BoxModel.BorderBox.Width;
        }
        totalCellWidth.ShouldBe(400, 2.0f);
    }

    [Fact]
    public void TableLayout_Auto_NarrowTable_ShouldRespectMinContent()
    {
        // Arrange
        // 当可用宽度小于内容 max-content 时，列宽收缩但不小于 min-content
        var table = new TableElement();
        var row = new TrElement();
        var cell1 = new TdElement { TextContent = "Word" };
        var cell2 = new TdElement { TextContent = "AnotherWord" };
        row.AddChild(cell1);
        row.AddChild(cell2);
        table.AddChild(row);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("table"),
                        Style = new Style
                        {
                            Display = Display.Table,
                            TableLayout = TableLayoutAlgorithm.Auto,
                            Width = Length.Px(50)  // 故意设置很窄
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(table, styleSheets, 800, 600);

        // Assert
        // 所有单元格宽度应大于 0（不应塌缩）
        var layoutRow = layoutRoot.Children[0];
        foreach (var cell in layoutRow.Children)
        {
            cell.BoxModel.Content.Width.ShouldBeGreaterThan(0);
        }
    }

    #endregion

    #region table-layout: fixed 算法测试

    [Fact]
    public void TableLayout_Fixed_ShouldDistributeColumnsEvenly()
    {
        // Arrange
        // fixed 算法忽略内容，按列数平均分配
        var table = new TableElement();
        var row = new TrElement();
        var shortCell = new TdElement { TextContent = "A" };
        var longCell = new TdElement { TextContent = "This is a much longer content cell" };
        row.AddChild(shortCell);
        row.AddChild(longCell);
        table.AddChild(row);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("table"),
                        Style = new Style
                        {
                            Display = Display.Table,
                            TableLayout = TableLayoutAlgorithm.Fixed,
                            Width = Length.Px(600)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(table, styleSheets, 800, 600);

        // Assert
        var layoutRow = layoutRoot.Children[0];
        var layoutShort = layoutRow.Children[0];
        var layoutLong = layoutRow.Children[1];

        // fixed 算法下，两列应等宽（忽略内容差异），各占 300（600/2）
        // 内容宽度 = 300 - padding(2px) = 298
        layoutShort.BoxModel.Content.Width.ShouldBe(layoutLong.BoxModel.Content.Width, 1.0f);
    }

    [Fact]
    public void TableLayout_Fixed_WithExplicitCellWidth_ShouldUseSpecifiedWidth()
    {
        // Arrange
        // fixed 算法下，首行单元格的显式宽度决定列宽
        var table = new TableElement();
        var row = new TrElement();
        var cell1 = new TdElement { TextContent = "A", Style = new Style { Width = Length.Px(100) } };
        var cell2 = new TdElement { TextContent = "B" };
        row.AddChild(cell1);
        row.AddChild(cell2);
        table.AddChild(row);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("table"),
                        Style = new Style
                        {
                            Display = Display.Table,
                            TableLayout = TableLayoutAlgorithm.Fixed,
                            Width = Length.Px(600)
                        }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(table, styleSheets, 800, 600);

        // Assert
        var layoutRow = layoutRoot.Children[0];
        var layoutCell1 = layoutRow.Children[0];

        // 第一列应使用显式宽度 100（border-box，content = 100 - padding 2 = 98）
        layoutCell1.BoxModel.BorderBox.Width.ShouldBe(100, 2.0f);
    }

    [Fact]
    public void TableLayout_FixedVsAuto_ShouldProduceDifferentLayouts()
    {
        // Arrange - 相同的表格结构，不同算法
        StyleSheet MakeSheet(TableLayoutAlgorithm algo) => new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new StyleRule
                {
                    Selector = new TagSelector("table"),
                    Style = new Style
                    {
                        Display = Display.Table,
                        TableLayout = algo,
                        Width = Length.Px(600)
                    }
                }
            }
        };

        TableElement MakeTable()
        {
            var table = new TableElement();
            var row = new TrElement();
            row.AddChild(new TdElement { TextContent = "A" });
            row.AddChild(new TdElement { TextContent = "This is much longer text content here" });
            table.AddChild(row);
            return table;
        }

        // Act
        var autoLayout = _layoutEngine.Layout(MakeTable(), new List<StyleSheet> { MakeSheet(TableLayoutAlgorithm.Auto) }, 800, 600);
        var fixedLayout = _layoutEngine.Layout(MakeTable(), new List<StyleSheet> { MakeSheet(TableLayoutAlgorithm.Fixed) }, 800, 600);

        // Assert
        var autoCell1 = autoLayout.Children[0].Children[0];
        var fixedCell1 = fixedLayout.Children[0].Children[0];

        // auto 下第一列（短内容）应比 fixed 下窄（fixed 等分，auto 按内容）
        autoCell1.BoxModel.Content.Width.ShouldBeLessThan(fixedCell1.BoxModel.Content.Width);
    }

    #endregion

    #region UA 默认样式测试

    [Fact]
    public void TableLayout_TableElement_ShouldHaveTableDisplayByDefault()
    {
        // Arrange - 无任何用户样式表
        var table = new TableElement();
        var styleSheets = new List<StyleSheet>();

        // Act
        var layoutRoot = _layoutEngine.Layout(table, styleSheets, 800, 600);

        // Assert
        // table 元素默认 display: table
        layoutRoot.ComputedStyle.Display.ShouldBe(Display.Table);
    }

    [Fact]
    public void TableLayout_TrElement_ShouldHaveTableRowDisplayByDefault()
    {
        // Arrange
        var table = new TableElement();
        var row = new TrElement();
        table.AddChild(row);
        var styleSheets = new List<StyleSheet>();

        // Act
        var layoutRoot = _layoutEngine.Layout(table, styleSheets, 800, 600);

        // Assert
        layoutRoot.Children[0].ComputedStyle.Display.ShouldBe(Display.TableRow);
    }

    [Fact]
    public void TableLayout_TdElement_ShouldHaveTableCellDisplayByDefault()
    {
        // Arrange
        var table = new TableElement();
        var row = new TrElement();
        var cell = new TdElement { TextContent = "Data" };
        row.AddChild(cell);
        table.AddChild(row);
        var styleSheets = new List<StyleSheet>();

        // Act
        var layoutRoot = _layoutEngine.Layout(table, styleSheets, 800, 600);

        // Assert
        var layoutCell = layoutRoot.Children[0].Children[0];
        layoutCell.ComputedStyle.Display.ShouldBe(Display.TableCell);
    }

    [Fact]
    public void TableLayout_ThElement_BoldShouldNotBeOverriddenByInheritance()
    {
        // Arrange
        // 回归测试：父元素（table/tr）的 font-weight 不应覆盖 th 的 UA bold 默认值。
        // 此前 bug：继承先于 UA 默认应用，导致 th 继承到 Normal 而 UA bold 失效。
        var table = new TableElement();
        var row = new TrElement();
        var headerCell = new ThElement { TextContent = "Header" };
        row.AddChild(headerCell);
        table.AddChild(row);

        var styleSheets = new List<StyleSheet>();

        // Act
        var layoutRoot = _layoutEngine.Layout(table, styleSheets, 800, 600);

        // Assert
        var layoutCell = layoutRoot.Children[0].Children[0];
        layoutCell.ComputedStyle.FontWeight.ShouldBe(FontWeight.Bold);
    }

    [Fact]
    public void TableLayout_ThVsTd_ShouldHaveDifferentDefaultAlignment()
    {
        // Arrange
        // th 默认居中，td 默认左对齐
        var table = new TableElement();
        var row = new TrElement();
        var th = new ThElement { TextContent = "Header" };
        var td = new TdElement { TextContent = "Data" };
        row.AddChild(th);
        row.AddChild(td);
        table.AddChild(row);

        var styleSheets = new List<StyleSheet>();

        // Act
        var layoutRoot = _layoutEngine.Layout(table, styleSheets, 800, 600);

        // Assert
        var layoutRow = layoutRoot.Children[0];
        var layoutTh = layoutRow.Children[0];
        var layoutTd = layoutRow.Children[1];

        layoutTh.ComputedStyle.TextAlign.ShouldBe(TextAlign.Center);
        layoutTd.ComputedStyle.TextAlign.ShouldBe(TextAlign.Left);
    }

    [Fact]
    public void TableLayout_UserStyleShouldOverrideThBoldDefault()
    {
        // Arrange
        // 用户样式（th { font-weight: normal }）应能覆盖 UA 默认的 bold
        var table = new TableElement();
        var row = new TrElement();
        var th = new ThElement { TextContent = "Header" };
        row.AddChild(th);
        table.AddChild(row);

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new StyleRule
                    {
                        Selector = new TagSelector("th"),
                        Style = new Style { FontWeight = FontWeight.Normal }
                    }
                }
            }
        };

        // Act
        var layoutRoot = _layoutEngine.Layout(table, styleSheets, 800, 600);

        // Assert
        var layoutTh = layoutRoot.Children[0].Children[0];
        // 用户样式优先级高于 UA 默认
        layoutTh.ComputedStyle.FontWeight.ShouldBe(FontWeight.Normal);
    }

    #endregion
}
