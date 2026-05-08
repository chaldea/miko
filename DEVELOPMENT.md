# Miko 开发说明

## 项目简介

Miko 是一个轻量级 DOM 渲染引擎，使用 C# 编写，基于 SkiaSharp 进行图形渲染。它实现了类浏览器的渲染管线，但通过编程方式构建 DOM 树和样式（无需 HTML/CSS 解析）。

## 环境要求

- .NET 10.0 SDK
- 操作系统：Windows / macOS / Linux（支持 SkiaSharp 的平台）

## 项目结构

```
miko-v5/
├── miko.slnx                          # 解决方案文件
├── src/Miko/                          # 核心库
│   ├── Common/                        # 基础类型（Length, Color, RectF, Enums）
│   ├── Core/                          # DOM 元素与引擎
│   │   ├── Element.cs                 # 元素基类
│   │   ├── MikoEngine.cs             # 主引擎（协调布局与渲染）
│   │   ├── ElementState.cs           # 元素状态（Hover, Focus, Disabled 等）
│   │   └── DomElements/             # 具体元素实现
│   ├── Styling/                       # 样式系统
│   │   ├── Style.cs                  # 样式属性定义
│   │   ├── StyleSheet.cs             # 样式表与规则
│   │   ├── StyleResolver.cs          # 样式解析与级联
│   │   ├── ComputedStyle.cs          # 计算后的最终样式
│   │   └── Selectors/               # 选择器（Tag, Class, ID, 伪类, 复合）
│   ├── Layout/                        # 布局引擎
│   │   ├── LayoutEngine.cs           # 布局协调器
│   │   ├── LayoutBox.cs              # 布局树节点
│   │   ├── BoxModel.cs               # CSS 盒模型
│   │   ├── LayoutConstraints.cs      # 布局约束
│   │   └── LayoutAlgorithms/        # 布局算法（Block, Inline, Flex）
│   ├── Rendering/                     # 渲染引擎
│   │   ├── RenderEngine.cs           # 渲染管理（含脏区域优化）
│   │   ├── Painter.cs                # SkiaSharp 绘制原语
│   │   └── DirtyRegionManager.cs    # 脏区域追踪与合并
│   ├── Fonts/                         # 字体管理
│   │   ├── FontManager.cs            # 字体注册、查找、缓存
│   │   ├── FontFallbackResolver.cs   # 字体回退链
│   │   ├── Woff2Decoder.cs           # WOFF2 解码器
│   │   └── TextRun.cs               # 文本分段
│   ├── Events/                        # 事件系统
│   │   ├── EventDispatcher.cs        # 事件分发（冒泡/捕获）
│   │   ├── EventTypes.cs             # 事件类型定义
│   │   └── EventArgs.cs             # 事件参数
│   └── Utils/                         # 工具类
│       ├── TreeTraversal.cs          # DOM 树遍历
│       ├── TextMeasurer.cs           # 文本测量
│       └── GeometryUtils.cs         # 几何计算
├── tests/Miko.Tests/                  # 单元测试
└── examples/Miko.Examples.Bootstrap/  # Bootstrap 风格示例
```

## 构建与测试

```bash
# 构建整个解决方案
dotnet build miko.slnx

# 运行所有测试
dotnet test

# 运行特定测试类
dotnet test --filter "FullyQualifiedName~Miko.Tests.Layout.BoxModelTests"

# 运行示例程序（输出 PNG 到 output/ 目录）
dotnet run --project examples/Miko.Examples.Bootstrap/Miko.Examples.Bootstrap.csproj

# 清理构建产物
dotnet clean
```

## 依赖项

| 包名 | 版本 | 用途 |
|------|------|------|
| SkiaSharp | 3.119.1 | 2D 图形渲染 |
| xUnit | 2.9.3 | 测试框架 |
| Shouldly | 4.3.0 | 断言库 |
| coverlet.collector | 6.0.4 | 代码覆盖率 |

## 渲染管线

```
1. DOM 构建        → 程序化创建 Element 树
2. 样式计算        → StyleResolver 匹配选择器并级联
3. 布局树构建      → 根据 display 属性过滤元素
4. 布局计算        → 约束向下传递，尺寸向上回传
5. 绘制            → RenderEngine 通过 Painter 绘制到 SKCanvas
```

## 核心概念

### Element（元素）

所有 DOM 元素继承自 `Element` 基类。可用元素：

| 类名 | 标签 | 说明 |
|------|------|------|
| DivElement | div | 块级容器 |
| SpanElement | span | 行内容器 |
| ParagraphElement | p | 段落 |
| H1Element ~ H6Element | h1~h6 | 标题 |
| ButtonElement | button | 按钮 |
| InputElement | input | 输入框（Text/Password/Checkbox/Radio/Range） |
| SelectElement | select | 下拉选择框 |
| ImageElement | img | 图片 |
| UlElement / OlElement / LiElement | ul/ol/li | 列表 |
| TableElement 等 | table/tr/td/th | 表格 |

