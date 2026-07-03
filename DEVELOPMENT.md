# Miko 开发说明

## 项目简介

Miko 是一个原生、跨平台的 UI 渲染引擎，使用 Razor 作为布局 DSL。它使用 SkiaSharp 直接绘制每个像素——无需浏览器、无需 WebView、无需 HTML/CSS 运行时——在 GPU 加速画布上运行类浏览器的样式级联、布局和增量绘制管线。

你使用 Razor 组件编写 UI——与 Blazor 相同的 `.razor` 语法——Miko 的源生成器将它们编译为原生绘制调用。如果你了解 HTML 和 Razor，就已经知道如何构建 Miko 应用；无需学习 XAML。

## 环境要求

- .NET 10.0 SDK
- 操作系统：Windows / macOS / Linux / Android / iOS（支持 SkiaSharp 的平台）
- 移动端开发需要安装相应的工作负载（`dotnet workload install android` / `dotnet workload install ios`）

## 解决方案结构

项目使用单个解决方案文件：

- **miko.slnx**：包含所有项目（核心库 + 桌面 + 移动端）

## 项目结构

```
miko-v5/
├── miko.slnx                          # 主解决方案（包含所有项目）
├── src/Miko/                          # 核心渲染引擎（仅平台抽象）
│   ├── Common/                        # 基础类型
│   │   ├── Length.cs                  # CSS 长度单位（px, %, auto）
│   │   ├── Color.cs                   # RGBA 颜色（支持 hex/命名颜色解析）
│   │   ├── RectF.cs                   # 矩形几何
│   │   ├── Padding.cs, Margin.cs      # 盒模型间距
│   │   ├── Border.cs, BorderRadius.cs # 边框样式
│   │   ├── BoxShadow.cs               # 阴影效果
│   │   ├── SafeAreaInsets.cs          # 刘海屏安全区
│   │   └── Enums: Display, Position, FlexDirection 等
│   ├── Core/                          # DOM 元素与引擎
│   │   ├── Element.cs                 # 元素基类（树结构、样式、事件、状态）
│   │   ├── MikoEngine.cs              # 主引擎（协调布局/渲染/动画/事件）
│   │   ├── ElementState.cs            # 元素状态（Hover, Focus, Active, Disabled, Checked）
│   │   └── DomElements/               # 具体元素实现
│   │       ├── DivElement, SpanElement, ParagraphElement
│   │       ├── HeadingElements.cs     # H1–H6
│   │       ├── InteractiveElements.cs # Button, Input, Select
│   │       ├── ListElements.cs        # Ul, Ol, Li
│   │       ├── TableElements.cs       # Table, Tr, Td, Th
│   │       ├── MediaElements.cs       # Video, Image
│   │       └── PseudoElement.cs       # ::before, ::after
│   ├── Styling/                       # 样式系统
│   │   ├── Style.cs                   # 样式属性定义（可空以支持级联）
│   │   ├── StyleSheet.cs              # 样式表与规则
│   │   ├── StyleResolver.cs           # 样式解析与级联（特异性排序）
│   │   ├── ComputedStyle.cs           # 计算后的最终样式
│   │   ├── CssObject.cs               # 类型安全的样式构建
│   │   ├── MediaRule.cs, MediaCondition.cs # 媒体查询
│   │   ├── PseudoElementRule.cs       # 伪元素规则
│   │   └── Selectors/                 # 选择器实现
│   │       ├── Selector.cs            # 基类（Matches + Specificity）
│   │       ├── SimpleSelectors.cs     # Tag, Class, ID
│   │       ├── PseudoClassSelectors.cs # :hover, :focus, :checked, :nth-child 等
│   │       ├── PseudoElementSelectors.cs # ::before, ::after
│   │       ├── AttributeSelector.cs   # [attr=value]
│   │       ├── CombinatorSelectors.cs # 后代、子代、相邻、通用兄弟
│   │       ├── CompoundSelector.cs    # 组合选择器
│   │       ├── GroupSelector.cs       # 分组选择器（逗号）
│   │       └── CssSelectorParser.cs   # CSS 选择器字符串解析
│   ├── Layout/                        # 布局引擎
│   │   ├── LayoutEngine.cs            # 布局协调器（样式计算→布局树→布局计算→定位）
│   │   ├── LayoutBox.cs               # 布局树节点（computed style + dimensions）
│   │   ├── BoxModel.cs                # CSS 盒模型（content/padding/border/margin box）
│   │   ├── LayoutConstraints.cs       # 布局约束（可用宽高）
│   │   └── LayoutAlgorithms/          # 布局算法实现
│   │       ├── LayoutDispatcher.cs    # 算法分发器
│   │       ├── BlockLayout.cs         # 块级布局（垂直堆叠）
│   │       ├── InlineLayout.cs        # 行内布局（水平流动 + 自动换行）
│   │       ├── FlexLayout.cs          # Flexbox 布局
│   │       └── TableLayout.cs         # 表格布局
│   ├── Rendering/                     # 渲染引擎
│   │   ├── RenderEngine.cs            # 渲染管理（全量/增量渲染 + 脏区域优化）
│   │   ├── Painter.cs                 # SkiaSharp 绘制原语（矩形、圆角、文本、图像、阴影）
│   │   └── DirtyRegionManager.cs      # 脏区域追踪与合并
│   ├── Fonts/                         # 字体管理
│   │   ├── FontManager.cs             # 单例字体注册表（TTF/OTF/WOFF/WOFF2）
│   │   ├── Woff2Decoder.cs            # WOFF2 解压缩
│   │   ├── FontFallbackResolver.cs    # 多脚本字体回退链
│   │   ├── TextRun.cs                 # 文本按脚本/字体分段
│   │   └── UnicodeScript.cs, FontFormat.cs
│   ├── Events/                        # 事件系统
│   │   ├── EventDispatcher.cs         # DOM 样式事件分发（捕获 + 冒泡）
│   │   ├── EventTypes.cs              # 事件类型常量
│   │   └── EventArgs.cs               # Mouse/Keyboard/Focus/Change/Scroll/Input 事件参数
│   ├── Animation/                     # 动画系统
│   │   ├── AnimationManager.cs        # 关键帧动画和过渡管理器
│   │   ├── KeyframeAnimation.cs       # CSS 样式关键帧动画
│   │   ├── Transition.cs, TransitionBuilder.cs # 属性过渡
│   │   ├── Transform.cs               # 2D 变换（translate, rotate, scale）
│   │   └── EasingFunctions.cs         # 缓动曲线
│   ├── Platform/                      # 平台抽象
│   │   ├── MikoInput.cs               # 平台无关输入枚举（MikoKey, MikoKeyModifiers）
│   │   ├── MikoInteractionController.cs # 平台无关交互逻辑（命中测试、焦点、点击、滚动、文本编辑）
│   │   ├── MikoDispatcher.cs          # 线程调度器
│   │   ├── MikoSynchronizationContext.cs # 同步上下文
│   │   ├── Resources/                 # 资源加载抽象
│   │   │   ├── IImageLoader.cs        # 图像加载接口
│   │   │   └── ResourceManager.cs     # 默认资源管理器（嵌入资源 + 文件路径）
│   │   └── Video/                     # 视频播放抽象
│   │       ├── IVideoBackend.cs       # 视频后端接口
│   │       ├── IVideoSession.cs       # 视频会话
│   │       └── IVideoFrameSource.cs   # 视频帧源（GPU 零拷贝）
│   ├── Components/                    # Razor 组件运行时
│   │   ├── MikoComponent.cs           # 组件基类（Build() 返回 Element）
│   │   ├── LayoutComponentBase.cs     # 布局组件基类
│   │   ├── IComponent.cs              # 组件接口
│   │   ├── RenderFragment.cs          # 子内容渲染委托
│   │   ├── EventCallback.cs           # 异步事件处理器
│   │   ├── 特性：Parameter, CascadingParameter, Route, Layout, Inject
│   │   └── CompilerServices/RuntimeHelpers.cs # 生成代码运行时支持
│   ├── Routing/                       # 客户端路由
│   │   ├── Router.cs                  # 路由器（模式匹配）
│   │   ├── NavigationManager.cs       # 编程式导航
│   │   └── RouteData.cs               # 路由参数与值
│   ├── Hosting/                       # 应用宿主装配
│   │   ├── MikoAppBuilder.cs          # 流式构建器（DI + 配置）
│   │   ├── MikoAppContext.cs          # 平台无关应用上下文（平台宿主消费）
│   │   ├── MikoAppOptions.cs          # 配置选项（标题、尺寸、根组件、样式表、路由）
│   │   └── HotReloadService.cs        # 热重载服务
│   └── Utils/                         # 工具类
│       ├── TreeTraversal.cs           # DOM 树遍历
│       ├── TextMeasurer.cs            # SkiaSharp 文本测量
│       └── GeometryUtils.cs           # 几何计算
├── src/Miko.Windowing/                # 桌面实现（Silk.NET）
│   ├── SilkDesktopHost.cs             # 桌面宿主（窗口/OpenGL/输入/渲染循环）
│   ├── SilkKeyMap.cs                  # Silk ↔ Miko 键码映射
│   └── DesktopHostExtensions.cs       # RunDesktop() 扩展方法
├── src/Miko.Android/                  # Android 实现
│   ├── MikoSurfaceView.cs             # GLSurfaceView + 触摸输入
│   └── MikoAndroidApp.cs              # CreateView() 便捷入口
├── src/Miko.iOS/                      # iOS 实现
│   ├── MikoGLView.cs                  # GLKView + 触摸输入
│   └── MikoViewController.cs          # 视图控制器（CADisplayLink 渲染循环）
├── src/Miko.Simulator/                # 设备模拟器宿主
│   └── SimulatorHost.cs               # 在桌面预览移动应用（设备画布 + 设置面板）
├── src/Miko.Bootstrap/                # Bootstrap 风格 Razor 组件库
│   ├── Components/                    # 按钮、卡片、警告、手风琴等
│   └── Resources/                     # 嵌入式样式表和资源
├── src/Miko.Ionic/                    # Ionic 风格 Razor 组件库
│   ├── Components/                    # IonButton, IonCard, IonTabs 等
│   ├── IonicPlatform.cs               # 平台检测（iOS/Android）
│   ├── IonicStyleSheetFactory.cs      # 模式化样式表（md/ios）
│   └── Components/Core/               # ClassMapper, IonicComponentBase
├── src/Miko.Razor.Compiler/           # 自定义 Razor 源生成器（net9.0）
│   └── 编译 .razor 组件为原生 Miko DOM 元素
├── src/Miko.DevTools/                 # 运行时调试工具
│   └── DOM 和布局树检查器
├── src/Miko.Testing/                  # 测试实用工具
├── tests/Miko.Tests/                  # 核心库单元测试
├── tests/Miko.Ionic.Tests/            # Ionic 组件测试
└── examples/                          # 示例应用
    ├── Bootstrap/                     # Bootstrap 风格示例
    ├── Multiplatform/                 # 多平台示例（Desktop/Android/iOS/Simulator）
    │   ├── MikoAppBlank/              # 空白应用模板
    │   ├── MikoAppTabs/               # Ionic 标签栏应用
    │   └── MikoAppSidemenu/           # Ionic 侧边菜单应用
    ├── Windows/                       # 仅桌面示例
    ├── Async/                         # 异步/HTTP 示例
    ├── Media/                         # 媒体播放示例
    └── Apps/Anime/                    # 完整应用示例
```

