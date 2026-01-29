# Miko 架构设计文档

## 1. 概述

Miko 是一个基于 C# 的轻量级 DOM 渲染引擎，使用 SkiaSharp 进行图形渲染。它借鉴了浏览器的渲染流水线设计，但通过代码直接构建 DOM 树和样式，无需 HTML/CSS 解析器和脚本引擎。

### 1.1 核心特性
- 编程式 DOM 构建
- 样式系统（选择器、样式表、行内样式）
- 完整的盒子模型支持
- 布局计算引擎
- 脏区域重绘优化
- SkiaSharp 渲染后端

### 1.2 技术栈
- **开发语言**: C#
- **渲染引擎**: SkiaSharp
- **目标框架**: .NET

---

## 2. 架构分层

```
┌─────────────────────────────────────┐
│         应用层 (Application)         │
│    DOM 构建 + 样式定义 + 事件处理     │
└─────────────────────────────────────┘
                  ↓
┌─────────────────────────────────────┐
│         DOM 层 (DOM Tree)           │
│  Element 树形结构 + 节点属性管理      │
└─────────────────────────────────────┘
                  ↓
┌─────────────────────────────────────┐
│        样式层 (Style System)         │
│  StyleSheet + Selector + Cascade     │
└─────────────────────────────────────┘
                  ↓
┌─────────────────────────────────────┐
│       布局层 (Layout Engine)         │
│  Box Model + Flow Layout + Flex      │
└─────────────────────────────────────┘
                  ↓
┌─────────────────────────────────────┐
│       渲染层 (Render Engine)         │
│  Dirty Rect + Paint + SkiaSharp      │
└─────────────────────────────────────┘
```

---

## 3. 核心模块设计

### 3.1 DOM 模块

#### 3.1.1 元素基类

```csharp
public abstract class Element
{
    public string Id { get; set; }
    public string Class { get; set; }
    public List<Element> Children { get; set; }
    public Element Parent { get; private set; }
    public Style Style { get; set; }

    // 布局后的盒子模型引用
    internal LayoutBox LayoutBox { get; set; }

    // 脏标记
    internal bool IsDirty { get; set; }

    public void AddChild(Element child);
    public void RemoveChild(Element child);
    public abstract string TagName { get; }
}
```

#### 3.1.2 具体元素类型

```csharp
// 容器元素
public class DivElement : Element
public class SpanElement : Element
public class ParagraphElement : Element

// 标题元素
public class H1Element : Element
public class H2Element : Element
public class H3Element : Element
public class H4Element : Element
public class H5Element : Element
public class H6Element : Element

// 交互元素
public class ButtonElement : Element
public class InputElement : Element

// 媒体元素
public class ImageElement : Element
{
    public string Source { get; set; }
    public SKBitmap Bitmap { get; internal set; }
}
```

---

### 3.2 样式模块

#### 3.2.1 样式对象

```csharp
public class Style
{
    // 布局属性
    public Display Display { get; set; }           // block, inline, inline-block, flex, none
    public FlexDirection FlexDirection { get; set; }
    public JustifyContent JustifyContent { get; set; }
    public AlignItems AlignItems { get; set; }

    // 盒子模型
    public Length Width { get; set; }
    public Length Height { get; set; }
    public Length MinWidth { get; set; }
    public Length MinHeight { get; set; }
    public Length MaxWidth { get; set; }
    public Length MaxHeight { get; set; }

    // 内边距
    public Length PaddingTop { get; set; }
    public Length PaddingRight { get; set; }
    public Length PaddingBottom { get; set; }
    public Length PaddingLeft { get; set; }

    // 外边距
    public Length MarginTop { get; set; }
    public Length MarginRight { get; set; }
    public Length MarginBottom { get; set; }
    public Length MarginLeft { get; set; }

    // 边框
    public Length BorderWidth { get; set; }
    public Color BorderColor { get; set; }
    public BorderStyle BorderStyle { get; set; }

    // 视觉属性
    public Color BackgroundColor { get; set; }
    public Color Color { get; set; }
    public string FontFamily { get; set; }
    public Length FontSize { get; set; }
    public FontWeight FontWeight { get; set; }
    public TextAlign TextAlign { get; set; }

    // 定位
    public Position Position { get; set; }         // static, relative, absolute, fixed
    public Length Top { get; set; }
    public Length Right { get; set; }
    public Length Bottom { get; set; }
    public Length Left { get; set; }

    public float Opacity { get; set; }
    public int ZIndex { get; set; }
}
```

#### 3.2.2 样式表系统