### Style（样式）

样式属性使用 nullable 类型支持级联（null 表示未设置）：

```csharp
var style = new Style
{
    Display = Display.Flex,
    FlexDirection = FlexDirection.Row,
    Width = Length.Percent(100),
    Padding = new Padding(Length.Px(16)),
    BackgroundColor = new Color(255, 255, 255),
    BorderRadius = new BorderRadius(4)
};
```

### Length（长度）

支持三种单位：

```csharp
Length.Px(16)        // 像素
Length.Percent(50)   // 百分比（相对于父容器）
Length.Auto          // 自动计算
```

### StyleSheet（样式表）

通过选择器匹配元素并应用样式：

```csharp
var styleSheet = new StyleSheet();
styleSheet.AddRule(new ClassSelector("btn"), new Style { ... });
styleSheet.AddRule(new IdSelector("header"), new Style { ... });
styleSheet.AddRule(new TagSelector("p"), new Style { ... });
```

选择器优先级：内联样式 > ID > Class > Tag

### Layout（布局）

支持三种布局算法：

- **BlockLayout**：块级布局，子元素垂直排列
- **InlineLayout**：行内布局，子元素水平排列并自动换行
- **FlexLayout**：弹性布局，支持 flex-direction、justify-content、align-items

### 字体管理

FontManager 是单例模式，支持注册自定义字体（TTF/OTF/WOFF/WOFF2）：

```csharp
FontManager.Instance.RegisterFont("MyFont", "/path/to/font.woff2");
```

内置字体回退链：Arial → Segoe UI → Microsoft YaHei → SimSun → MS Gothic → Malgun Gothic

## 开发指南

### 添加新元素

1. 在 `Core/DomElements/` 中创建新类，继承 `Element`
2. 实现 `TagName` 属性
3. 如需特殊渲染逻辑，在 `RenderEngine` 中添加处理

```csharp
public class MyElement : Element
{
    public override string TagName => "my-element";
}
```

### 添加新布局算法

1. 在 `Layout/LayoutAlgorithms/` 中创建新类
2. 实现布局逻辑：计算自身尺寸 → 布局子元素 → 设置最终尺寸
3. 在 `LayoutEngine.CalculateLayout()` 中注册分发

### 添加新选择器

1. 在 `Styling/Selectors/` 中创建新类，继承 `Selector`
2. 实现 `Matches(Element)` 和 `Specificity` 属性

### 编写测试

测试文件镜像源码结构，使用 Shouldly 断言：

```csharp
public class MyFeatureTests
{
    [Fact]
    public void Should_do_something_when_condition()
    {
        var element = new DivElement { Class = "test" };
        element.HasClass("test").ShouldBeTrue();
    }
}
```

## 使用示例

### 最小渲染流程

```csharp
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Miko.Styling.Selectors;
using Miko.Common;
using SkiaSharp;

// 1. 创建样式表
var styleSheet = new StyleSheet();
styleSheet.AddRule(new ClassSelector("container"), new Style
{
    Display = Display.Flex,
    FlexDirection = FlexDirection.Column,
    Padding = new Padding(Length.Px(20)),
    BackgroundColor = new Color(245, 245, 245)
});

// 2. 构建 DOM 树
var root = new DivElement { Class = "container" };
root.AddChild(new H1Element { TextContent = "Hello Miko" });
root.AddChild(new ParagraphElement { TextContent = "A lightweight rendering engine." });

// 3. 创建画布并渲染
using var surface = SKSurface.Create(new SKImageInfo(800, 600));
var canvas = surface.Canvas;
canvas.Clear(SKColors.White);

var engine = new MikoEngine();
engine.Initialize(root, new List<StyleSheet> { styleSheet }, canvas, 800, 600);

// 4. 导出图片
using var image = surface.Snapshot();
using var data = image.Encode(SKEncodedImageFormat.Png, 100);
using var stream = File.OpenWrite("output.png");
data.SaveTo(stream);
```

### 增量更新

```csharp
// 修改元素后标记为脏
element.TextContent = "Updated text";
engine.InvalidateElement(element);

// 增量渲染（只重绘脏区域）
engine.Update(canvas);
```

### 事件处理

```csharp
var button = new ButtonElement { TextContent = "Click me" };
button.OnClick = (sender, args) =>
{
    Console.WriteLine($"Clicked at ({args.X}, {args.Y})");
};

button.AddEventListener<MouseEventArgs>("click", (sender, args) =>
{
    // 自定义事件处理
});
```

## 注意事项

- SkiaSharp 资源需要正确释放（使用 `using` 语句）
- 布局计算使用 float，注意浮点精度问题
- 启用了 nullable reference types，遵守 null 注解
- 样式级联中 null 表示"未设置"，不要与默认值混淆
- `IsDirty` 标记在 `AddChild`/`RemoveChild`/状态变更时自动设置