### 平台抽象模型

核心 `Miko` 包仅包含平台抽象。共享 UI 项目只引用 `Miko`，通过 `MikoAppBuilder.Build()` 生成 `MikoAppContext`。然后平台特定的启动项目驱动渲染：

```csharp
// 共享 UI 项目（仅引用 Miko）
public static class App
{
    public static MikoAppContext CreateContext()
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.UseTitle("My App");
        builder.UseSize(1024, 768);
        builder.UseGeneratedRoutes();
        return builder.Build();
    }
}

// 桌面启动（引用 Miko.Windowing）
App.CreateContext().RunDesktop();

// Android Activity（引用 Miko.Android）
SetContentView(MikoAndroidApp.CreateView(this, App.CreateContext));

// iOS AppDelegate（引用 Miko.iOS）
Window.RootViewController = new MikoViewController(App.CreateContext());

// 模拟器（引用 Miko.Simulator）
App.CreateContext().RunSimulator();
```

平台宿主提供：
- 窗口/视图管理
- OpenGL/Metal 上下文创建
- 原生输入事件转换为 `MikoKey`/指针事件
- 将事件转发到 `MikoInteractionController`

## 构建与测试

```bash
# 构建整个解决方案
dotnet build miko.slnx

# 构建特定项目
dotnet build src/Miko/Miko.csproj
dotnet build src/Miko.Ionic/Miko.Ionic.csproj

# 构建移动端项目（需要先安装相应工作负载）
dotnet build src/Miko.Android/Miko.Android.csproj
dotnet build src/Miko.iOS/Miko.iOS.csproj

# 运行所有测试
dotnet test

# 运行特定测试项目
dotnet test tests/Miko.Tests/Miko.Tests.csproj
dotnet test tests/Miko.Ionic.Tests/Miko.Ionic.Tests.csproj

# 运行特定测试类
dotnet test --filter "FullyQualifiedName~Miko.Tests.Common.ColorTests"
dotnet test --filter "FullyQualifiedName~Miko.Tests.Layout.BoxModelTests"

# 详细输出运行测试
dotnet test --verbosity detailed

# 运行示例程序
# 桌面示例
dotnet run --project examples/Windows/MikoAppBlank/MikoAppBlank.csproj
dotnet run --project examples/Multiplatform/MikoAppBlank/MikoAppBlank.Desktop/MikoAppBlank.Desktop.csproj

# 设备模拟器（在桌面预览移动应用）
dotnet run --project examples/Multiplatform/MikoAppBlank/MikoAppBlank.Simulator/MikoAppBlank.Simulator.csproj
dotnet run --project examples/Multiplatform/MikoAppTabs/MikoAppTabs.Simulator/MikoAppTabs.Simulator.csproj

# 清理构建产物
dotnet clean
```