```csharp
public class StyleSheet
{
    public List<StyleRule> Rules { get; set; }
}

public class StyleRule
{
    public Selector Selector { get; set; }
    public Style Style { get; set; }
}

public abstract class Selector
{
    public abstract bool Matches(Element element);
    public abstract int Specificity { get; }
}

// 选择器类型
public class ClassSelector : Selector         // .class-name
public class IdSelector : Selector            // #id
public class TagSelector : Selector           // div, span
public class CombinedSelector : Selector      // 组合选择器
```

#### 3.2.3 样式计算引擎

```csharp
public class StyleResolver
{
    public ComputedStyle Resolve(Element element, List<StyleSheet> styleSheets)
    {
        // 1. 收集所有匹配的样式规则
        // 2. 按照特异性排序
        // 3. 级联合并（行内样式 > ID选择器 > Class选择器 > 标签选择器）
        // 4. 继承父元素的可继承属性
        // 5. 应用默认值
        // 6. 返回计算后的样式
    }
}

public class ComputedStyle : Style
{
    // 包含所有计算和解析后的最终样式值
}
```

---

### 3.3 布局模块

#### 3.3.1 布局引擎

```csharp
public class LayoutEngine
{
    public LayoutBox Layout(Element root, List<StyleSheet> styleSheets)
    {
        // 1. 样式计算：为每个元素计算最终样式
        var resolver = new StyleResolver();
        ComputeStyles(root, styleSheets, resolver);

        // 2. 构建布局树：根据 display 属性过滤和组织
        var layoutRoot = BuildLayoutTree(root);

        // 3. 布局计算：计算每个盒子的位置和尺寸
        CalculateLayout(layoutRoot, new LayoutConstraints());

        return layoutRoot;
    }

    private void CalculateLayout(LayoutBox box, LayoutConstraints constraints)
    {
        switch (box.ComputedStyle.Display)
        {
            case Display.Block:
                LayoutBlock(box, constraints);
                break;
            case Display.Inline:
                LayoutInline(box, constraints);
                break;
            case Display.Flex:
                LayoutFlex(box, constraints);
                break;
            case Display.None:
                // 不布局
                break;
        }
    }
}
```

#### 3.3.2 盒子模型

```csharp
public class LayoutBox
{
    public Element Element { get; set; }
    public ComputedStyle ComputedStyle { get; set; }

    // 盒子维度
    public BoxModel BoxModel { get; set; }

    // 子盒子
    public List<LayoutBox> Children { get; set; }

    // 布局类型
    public LayoutType Type { get; set; }
}

public class BoxModel
{
    // Content box
    public RectF Content { get; set; }

    // Padding box = Content + Padding
    public EdgeSizes Padding { get; set; }

    // Border box = Padding box + Border
    public EdgeSizes Border { get; set; }

    // Margin box = Border box + Margin
    public EdgeSizes Margin { get; set; }

    // 计算后的完整区域
    public RectF PaddingBox => /* Content + Padding */;
    public RectF BorderBox => /* PaddingBox + Border */;
    public RectF MarginBox => /* BorderBox + Margin */;
}

public struct EdgeSizes
{
    public float Top;
    public float Right;
    public float Bottom;
    public float Left;
}

public enum LayoutType
{
    Block,
    Inline,
    InlineBlock,
    Flex,
    FlexItem,
    Anonymous  // 匿名盒子
}
```

#### 3.3.3 布局算法

```csharp
// 块级布局
public class BlockLayout
{
    public void Layout(LayoutBox box, LayoutConstraints constraints)
    {
        // 1. 计算 width（使用约束）
        // 2. 计算 margin, border, padding
        // 3. 布局子元素（垂直堆叠）
        // 4. 计算 height（内容高度或显式指定）
    }
}

// 行内布局
public class InlineLayout
{
    public void Layout(LayoutBox box, LayoutConstraints constraints)
    {
        // 1. 创建行盒子（line box）
        // 2. 水平排列行内元素
        // 3. 处理换行
        // 4. 垂直对齐
    }
}

// Flexbox 布局
public class FlexLayout
{
    public void Layout(LayoutBox box, LayoutConstraints constraints)
    {
        // 1. 确定主轴和交叉轴
        // 2. 计算 flex items 的基础尺寸
        // 3. 处理 flex-grow 和 flex-shrink
        // 4. 主轴对齐（justify-content）
        // 5. 交叉轴对齐（align-items, align-self）
    }
}
```

---

### 3.4 渲染模块

#### 3.4.1 渲染引擎