## 核心依赖项

**Miko 核心库：**
- SkiaSharp 3.119.1 — 2D 图形渲染
- Svg.Skia 2.0.0.4 — SVG 支持
- Microsoft.Extensions.* 9.0.0 — DI、日志、HTTP 客户端

**平台实现：**
- Silk.NET 2.22.0 — 桌面窗口和 OpenGL（Miko.Windowing、Miko.Simulator）
- Android/iOS SDK — 移动平台原生 API

**测试：**
- xUnit 2.9.3 — 测试框架
- Shouldly 4.3.0 — 流式断言库
- coverlet.collector 6.0.4 — 代码覆盖率
- SkiaSharp.NativeAssets.Linux 3.119.1 — Linux CI 原生库

## 渲染管线

Miko 实现类浏览器的渲染管线：

```
1. DOM 构建        → Razor 组件编译为 Element 树（或程序化创建）
2. 样式计算        → StyleResolver 匹配选择器并按特异性级联
3. 布局树构建      → 根据 display 属性过滤元素（display: none 不参与布局）
4. 布局计算        → 约束向下传递（父→子），尺寸向上回传（子→父）
5. 定位调整        → 处理 relative/absolute 定位偏移
6. 绘制            → RenderEngine 通过 Painter 绘制到 SKCanvas（支持增量渲染）
```

### 样式级联

选择器特异性（从低到高）：**标签选择器 → 类选择器 → ID 选择器 → 内联样式**

特异性计算：
- 标签选择器：1
- 类选择器/伪类/属性选择器：10
- ID 选择器：100
- 内联样式：1000

相同特异性时，后定义的规则优先（`StyleResolver` 按索引排序）。

### 布局算法

**BlockLayout（块级布局）**
- 子元素垂直堆叠
- 宽度填充父容器（除非显式指定）
- 高度适应内容

**InlineLayout（行内布局）**
- 子元素水平流动
- 自动换行（达到容器边界时）
- 支持行高和基线对齐

**FlexLayout（Flexbox 布局）**
- 支持 `flex-direction`（row/column）
- 支持 `justify-content`（主轴对齐）
- 支持 `align-items`（交叉轴对齐）
- 支持 `flex-grow` 和 `flex-shrink`

**TableLayout（表格布局）**
- 自动列宽分配
- 支持跨行/跨列（colspan/rowspan）

### 脏标记与增量渲染

元素追踪 `IsDirty` 标志。当元素变更（样式、文本、子元素）时标记为脏。`RenderEngine` 仅重绘脏区域，合并重叠/相邻矩形以优化性能。

**触发脏标记的操作：**
- `AddChild()` / `RemoveChild()`
- 修改 `TextContent`、`Style`
- 状态变更（`SetState()` / `ClearState()`）
- 动画帧更新

**渲染模式：**
- `MikoEngine.Render(canvas)` — 全量渲染
- `MikoEngine.InvalidateElement(element)` + `MikoEngine.Update(canvas)` — 增量渲染

**增量渲染优化：**
- 脏区域数量 ≤ 30：增量渲染（裁剪 + 多次遍历）
- 脏区域数量 > 30：回退到全量渲染（避免过多遍历开销）

## 核心概念

### Element（元素）

所有 DOM 元素继承自 `Element` 基类。元素形成父子树结构，支持样式、事件和状态管理。

**核心属性：**
- `Id` / `Class` — CSS 选择器匹配
- `Style` — 内联样式
- `TextContent` — 文本内容
- `Children` — 子元素列表
- `Parent` — 父元素引用
- `LayoutBox` — 布局后的盒模型引用
- `IsDirty` — 脏标记（内部使用）
- `State` — 元素状态（Hover, Focus, Active, Disabled, Checked）

**可用元素：**

| 类名 | 标签 | 说明 |
|------|------|------|
| DivElement | div | 块级容器 |
| SpanElement | span | 行内容器 |
| ParagraphElement | p | 段落 |
| H1Element ~ H6Element | h1~h6 | 标题（1-6 级） |
| ButtonElement | button | 按钮 |
| InputElement | input | 输入框（Text/Password/Checkbox/Radio/Range/Date） |
| SelectElement | select | 下拉选择框 |
| ImageElement | img | 图片（支持占位符） |
| VideoElement | video | 视频（需平台注入 IVideoBackend） |
| UlElement / OlElement / LiElement | ul/ol/li | 无序/有序列表 |
| TableElement / TrElement / TdElement / ThElement | table/tr/td/th | 表格 |
| AnchorElement | a | 链接 |
| PseudoElement | ::before / ::after | 伪元素 |

**元素方法：**
- `AddChild(Element)` / `RemoveChild(Element)` — 添加/移除子元素（自动设置 IsDirty）
- `FindById(string)` / `FindByClass(string)` / `FindByTagName(string)` — 查找元素
- `HasClass(string)` / `AddClass(string)` / `RemoveClass(string)` — 类名操作
- `SetState(ElementState)` / `ClearState(ElementState)` — 状态管理
- `AddEventListener<T>(string, handler)` — 添加事件监听器

### Style（样式）

样式属性使用可空类型支持级联（`null` 表示未设置，不覆盖较低优先级规则）：

```csharp
var style = new Style
{
    // 布局属性
    Display = Display.Flex,
    FlexDirection = FlexDirection.Row,
    JustifyContent = JustifyContent.Center,
    AlignItems = AlignItems.Center,
    
    // 尺寸
    Width = Length.Percent(100),
    Height = Length.Auto,
    
    // 盒模型
    Padding = new Padding(Length.Px(16)),
    Margin = new Margin(Length.Px(8)),
    Border = new Border(1, Color.FromHex("#ddd")),
    BorderRadius = new BorderRadius(4),
    
    // 视觉
    BackgroundColor = Color.FromRgb(255, 255, 255),
    Color = Color.FromHex("#333"),
    BoxShadow = new BoxShadow(0, 2, 4, Color.FromRgba(0, 0, 0, 26)),
    Opacity = 1.0f,
    
    // 文本
    FontSize = Length.Px(14),
    FontWeight = FontWeight.Normal,
    TextAlign = TextAlign.Left,
    LineHeight = 1.5f,
    
    // 定位
    Position = Position.Relative,
    Top = Length.Px(10),
    Left = Length.Px(10),
    ZIndex = 1,
    
    // 过渡和变换
    Transition = TransitionBuilder.Create()
        .Property("background-color")
        .Duration(300)
        .Build(),
    Transform = Transform.Translate(10, 0)
};
```

**样式合并：**
`Style.Merge(other)` 方法只在当前值为 `null` 时采用 `other` 的值，实现级联效果。

### Length（长度）

支持三种单位和环境变量：

```csharp
Length.Px(16)        // 像素（绝对单位）
Length.Percent(50)   // 百分比（相对于父容器）
Length.Auto          // 自动计算（由布局算法决定）
```

**环境变量（CSS env() 函数）：**
```csharp
// 安全区内边距（刘海屏、底部手势条）
padding-top: env(safe-area-inset-top);
padding-bottom: env(safe-area-inset-bottom);
```

安全区是**选择性加入**的——只有声明了 `env()` 的元素才会添加内边距，视口本身不内缩，全屏浮层（如菜单遮罩）仍覆盖整个屏幕。

### StyleSheet（样式表）

通过 CSS 对象声明样式，支持嵌套选择器：

```csharp
var styleSheet = new StyleSheet();

styleSheet.Add(new CssObject
{
    // 标签选择器
    ["p"] = new()
    {
        FontSize = Length.Px(16),
        LineHeight = 1.5f
    },
    
    // 类选择器
    [".btn"] = new()
    {
        Padding = new Padding(10, 20),
        BackgroundColor = Color.FromRgb(13, 110, 253),
        Color = Color.White,
        BorderRadius = new BorderRadius(6),
        Cursor = Cursor.Pointer
    },
    
    // ID 选择器
    ["#header"] = new()
    {
        Height = Length.Px(60),
        BackgroundColor = Color.White
    },
    
    // 伪类选择器
    [".btn:hover"] = new()
    {
        BackgroundColor = Color.FromRgb(0, 86, 179)
    },
    
    // 组合选择器
    [".btn.primary"] = new()
    {
        BackgroundColor = Color.FromRgb(13, 110, 253)
    },
    
    // 后代选择器
    [".nav a"] = new()
    {
        Color = Color.FromRgb(33, 37, 41),
        TextDecoration = TextDecoration.None
    },
    
    // 子选择器
    [".list > li"] = new()
    {
        Padding = new Padding(8, 0)
    },
    
    // 伪元素
    [".icon::before"] = new()
    {
        Content = "\"→\"",
        MarginRight = Length.Px(8)
    }
});

// 媒体查询
styleSheet.AddMediaRule(
    new MediaCondition { MinWidth = 768 },
    new List<StyleRule>
    {
        new StyleRule
        {
            Selector = CssSelectorParser.Parse(".container"),
            Style = new Style { MaxWidth = Length.Px(720) }
        }
    }
);
```

**嵌套写法（推荐）：**
```csharp
styleSheet.Add(new CssObject
{
    [".card"] = new()
    {
        Padding = new Padding(20),
        BorderRadius = new BorderRadius(8),
        
        // 嵌套选择器会自动展开为 ".card .title"
        [".title"] = new()
        {
            FontSize = Length.Px(18),
            FontWeight = FontWeight.Bold
        },
        
        // 展开为 ".card .content"
        [".content"] = new()
        {
            MarginTop = Length.Px(12),
            Color = Color.FromRgb(108, 117, 125)
        }
    }
});
```

选择器特异性：**内联样式（1000） > ID（100） > Class/伪类/属性（10） > Tag（1）**

### Layout（布局）

支持四种布局算法：

- **BlockLayout**：块级布局，子元素垂直排列
- **InlineLayout**：行内布局，子元素水平排列并自动换行
- **FlexLayout**：弹性布局，支持 flex-direction、justify-content、align-items、flex-grow/shrink
- **TableLayout**：表格布局，支持自动列宽分配

**盒模型（BoxModel）：**
```
┌─────────────────── Margin Box ────────────────────┐
│  ┌───────────────── Border Box ──────────────────┐ │
│  │  ┌───────────── Padding Box ────────────────┐ │ │
│  │  │  ┌───────── Content Box ──────────────┐ │ │ │
│  │  │  │                                    │ │ │ │
│  │  │  │         Element Content            │ │ │ │
│  │  │  │                                    │ │ │ │
│  │  │  └────────────────────────────────────┘ │ │ │
│  │  └───────────────────────────────────────────┘ │ │
│  └───────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
```