```csharp
public class RenderEngine
{
    private SKCanvas _canvas;
    private List<RectF> _dirtyRegions;

    public void Render(LayoutBox layoutRoot)
    {
        // 全量渲染
        RenderBox(layoutRoot);
    }

    public void RenderDirty(LayoutBox layoutRoot, List<RectF> dirtyRegions)
    {
        // 脏区域渲染
        _dirtyRegions = dirtyRegions;

        // 设置裁剪区域
        foreach (var region in dirtyRegions)
        {
            _canvas.Save();
            _canvas.ClipRect(region);
            RenderBox(layoutRoot);
            _canvas.Restore();
        }
    }

    private void RenderBox(LayoutBox box)
    {
        if (!ShouldRender(box)) return;

        // 1. 绘制背景
        RenderBackground(box);

        // 2. 绘制边框
        RenderBorder(box);

        // 3. 绘制内容
        RenderContent(box);

        // 4. 递归绘制子元素
        foreach (var child in box.Children)
        {
            RenderBox(child);
        }
    }

    private bool ShouldRender(LayoutBox box)
    {
        // 检查是否在脏区域内
        if (_dirtyRegions != null && _dirtyRegions.Count > 0)
        {
            return _dirtyRegions.Any(r => r.IntersectsWith(box.BoxModel.BorderBox));
        }
        return true;
    }
}
```

#### 3.4.2 绘制功能

```csharp
public class Painter
{
    private SKCanvas _canvas;

    // 绘制背景
    public void DrawBackground(RectF rect, Color color)
    {
        using var paint = new SKPaint
        {
            Color = color.ToSKColor(),
            Style = SKPaintStyle.Fill
        };
        _canvas.DrawRect(rect.ToSKRect(), paint);
    }

    // 绘制边框
    public void DrawBorder(RectF rect, BorderStyle style)
    {
        using var paint = new SKPaint
        {
            Color = style.Color.ToSKColor(),
            StrokeWidth = style.Width,
            Style = SKPaintStyle.Stroke
        };
        _canvas.DrawRect(rect.ToSKRect(), paint);
    }

    // 绘制文本
    public void DrawText(string text, RectF rect, TextStyle style)
    {
        using var paint = new SKPaint
        {
            Color = style.Color.ToSKColor(),
            TextSize = style.FontSize,
            Typeface = SKTypeface.FromFamilyName(style.FontFamily),
            TextAlign = style.TextAlign.ToSKTextAlign()
        };
        _canvas.DrawText(text, rect.Left, rect.Top, paint);
    }

    // 绘制图片
    public void DrawImage(SKBitmap bitmap, RectF rect)
    {
        _canvas.DrawBitmap(bitmap, rect.ToSKRect());
    }
}
```

#### 3.4.3 脏区域管理

```csharp
public class DirtyRegionManager
{
    private List<RectF> _dirtyRegions = new List<RectF>();

    public void MarkDirty(Element element)
    {
        if (element.LayoutBox != null)
        {
            var rect = element.LayoutBox.BoxModel.BorderBox;
            AddDirtyRegion(rect);
            element.IsDirty = true;
        }
    }

    private void AddDirtyRegion(RectF rect)
    {
        // 合并相邻或重叠的脏区域以优化性能
        var merged = false;
        for (int i = 0; i < _dirtyRegions.Count; i++)
        {
            if (_dirtyRegions[i].IntersectsWith(rect) || IsAdjacent(_dirtyRegions[i], rect))
            {
                _dirtyRegions[i] = RectF.Union(_dirtyRegions[i], rect);
                merged = true;
                break;
            }
        }

        if (!merged)
        {
            _dirtyRegions.Add(rect);
        }
    }

    public List<RectF> GetDirtyRegions()
    {
        var regions = new List<RectF>(_dirtyRegions);
        _dirtyRegions.Clear();
        return regions;
    }
}
```

---

## 4. 渲染流水线

```
┌───────────────┐
│  DOM 构建     │  应用层创建元素树
└───────┬───────┘
        ↓
┌───────────────┐
│  样式计算     │  选择器匹配 + 级联 + 继承
└───────┬───────┘
        ↓
┌───────────────┐
│  布局树构建   │  根据 display 属性过滤
└───────┬───────┘
        ↓
┌───────────────┐
│  布局计算     │  盒子模型 + 位置计算
└───────┬───────┘
        ↓
┌───────────────┐
│  绘制列表生成 │  遍历布局树生成绘制命令
└───────┬───────┘
        ↓
┌───────────────┐
│  光栅化       │  SkiaSharp 执行绘制
└───────────────┘
```