**布局约束（LayoutConstraints）：**
- `AvailableWidth` / `AvailableHeight` — 父容器提供的可用空间
- 约束向下传递：父元素计算子元素的可用空间
- 尺寸向上回传：子元素计算完毕后，父元素根据子元素尺寸调整自身

**定位（Position）：**
- `Static`（默认）：常规流
- `Relative`：相对于自身在常规流中的位置偏移
- `Absolute`：相对于最近的定位祖先（relative/absolute）定位
- `Fixed`：相对于视口定位（尚未完全实现）

### 字体管理

FontManager 是单例模式，支持注册自定义字体（TTF/OTF/WOFF/WOFF2）：

```csharp
FontManager.Instance.RegisterFont("MyFont", "/path/to/font.woff2");
```

内置字体回退链：Arial → Segoe UI → Microsoft YaHei → SimSun → MS Gothic → Malgun Gothic

## 开发指南

### 使用 Razor 组件构建应用

**创建 Razor 组件：**

```razor
@page "/home"
@namespace MyApp.Pages

<div class="container">
    <h1>@Title</h1>
    <p>Welcome to Miko!</p>
    <button @onclick="HandleClick">Click Me</button>
</div>

@code {
    [Parameter]
    public string Title { get; set; } = "Home";
    
    private void HandleClick()
    {
        Title = "Clicked!";
        StateHasChanged();
    }
}
```

**配置应用：**

```csharp
public static class App
{
    public static MikoAppContext CreateContext()
    {
        var builder = MikoAppBuilder.CreateDefault();
        
        // 基础配置
        builder.UseTitle("My App");
        builder.UseSize(1024, 768);
        
        // 路由配置（自动发现 @page 指令）
        builder.UseGeneratedRoutes();
        builder.UseDefaultLayout<MainLayout>();
        
        // 添加样式表
        builder.AddStyleSheet(GlobalStyles.Create());
        
        // 启用热重载
        builder.EnableHotReload();
        
        // 添加开发工具
        builder.AddDevTools();
        
        return builder.Build();
    }
}
```

**桌面启动：**

```csharp
// Program.cs
App.CreateContext().RunDesktop();
```

### 自定义 Razor 编译器配置

Razor 项目必须包含 `_OverrideRazorSourceGenerator` 目标以使用 Miko 的自定义编译器：

```xml
<Target Name="_OverrideRazorSourceGenerator" AfterTargets="_PrepareRazorSourceGenerators">
  <PropertyGroup>
    <_CustomRazorGeneratorDir>$(MSBuildThisFileDirectory)..\Miko.Razor.Compiler\bin\$(Configuration)\net9.0\</_CustomRazorGeneratorDir>
  </PropertyGroup>
  <ItemGroup>
    <Analyzer Remove="@(_RazorAnalyzer)" />
    <_RazorAnalyzer Remove="@(_RazorAnalyzer)" />
  </ItemGroup>
  <ItemGroup>
    <_RazorAnalyzer Include="$(_CustomRazorGeneratorDir)Microsoft.CodeAnalysis.Razor.Compiler.dll" />
    <_RazorAnalyzer Include="$(_CustomRazorGeneratorDir)Microsoft.AspNetCore.Razor.Utilities.Shared.dll" />
    <_RazorAnalyzer Include="$(_CustomRazorGeneratorDir)Microsoft.Extensions.ObjectPool.dll" />
    <Analyzer Include="@(_RazorAnalyzer)" />
  </ItemGroup>
</Target>
```

项目还需要对 `Miko.Razor.Compiler` 的构建顺序引用：

```xml
<ProjectReference Include="..\Miko.Razor.Compiler\Miko.Razor.Compiler.csproj"
                  ReferenceOutputAssembly="false"
                  SkipGetTargetFrameworkProperties="true"
                  Private="false" />
```

### 程序化构建 DOM（无需 Razor）

```csharp
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Miko.Common;
using SkiaSharp;

// 1. 创建样式表
var styleSheet = new StyleSheet();
styleSheet.Add(new CssObject
{
    [".container"] = new()
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Padding = new Padding(Length.Px(20)),
        BackgroundColor = Color.FromRgb(245, 245, 245)
    },
    
    ["h1"] = new()
    {
        FontSize = Length.Px(32),
        FontWeight = FontWeight.Bold,
        Margin = new Margin(0, 0, Length.Px(16), 0)
    }
});

// 2. 构建 DOM 树
var root = new DivElement { Class = "container" };
root.AddChild(new H1Element { TextContent = "Hello Miko" });
root.AddChild(new ParagraphElement 
{ 
    TextContent = "A lightweight rendering engine for .NET" 
});

var button = new ButtonElement { TextContent = "Click me" };
button.OnClick = (sender, args) =>
{
    Console.WriteLine($"Button clicked at ({args.X}, {args.Y})");
};
root.AddChild(button);

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

### 增量更新与事件处理

```csharp
// 修改元素后标记为脏
element.TextContent = "Updated text";
engine.InvalidateElement(element);

// 增量渲染（只重绘脏区域）
engine.Update(canvas);

// 事件处理（多种方式）
var button = new ButtonElement { TextContent = "Click me" };

// 方式 1：使用属性
button.OnClick = (sender, args) =>
{
    Console.WriteLine($"Clicked at ({args.X}, {args.Y})");
};

// 方式 2：使用 AddEventListener
button.AddEventListener<MouseEventArgs>("click", (sender, args) =>
{
    if (args.Button == MouseButton.Left)
    {
        // 处理左键点击
    }
});

// 状态管理
button.OnMouseEnter = (sender, args) => button.SetState(ElementState.Hover);
button.OnMouseLeave = (sender, args) => button.ClearState(ElementState.Hover);
```

### 动画与过渡

```csharp
// 属性过渡
element.Style = new Style
{
    BackgroundColor = Color.FromHex("#007bff"),
    Transition = TransitionBuilder.Create()
        .Property("background-color")
        .Duration(300)
        .Easing(EasingFunction.EaseInOut)
        .Build()
};

// 触发过渡
element.Style.BackgroundColor = Color.FromHex("#0056b3");
engine.InvalidateElement(element);

// 关键帧动画
var animationManager = serviceProvider.GetRequiredService<AnimationManager>();

var fadeIn = new KeyframeAnimation
{
    Duration = 1000,
    Keyframes = new Dictionary<float, Style>
    {
        { 0f, new Style { Opacity = 0 } },
        { 1f, new Style { Opacity = 1 } }
    },
    Easing = EasingFunction.EaseOut
};

animationManager.PlayAnimation(element, fadeIn);

// 变换
element.Style = new Style
{
    Transform = Transform.Translate(100, 0)
        .Then(Transform.Rotate(45))
        .Then(Transform.Scale(1.5f))
};
```

### 使用 Ionic 组件

Ionic 组件库提供平台感知的 UI 组件：

```razor
@page "/tabs"
@using Miko.Ionic

<IonApp>
    <IonTabs>
        <IonTabButton Tab="home" @onclick="() => SelectTab(\"home\")">
            <IonIcon Name="home" />
            <IonLabel>Home</IonLabel>
        </IonTabButton>
        
        <IonTabButton Tab="settings" @onclick="() => SelectTab(\"settings\")">
            <IonIcon Name="settings" />
            <IonLabel>Settings</IonLabel>
        </IonTabButton>
    </IonTabs>
    
    @if (selectedTab == "home")
    {
        <IonContent>
            <IonList>
                <IonItem>
                    <IonLabel>Item 1</IonLabel>
                </IonItem>
            </IonList>
        </IonContent>
    }
</IonApp>

@code {
    private string selectedTab = "home";
    
    private void SelectTab(string tab)
    {
        selectedTab = tab;
        StateHasChanged();
    }
}
```

**平台模式：**
- iOS 设备：自动使用 `ios` 模式（圆角、轻盈设计）
- Android/其他：使用 `md` 模式（Material Design）
- 可通过 `IPlatformInfo` 服务自定义

### 字体管理

```csharp
// 注册自定义字体
FontManager.Instance.RegisterFont("MyFont", "/path/to/font.woff2");
FontManager.Instance.RegisterFont("MyFont", "/path/to/font-bold.ttf", bold: true);

// 使用字体
var style = new Style
{
    FontFamily = "MyFont",
    FontSize = Length.Px(16),
    FontWeight = FontWeight.Bold
};

// 内置字体回退链（自动按 Unicode 脚本选择）
// 拉丁文 → Arial/Segoe UI
// 中日韩 → Microsoft YaHei/SimSun/MS Gothic/Malgun Gothic
```

### 视频播放

```csharp
// 平台宿主注入视频后端
builder.Services.AddSingleton<IVideoBackend, MyVideoBackend>();

// 使用 VideoElement
var video = new VideoElement
{
    Source = new MediaSource { Uri = "https://example.com/video.mp4" },
    AutoPlay = true,
    Loop = true
};

// 控制播放
video.Play();
video.Pause();
video.Seek(TimeSpan.FromSeconds(10));
```

### 图像加载

```csharp
// 默认使用 ResourceManager（嵌入资源 + 文件路径）
var image = new ImageElement
{
    Source = "avares://MyApp/Assets/logo.png",  // 嵌入资源
    // 或
    Source = "/path/to/image.jpg",              // 文件路径
    // 或
    Source = "https://example.com/image.jpg"    // HTTP URL
};

// 占位符
image.Placeholder = "avares://MyApp/Assets/placeholder.png";

// 自定义加载器
builder.Services.AddSingleton<IImageLoader, MyCustomImageLoader>();
```

### 编写测试

测试文件镜像源码结构，使用 xUnit 和 Shouldly：

```csharp
using Miko.Common;
using Miko.Core.DomElements;
using Miko.Styling;
using Shouldly;
using Xunit;

namespace Miko.Tests.Styling;

public class StyleResolverTests
{
    [Fact]
    public void Should_apply_class_selector_style()
    {
        // Arrange
        var element = new DivElement { Class = "btn" };
        var styleSheet = new StyleSheet();
        styleSheet.AddRule(new ClassSelector("btn"), new Style
        {
            BackgroundColor = Color.FromHex("#007bff")
        });
        
        var resolver = new StyleResolver();
        
        // Act
        var computed = resolver.Resolve(element, new List<StyleSheet> { styleSheet });
        
        // Assert
        computed.BackgroundColor.ShouldNotBeNull();
        computed.BackgroundColor.Value.R.ShouldBe((byte)0);
        computed.BackgroundColor.Value.G.ShouldBe((byte)123);
        computed.BackgroundColor.Value.B.ShouldBe((byte)255);
    }
    
    [Theory]
    [InlineData("primary", "#007bff")]
    [InlineData("secondary", "#6c757d")]
    public void Should_apply_different_styles_based_on_class(string className, string expectedColor)
    {
        var element = new DivElement { Class = className };
        var color = Color.FromHex(expectedColor);
        
        // ... 测试逻辑
        
        computed.BackgroundColor.ShouldBe(color);
    }
}
```

**测试约定：**
- 测试类名：`[ClassName]Tests.cs`
- 测试方法命名：`Should_[期望行为]_when_[条件]` 或 `Should_[期望行为]`
- 使用 `[Fact]` 用于单个测试，`[Theory]` + `[InlineData]` 用于参数化测试
- Shouldly 断言：`result.ShouldBe(expected)`, `result.ShouldBeTrue()`, `result.ShouldNotBeNull()`

### 扩展引擎功能

**添加新元素类型：**

1. 在 `Core/DomElements/` 创建新类，继承 `Element`
2. 实现 `TagName` 属性
3. 如需特殊渲染，在 `RenderEngine.RenderBox()` 中添加处理

```csharp
public class ProgressElement : Element
{
    public override string TagName => "progress";
    
    public float Value { get; set; } = 0;
    public float Max { get; set; } = 100;
}
```

**添加新布局算法：**

1. 在 `Layout/LayoutAlgorithms/` 创建新类
2. 实现布局逻辑
3. 在 `LayoutDispatcher` 中注册

```csharp
public class GridLayout
{
    public void Layout(LayoutBox box, LayoutConstraints constraints)
    {
        // 1. 计算网格轨道尺寸
        // 2. 放置网格项
        // 3. 设置盒子最终尺寸
    }
}
```

**添加新选择器：**

1. 在 `Styling/Selectors/` 创建新类，继承 `Selector`
2. 实现 `Matches(Element)` 和 `Specificity` 属性

```csharp
public class NthLastChildSelector : Selector
{
    private readonly int _n;
    