### 4.1 完整渲染流程

```csharp
public class MikoEngine
{
    private LayoutEngine _layoutEngine;
    private RenderEngine _renderEngine;
    private DirtyRegionManager _dirtyManager;
    private List<StyleSheet> _styleSheets;

    public void Initialize(Element root, List<StyleSheet> styleSheets)
    {
        _styleSheets = styleSheets;

        // 首次完整渲染
        var layout = _layoutEngine.Layout(root, _styleSheets);
        _renderEngine.Render(layout);
    }

    public void Update(Element root)
    {
        // 增量更新
        if (_dirtyManager.HasDirtyRegions())
        {
            // 重新布局
            var layout = _layoutEngine.Layout(root, _styleSheets);

            // 只重绘脏区域
            var dirtyRegions = _dirtyManager.GetDirtyRegions();
            _renderEngine.RenderDirty(layout, dirtyRegions);
        }
    }

    public void InvalidateElement(Element element)
    {
        _dirtyManager.MarkDirty(element);
    }
}
```

---

## 5. 数据结构与算法

### 5.1 树遍历

- **前序遍历**: 样式计算、脏标记传播
- **后序遍历**: 高度计算、清理操作
- **层序遍历**: Z-index 排序、事件冒泡

### 5.2 样式特异性计算

```
Specificity = (ID数量, Class数量, 标签数量)
比较时按元组字典序
```

### 5.3 布局约束传递

```
父元素 → 子元素: 可用空间约束
子元素 → 父元素: 内容尺寸需求
```

---

## 6. 性能优化策略

### 6.1 脏区域重绘

- 只重绘发生变化的矩形区域
- 合并相邻的脏区域
- 裁剪不可见区域

### 6.2 布局优化

- 缓存布局结果
- 增量布局（只重新计算受影响的子树）
- 延迟布局（批量处理多次修改）

### 6.3 样式优化

- 样式计算缓存
- 选择器索引（按 ID、Class 建立哈希表）
- 避免重复计算

### 6.4 渲染优化

- 图层合成（Layer Compositing）
- 文本缓存
- 图片预加载和缓存

---

## 7. 扩展点设计

### 7.1 自定义元素

```csharp
public class CustomElement : Element
{
    public override string TagName => "custom";

    // 自定义渲染逻辑
    public override void CustomRender(Painter painter, RectF rect)
    {
        // 实现自定义绘制
    }
}
```

### 7.2 自定义布局

```csharp
public interface ILayoutAlgorithm
{
    void Layout(LayoutBox box, LayoutConstraints constraints);
}

// 注册自定义布局算法
LayoutEngine.RegisterLayoutAlgorithm(Display.Custom, new CustomLayout());
```

### 7.3 事件系统（未来扩展）

```csharp
public class EventManager
{
    public void HandleEvent(UIEvent evt)
    {
        // 事件捕获
        // 事件目标
        // 事件冒泡
    }
}
```

---

## 8. 使用示例

### 8.1 基本使用

```csharp
// 1. 创建样式表
var styleSheet = new StyleSheet
{
    Rules = new List<StyleRule>
    {
        new StyleRule
        {
            Selector = new ClassSelector(".container"),
            Style = new Style
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                Padding = new Length(20),
                BackgroundColor = Color.White
            }
        },
        new StyleRule
        {
            Selector = new TagSelector("h1"),
            Style = new Style
            {
                FontSize = new Length(24),
                FontWeight = FontWeight.Bold,
                MarginBottom = new Length(16)
            }
        }
    }
};

// 2. 构建 DOM 树
var root = new DivElement
{
    Class = "container",
    Children =
    {
        new H1Element { TextContent = "Hello Miko" },
        new DivElement
        {
            Style = new Style { Display = Display.Flex },
            Children =
            {
                new ButtonElement { TextContent = "Button 1" },
                new ButtonElement { TextContent = "Button 2" }
            }
        }
    }
};

// 3. 布局和渲染
var engine = new MikoEngine();
engine.Initialize(root, new List<StyleSheet> { styleSheet });

// 4. 更新元素
root.Children[0].TextContent = "Hello World";
engine.InvalidateElement(root.Children[0]);
engine.Update(root);
```

---

## 9. 项目结构