    public override int Specificity => 10;  // 伪类特异性
    
    public override bool Matches(Element element)
    {
        if (element.Parent == null) return false;
        var index = element.Parent.Children.Count - element.Parent.Children.IndexOf(element);
        return index == _n;
    }
}
```

### 设备模拟器

使用 `Miko.Simulator` 在桌面预览移动应用：

```bash
dotnet run --project MyApp.Simulator/MyApp.Simulator.csproj
```

**功能：**
- 在独立画布中渲染应用（设备尺寸）
- 侧边设置面板（切换设备型号、旋转、安全区）
- 触摸输入模拟
- 实时调整窗口大小

## 使用示例

### 最小 Razor 应用

```csharp
// Program.cs
using Miko.Hosting;

var builder = MikoAppBuilder.CreateDefault();
builder.UseTitle("Hello Miko");
builder.UseSize(800, 600);
builder.UseGeneratedRoutes();

var app = builder.Build();
app.RunDesktop();
```

```razor
@* Pages/Home.razor *@
@page "/"

<div style="padding: 20px;">
    <h1>Hello, Miko!</h1>
    <p>Counter: @count</p>
    <button @onclick="Increment">Increment</button>
</div>

@code {
    private int count = 0;
    
    private void Increment()
    {
        count++;
        StateHasChanged();
    }
}
```

### 程序化渲染单帧到 PNG

```csharp
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;
using Miko.Common;
using SkiaSharp;

var styleSheet = new StyleSheet();
styleSheet.Add(new CssObject
{
    [".container"] = new()
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Padding = new Padding(Length.Px(20)),
        BackgroundColor = Color.FromRgb(245, 245, 245)
    }
});

var root = new DivElement { Class = "container" };
root.AddChild(new H1Element { TextContent = "Hello Miko" });
root.AddChild(new ParagraphElement { TextContent = "A lightweight rendering engine." });

using var surface = SKSurface.Create(new SKImageInfo(800, 600));
var canvas = surface.Canvas;
canvas.Clear(SKColors.White);

var engine = new MikoEngine();
engine.Initialize(root, new List<StyleSheet> { styleSheet }, canvas, 800, 600);

using var image = surface.Snapshot();
using var data = image.Encode(SKEncodedImageFormat.Png, 100);
using var stream = File.OpenWrite("output.png");
data.SaveTo(stream);
```

### 响应式布局与媒体查询

```csharp
var styleSheet = new StyleSheet();

styleSheet.Add(new CssObject
{
    // 基础样式
    [".container"] = new()
    {
        Width = Length.Percent(100),
        Padding = new Padding(Length.Px(16))
    }
});

// 平板及以上（≥768px）
styleSheet.AddMediaRule(
    new MediaCondition { MinWidth = 768 },
    new List<StyleRule>
    {
        new StyleRule
        {
            Selector = CssSelectorParser.Parse(".container"),
            Style = new Style { Padding = new Padding(Length.Px(32)) }
        }
    }
);

// 桌面（≥1024px）
styleSheet.AddMediaRule(
    new MediaCondition { MinWidth = 1024 },
    new List<StyleRule>
    {
        new StyleRule
        {
            Selector = CssSelectorParser.Parse(".container"),
            Style = new Style { MaxWidth = Length.Px(1200) }
        }
    }
);
```

### 完整交互式应用

```csharp
public class App
{
    public static MikoAppContext CreateContext()
    {
        var builder = MikoAppBuilder.CreateDefault();
        
        builder.UseTitle("Todo App");
        builder.UseSize(400, 600);
        builder.UseGeneratedRoutes();
        builder.UseDefaultLayout<MainLayout>();
        builder.AddStyleSheet(CreateStyles());
        builder.EnableHotReload();
        
        return builder.Build();
    }
    
    private static StyleSheet CreateStyles()
    {
        var sheet = new StyleSheet();
        
        sheet.Add(new CssObject
        {
            [".todo-item"] = new()
            {
                Display = Display.Flex,
                JustifyContent = JustifyContent.SpaceBetween,
                Padding = new Padding(Length.Px(12)),
                BorderBottom = new Border(1, Color.FromHex("#eee"))
            },
            
            [".todo-item:hover"] = new()
            {
                BackgroundColor = Color.FromHex("#f8f9fa")
            }
        });
        
        return sheet;
    }
}
```

```razor
@* Pages/TodoList.razor *@
@page "/todos"

<div class="container">
    <h1>Todo List</h1>
    
    <div class="input-group">
        <input type="text" @bind="newTodoText" placeholder="Add new todo..." />
        <button @onclick="AddTodo">Add</button>
    </div>
    
    <div class="todo-list">
        @foreach (var todo in todos)
        {
            <div class="todo-item">
                <label>
                    <input type="checkbox" @bind="todo.Completed" />
                    <span style="@(todo.Completed ? "text-decoration: line-through;" : "")">
                        @todo.Text
                    </span>
                </label>
                <button @onclick="() => RemoveTodo(todo)">Delete</button>
            </div>
        }
    </div>
</div>

@code {
    private string newTodoText = "";
    private List<TodoItem> todos = new();
    
    private void AddTodo()
    {
        if (!string.IsNullOrWhiteSpace(newTodoText))
        {
            todos.Add(new TodoItem { Text = newTodoText });
            newTodoText = "";
            StateHasChanged();
        }
    }
    
    private void RemoveTodo(TodoItem todo)
    {
        todos.Remove(todo);
        StateHasChanged();
    }
    
    class TodoItem
    {
        public string Text { get; set; } = "";
        public bool Completed { get; set; }
    }
}
```

## 注意事项

### 资源管理

- **SkiaSharp 资源必须正确释放**：使用 `using` 语句管理 `SKSurface`、`SKImage`、`SKBitmap` 等资源
- **字体文件加载**：`FontManager.RegisterFont()` 会立即加载字体到内存，避免重复注册
- **视频会话**：`MikoEngine` 自动管理视频会话生命周期，无需手动释放

### 类型系统

- **启用 nullable reference types**：所有 `null` 注解必须遵守，避免意外的空引用异常
- **样式属性的 null 语义**：`Style` 中的 `null` 表示"未设置"，不同于默认值，支持级联
- **浮点精度**：布局计算使用 `float`，注意浮点精度问题（特别是累积误差）

### 性能考虑

- **脏标记机制**：`IsDirty` 在 `AddChild`/`RemoveChild`/状态变更时自动设置，避免手动标记
- **增量渲染阈值**：脏区域超过 30 个时自动回退到全量渲染（可通过 `RenderEngine.MaxIncrementalDirtyRegions` 调整）
- **事件处理**：避免在高频事件（如 `OnMouseMove`）中执行耗时操作
- **样式解析缓存**：`ComputedStyle` 在布局时计算并缓存，避免重复解析

### 平台差异

- **安全区（Safe Area）**：仅移动平台支持，桌面默认为零
  - 安全区是**选择性加入**的：通过 CSS `env(safe-area-inset-*)` 声明
  - 视口本身不内缩，全屏浮层（菜单遮罩）仍覆盖整个屏幕
- **视频播放**：需要平台注入 `IVideoBackend` 实现，否则 `<video>` 元素仅显示背景/poster
- **字体回退**：不同平台的系统字体不同，使用 `FontFallbackResolver` 确保跨平台一致性
- **输入法（IME）**：移动平台支持原生软键盘，桌面依赖操作系统 IME

### Razor 编译器

- **目标框架**：`Miko.Razor.Compiler` 目标 net9.0，作为分析器 DLL 被 net10.0 项目消费
- **热重载**：仅桌面平台支持，通过 `EnableHotReload()` 和 `InitializeHotReload()` 启用
- **路由发现**：`UseGeneratedRoutes()` 自动扫描 `@page` 指令，无需手动注册路由
- **编译输出**：生成的代码在 `obj/GeneratedFiles/` 目录，可查看调试

### 线程安全

- **UI 线程**：所有 DOM 操作和渲染必须在 UI 线程执行
- **MikoDispatcher**：使用 `dispatcher.InvokeAsync()` 将操作调度到 UI 线程
- **外部失效**：视频解码等后台线程通过 `MikoEngine._pendingInvalidations` 队列投递失效请求，主线程排空

### 调试技巧

- **开发工具**：`builder.AddDevTools()` 启用运行时 DOM/布局树检查器
- **日志记录**：使用 `builder.UseLogging(config => config.AddConsole())` 启用日志
- **渲染调试**：检查 `LayoutBox` 的 `ContentBox`/`PaddingBox`/`BorderBox`/`MarginBox` 边界
- **选择器调试**：使用 `CssSelectorParser.Parse()` 验证选择器字符串解析
- **脏区域可视化**：自定义 `RenderEngine.OverlayCallback` 绘制脏区域边界

### 文档与资源

- **在线文档**：https://chaldea.github.io/miko/（VitePress 站点，位于 `docs/` 目录）
- **开发指南**：本文档（DEVELOPMENT.md）
- **项目指南**：CLAUDE.md（面向 AI 助手的项目概览）
- **示例代码**：`examples/` 目录包含 Bootstrap、Ionic、多平台、异步、媒体播放等示例
- **单元测试**：`tests/` 目录展示 API 使用模式和预期行为

## 常见问题

**Q: 如何在 Razor 中使用循环和条件？**

A: 标准 Razor 语法：
```razor
@if (condition)
{
    <div>Visible when true</div>
}

@foreach (var item in items)
{
    <div>@item.Name</div>
}
```

**Q: 如何在组件间传递数据？**

A: 使用 `[Parameter]` 属性：
```razor
@* Parent.razor *@
<ChildComponent Title="Hello" OnClick="HandleChildClick" />

@* Child.razor *@
@code {
    [Parameter] public string Title { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }
}
```

**Q: 如何实现导航？**

A: 注入 `NavigationManager`：
```razor
@inject NavigationManager Navigation

<button @onclick='() => Navigation.NavigateTo("/home")'>Go Home</button>
```

**Q: 样式不生效怎么办？**

A: 检查选择器特异性和级联顺序：
1. 使用浏览器开发工具（F12）检查计算样式
2. 确认选择器正确匹配元素（`selector.Matches(element)`）
3. 检查样式表添加顺序（后添加的优先级更高）
4. 使用内联样式测试（优先级最高）

**Q: 布局显示异常？**

A: 检查以下几点：
1. 父容器是否提供足够的空间（`LayoutConstraints`）
2. 是否设置了冲突的布局属性（如 `width: 100%` + `flex-grow: 1`）
3. 使用 DevTools 检查 `LayoutBox` 的边界框
4. 检查 `display` 属性（`none` 不参与布局）

**Q: 如何处理异步操作？**

A: 使用 `async`/`await` 和 `StateHasChanged()`：
```razor
@code {
    private string data = "";
    
    protected override async Task OnInitializedAsync()
    {
        data = await FetchDataAsync();
        StateHasChanged();
    }
}
```

**Q: 移动端和桌面端有什么区别？**

A: 主要差异：
- **输入**：移动端触摸事件，桌面端鼠标事件（统一为指针事件）
- **安全区**：移动端刘海屏/底部手势条，桌面端无
- **Ionic 模式**：iOS 使用 `ios` 模式，Android/桌面使用 `md` 模式
- **窗口管理**：桌面支持调整大小，移动端固定为屏幕尺寸

## 贡献指南

欢迎贡献！在提交 Pull Request 之前：

1. **代码风格**：遵循 `.editorconfig` 规范（强制执行）
2. **测试覆盖**：为新功能和 bug 修复添加单元测试
3. **构建验证**：确保 `dotnet build` 和 `dotnet test` 通过
4. **文档更新**：更新相关文档（README.md、DEVELOPMENT.md、CLAUDE.md）
5. **提交信息**：使用描述性的提交信息（如 `feat: add table layout`, `fix: resolve selector specificity bug`）

## 许可证

Miko 使用 MIT 许可证发布。详见 LICENSE 文件。