```
Miko/
├── Core/
│   ├── Element.cs                  # 元素基类
│   ├── DomElements/                # 具体元素实现
│   │   ├── DivElement.cs
│   │   ├── TextElements.cs
│   │   ├── ButtonElement.cs
│   │   └── ImageElement.cs
│   └── MikoEngine.cs               # 主引擎
│
├── Styling/
│   ├── Style.cs                    # 样式对象
│   ├── StyleSheet.cs               # 样式表
│   ├── Selectors/                  # 选择器实现
│   │   ├── Selector.cs
│   │   ├── ClassSelector.cs
│   │   ├── IdSelector.cs
│   │   └── TagSelector.cs
│   ├── StyleResolver.cs            # 样式计算
│   └── ComputedStyle.cs            # 计算后样式
│
├── Layout/
│   ├── LayoutEngine.cs             # 布局引擎
│   ├── LayoutBox.cs                # 布局盒子
│   ├── BoxModel.cs                 # 盒子模型
│   ├── LayoutAlgorithms/           # 布局算法
│   │   ├── BlockLayout.cs
│   │   ├── InlineLayout.cs
│   │   └── FlexLayout.cs
│   └── LayoutConstraints.cs        # 布局约束
│
├── Rendering/
│   ├── RenderEngine.cs             # 渲染引擎
│   ├── Painter.cs                  # 绘制工具
│   ├── DirtyRegionManager.cs       # 脏区域管理
│   └── RenderContext.cs            # 渲染上下文
│
├── Common/
│   ├── Length.cs                   # 长度单位
│   ├── Color.cs                    # 颜色
│   ├── RectF.cs                    # 矩形
│   └── Enums.cs                    # 枚举定义
│
└── Utils/
    ├── TreeTraversal.cs            # 树遍历工具
    └── GeometryUtils.cs            # 几何计算工具
```

---

## 10. 开发路线图

### Phase 1: 核心基础（MVP）
- [x] 基础元素类型（Div, Span, H1-H6, P）
- [ ] 基础样式属性（width, height, padding, margin, color, background）
- [ ] 简单选择器（class, id, tag）
- [ ] 块级布局
- [ ] 基础渲染

### Phase 2: 盒子模型完善
- [ ] 完整的边框支持
- [ ] Display 属性（block, inline, inline-block, none）
- [ ] Position 定位（static, relative, absolute, fixed）
- [ ] 脏区域重绘

### Phase 3: 高级布局
- [ ] Flexbox 布局
- [ ] 行内布局和文本换行
- [ ] Z-index 层叠上下文

### Phase 4: 交互与优化
- [ ] 交互元素（Button, Input）
- [ ] 图片加载和渲染
- [ ] 性能优化（布局缓存、渲染优化）
- [ ] 事件系统

### Phase 5: 扩展特性
- [ ] 动画支持
- [ ] 阴影和圆角
- [ ] 渐变背景
- [ ] 自定义元素 API

---

## 11. 技术挑战与解决方案

### 11.1 文本测量和换行

**挑战**: SkiaSharp 需要精确测量文本尺寸以进行布局计算

**解决方案**:
- 使用 `SKPaint.MeasureText()` 获取文本宽度
- 实现文本分词算法处理换行
- 缓存字体度量信息

### 11.2 循环依赖

**挑战**: 子元素尺寸依赖父元素，父元素尺寸依赖子元素

**解决方案**:
- 采用约束-尺寸两阶段布局
- 明确定义布局算法的输入输出
- 使用显式的布局约束对象

### 11.3 浮点精度

**挑战**: 布局计算中的浮点误差累积

**解决方案**:
- 使用亚像素定位但对齐到像素边界绘制
- 实现容差比较
- 关键计算使用更高精度

### 11.4 内存管理

**挑战**: 大型 DOM 树的内存占用

**解决方案**:
- 对象池复用
- 延迟加载和虚拟滚动
- 及时释放 SkiaSharp 资源

---

## 12. 测试策略

### 12.1 单元测试
- 样式计算测试
- 布局算法测试
- 选择器匹配测试
- 盒子模型计算测试

### 12.2 集成测试
- 完整渲染流水线测试
- 多种布局组合测试
- 脏区域更新测试

### 12.3 视觉回归测试
- 截图对比
- 像素级差异检测

### 12.4 性能测试
- 大型 DOM 树渲染性能
- 布局计算耗时
- 内存占用监控

---

## 13. 参考资料

- CSS 2.1 规范：盒子模型、视觉格式化模型
- Flexbox 规范：弹性布局算法
- SkiaSharp 文档：2D 图形绘制 API
- 浏览器渲染原理：Chrome、Firefox 渲染引擎设计

---

**文档版本**: 1.0
**最后更新**: 2026-01-27
**维护者**: Miko Team
