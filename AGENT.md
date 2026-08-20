# Miko 开发指南

本文档是当前仓库的开发约定，供代码修改、问题排查和评审使用。内容以当前源码、测试和 `issues/` 中的修复记录为准。

## 项目定位

Miko 是使用 Razor 作为 UI DSL、使用 SkiaSharp 直接绘制的原生跨平台 .NET UI 引擎。它没有浏览器、WebView 或 HTML/CSS 运行时，核心管线为：

~~~text
Razor / C# 组件
  -> Razor 编译器与 RenderTreeBuilder
  -> Element DOM 树
  -> StyleResolver 样式级联
  -> LayoutEngine 布局树
  -> RenderEngine / Painter Skia 绘制
  -> Desktop / Simulator / Android / iOS 宿主
~~~

基本技术约束：

- 核心库目标框架为 `net10.0`，启用 nullable reference types；`Miko.Razor.Compiler` 为 `net9.0` 工具/分析器，`Miko.SourceGenerators` 为 `netstandard2.0` Roslyn 源生成器。
- UI 线程负责 DOM、样式、布局和绘制。后台任务只能通过 `MikoDispatcher`、`PostInvalidate` 等入口把工作投递回引擎。
- `Style` 的 `null` 表示“未设置”，不能当作 CSS 默认值；`ComputedStyle` 才是布局和渲染使用的最终样式。
- Skia 对象（`SKSurface`、`SKImage`、`SKBitmap`、`GRBackendRenderTarget` 等）有明确所有权，尤其是每帧创建的 GPU 对象必须在本帧释放。

## 目录与职责

| 路径 | 职责 |
| --- | --- |
| `src/Miko/Core` | `Element`、DOM 元素、`MikoEngine`、文本节点、滚动快照 |
| `src/Miko/Styling` | `Style`、`ComputedStyle`、CSS 对象、变量、媒体查询、选择器、级联和 `RuleIndex` |
| `src/Miko/Layout` | `LayoutBox`、约束、盒模型及 block/inline/flex/grid/table 算法 |
| `src/Miko/Rendering` | 脏区域、增量绘制、文本/图像/变换/堆叠上下文绘制 |
| `src/Miko/Animation` | transition、keyframe animation、动画目标迁移和回收 |
| `src/Miko/Platform` | 输入、调度、资源、视频和安全区抽象 |
| `src/Miko/Hosting` | `MikoAppBuilder`、DI、路由发现、热重载和应用上下文 |
| `src/Miko.Razor.Compiler` | Razor 语法树处理、组件生命周期、绑定、注入和代码生成 |
| `src/Miko.SourceGenerators` | `Style` 属性遍历、合并和 `ComputedStyle` 应用代码生成 |
| `src/Miko.Windowing` | Silk.NET 桌面窗口、OpenGL 上下文和桌面输入 |
| `src/Miko.Simulator` | 设备尺寸/DPI 模拟、离屏合成和设置面板 |
| `src/Miko.Android` / `src/Miko.iOS` | 移动平台宿主和原生能力适配 |
| `src/Miko.Bootstrap` / `src/Miko.Ionic` | 组件库及其样式、平台 mode 和 overlay |
| `src/Miko.DevTools` / `src/Miko.McpServer` | DOM/Layout 检查、日志面板和 MCP 调试接口 |
| `tests/*` | 核心、Ionic、Razor、MCP 回归测试；目录按功能镜像源码 |
| `examples/*` | 可运行示例和人工验证场景 |
| `benchmarks/*` | 布局、渲染、样式索引和交互帧性能基准 |
| `docs/*` | VitePress 使用文档；`issues/*` 是问题分析和修复档案 |

## 构建、测试与运行

在仓库根目录执行：

~~~bash
dotnet restore miko.slnx
dotnet build miko.slnx
dotnet test
dotnet test tests/Miko.Tests/Miko.Tests.csproj
dotnet test tests/Miko.Ionic.Tests/Miko.Ionic.Tests.csproj
dotnet test --filter "FullyQualifiedName~Miko.Tests.Layout"
dotnet test --verbosity normal
~~~

常用运行入口：

~~~bash
dotnet run --project examples/Windows/MikoAppBlank/MikoAppBlank.csproj
dotnet run --project examples/Multiplatform/MikoAppBlank/MikoAppBlank.Simulator/MikoAppBlank.Simulator.csproj
dotnet run --project examples/Ionic/IonicDemo
~~~

移动平台需要相应 workload（Android/iOS）；不要因为本机没有移动 SDK 就改动平台抽象。文档站命令为 `npm run docs:dev` 和 `npm run docs:build`。性能回归使用 `dotnet run -c Release --project benchmarks/Miko.Benchmarks/Miko.Benchmarks.csproj`。

修改前先定位最小测试项目；修改跨模块契约时再运行完整 `dotnet test`。构建生成的 Razor 文件位于 `obj/GeneratedFiles/`，源生成器输出位于 `src/Miko/obj/GeneratedFiles/`，调试生成代码时不要把这些产物提交到源码目录。

## 修改流程

1. 先读目标模块、相邻测试和相关 `issues/ISSUE-*.md`，确认问题属于 DOM、级联、布局、绘制、宿主还是组件层。
2. 先写能复现行为的测试，再修改实现。渲染问题优先断言 `LayoutBox`、`ComputedStyle`、事件状态或资源生命周期；需要视觉验证时补充 example/截图。
3. 保持所有权边界：布局算法不直接创建 Skia 资源，平台宿主不复制核心布局规则，组件库通过公共样式/API 工作。
4. 任何使元素、样式、视口、滚动或动画状态变化的操作，都要确认脏标记、布局版本和重绘请求是否正确更新。
5. 完成后运行目标测试、相关全量测试和 `dotnet build miko.slnx`；若无法运行某个平台测试，在提交说明中写明原因。

## 核心行为契约

### 样式与选择器

- 级联优先级依次考虑 `StyleSheet.Layer`、选择器 specificity、定义顺序，行内 `Style` 最高；layer 优先于 specificity。组件库使用较低 layer，让应用样式可以覆盖组件 host 样式。
- `StyleResolver` 通过 `RuleIndex` 取得候选规则，再调用完整 `Selector.Matches`。索引只能产生“超集”，不能漏规则；无法安全分桶的通配、纯伪类和分组分支必须进入 universal bucket。
- 分组选择器的每个分支只能加入一次候选；`:not()` 内部的 class/id/tag 不能被当作关键索引。修改索引后必须运行 `RuleIndexTests`，并与朴素全表扫描比较结果。
- `Element.HasClass` 位于选择器热路径，保持 CSS 空白分词的零分配实现；不要恢复 `Split`、LINQ 或每次调用创建临时数组。
- `Style`、`ComputedStyle` 的属性增删必须同步检查源生成器；不要手写一部分 `HasAnyProperty`、`Merge` 或 `FromStyle` 而漏掉新属性。必要时检查生成文件和 `CssObjectResolver`。
- CSS 变量按 DOM 继承，`var()`、`calc()`、逻辑属性、媒体查询和 `env(safe-area-inset-*)` 在计算样式阶段解析；不要在布局算法中重复解析。

### 布局、文本与盒模型

- 约束自上而下传递，尺寸自下而上汇总。`display:none` 不进入布局树；绝对/固定定位元素脱离普通流，不应撑大父元素内容尺寸。
- 百分比尺寸针对 indefinite（未确定）的父尺寸时按 CSS 规则退化为 `auto`，不能解析为 0；留意 `Length.HasPercentComponent` 和 block/inline/flex 三处处理。
- `min-width`/`max-width`/`min-height`/`max-height` 在最终尺寸钳制阶段生效；`auto` margin、flex shrink/grow、百分比宽度和滚动条占用必须在同一约束模型中计算。
- block、inline、inline-block、inline-flex、flex、grid、table 使用各自算法。新增属性必须同时更新 `Style`、`ComputedStyle`、布局分发、默认样式和测试。
- `white-space` 语义必须遵守：`normal` 折叠空白并换行，`nowrap` 折叠但不换行，`pre` 保留且不换行，`pre-wrap` 保留并换行，`pre-line` 折叠空格但保留换行。文本高度应使用实际行数和显式/自然 `line-height`。
- 文本节点和 `Element.TextContent` 参与布局；flex 中直接文本视为匿名 flex item，使用 `LayoutBox.TextContentOffsetX/Y` 保存对齐偏移。不要在 `text-align` 和 flex 对齐之间重复施加偏移。
- 当前 `Element.TextContent` 不能表达 `text1 + child + text2` 的交错顺序；需要此能力时先提出 TextNode 架构变更，不要在现有字符串上添加脆弱的特殊解析。

### 渲染、动画与资源

- 修改 DOM、样式、文本、状态、滚动或输入后应通过现有 invalidation 机制触发布局/绘制；不要无条件调用全量 `Render`。
- 单引擎宿主用 `MikoEngine.HasPendingVisualWork` 判断是否需要新帧；同进程次级引擎（DevTools）不能使用它，因为 `Element.MutationVersion` 是全局静态版本号，应使用 `HasPendingRenderWork` 并自行判断 DOM 是否需要重建。
- Silk 宿主关闭自动交换（`ShouldSwapAutomatically = false`），只有实际绘制后才 `SwapBuffers`。跳帧时必须维护待呈现计数，避免后备缓冲闪烁。
- 每帧创建的 `GRBackendRenderTarget` 和 `SKSurface` 必须 `using`/`Dispose`；必要时设置合理的 `GRContext` 资源缓存上限。宿主退出时释放 GL、输入上下文、surface 和 context。
- 模拟器高 DPI 离屏 surface 使用 `SKSurfaceProperties`；缩放合成使用高质量采样（当前为 Mitchell cubic）。SVG 栅格化和位图绘制要启用抗锯齿/高质量采样。
- 动画条目以逻辑元素身份迁移到 `SupersededBy` 新实例；迁移发生在过渡检测前，随后回收脱离 DOM 或已撤下声明的动画。组件回调触发 `StateHasChanged` 不应重置正在运行的动画。
- 图片通过 DI 管理的 `ResourceManager` 加载，支持 `file://`、`res://`、HTTP(S) 和 data URI。多平台嵌入资源先用 `AddResourceAssembly` 注册；相对文件路径基于 `AppContext.BaseDirectory` 解析。视频必须由平台注入 `IVideoBackend`，核心层不能依赖 FFmpeg 或原生控件。

### 路由、滚动与平台

- `NavigationTransitionInfo` 即使没有动画也必须传递；它描述导航方向和路径，`Transition` 可以为 null。
- 同路径重建（状态更新、热重载、无限列表追加）可以使用宽松的子序列结构匹配恢复稳定容器滚动；跨页面导航必须使用严格结构匹配，防止把上一页滚动位置带到新页面。
- `ScrollSnapshotStore` 按路由路径保存布局树索引路径和末端标签名。`Back` 回放并消费快照，`Forward` 从顶部开始，`Root` 清空快照；回放时按当前可滚动范围钳制。
- safe area 是可选的 `env()` 输入，不是根 viewport 的默认内缩；overlay/menu 必须仍能覆盖完整窗口。Ionic mode 由平台信息决定（iOS 为 `ios`，其他平台通常为 `md`）。

### Razor、组件与事件

- Razor 组件通过自定义编译器生成 `RenderTreeBuilder` 调用；组件更新必须维护 sequence、fragment 顺序、`ChildContent`、`[Parameter]`、`[CascadingParameter]` 和 `[Inject]` 语义。
- 事件回调完成后应让组件状态进入正常重建流程；异步处理使用 `async`/`await`，跨线程更新通过 dispatcher，必要时调用 `StateHasChanged`。
- `@bind` 必须同时验证 value 属性和对应 change callback；表单 input/select/range 的默认尺寸、文本垂直对齐、光标和事件行为应有专门测试。
- 路由优先使用编译期生成的注册表（`UseGeneratedRoutes`），不要重新引入运行时程序集扫描，以保持 trimming/AOT 兼容。

## 测试重点

测试文件按源码模块放置，命名使用 `Should_...`，断言使用 Shouldly。高风险回归至少覆盖：

- `tests/Miko.Tests/Layout`：百分比 indefinite、auto margin、min/max、flex/grid/table、white-space、line-height、overflow、滚动和循环依赖。
- `tests/Miko.Tests/Styling`：级联 layer/specificity、变量、calc、逻辑属性、规则索引与朴素扫描等价性。
- `tests/Miko.Tests/Rendering`：文本/图片抗锯齿、变换、堆叠上下文、脏区域和边框/圆角。
- `tests/Miko.Tests/Components`、`tests/Miko.Razor.Tests`：生命周期、RenderFragment、事件回调、绑定、注入和 Razor whitespace。
- `tests/Miko.Tests/Core`、`tests/Miko.Tests/Routing`、`tests/Miko.Tests/Platform`：事件命中、输入、导航转场、滚动快照、资源和视频生命周期。
- `tests/Miko.Ionic.Tests`：组件 mode、host/native 结构、overlay、segment、picker、滚动和组件样式。
- 性能改动同时运行 `benchmarks/Miko.Benchmarks`，关注每帧分配、Gen0/Gen2、帧耗时和候选规则数量，不只看平均吞吐。

## Issue 修复索引

`issues/` 是问题背景、根因和验证记录的索引，不是待办列表。以下是当前实现中仍需遵守的修复主题：

| 主题 | 代表记录 | 开发时要点 |
| --- | --- | --- |
| Razor/组件/路由基础 | `ISSUE-007`, `016`, `032`, `049`, `060`, `065`, `066`, `069`, `072`, `100`, `115` | 编译期路由、fragment 顺序、生命周期、DI、绑定和事件更新不能回退到运行时猜测 |
| 样式与 CSS 语义 | `ISSUE-010`, `017`, `023`, `033`, `038`, `039`, `080`, `082`, `088`, `090`, `103`, `107`, `113` | layer/cascade、变量/calc、生成器、规则索引和零分配热路径必须保持等价 |
| 布局与文本 | `ISSUE-024`, `027`, `034`, `037`, `040-042`, `070`, `075-079`, `081`, `085-086`, `093-094`, `097-099`, `102`, `105-106`, `109-110`, `116`, `122`, `124-126` | 先修复约束和布局树，再处理绘制；必须添加针对实际 display 默认值的回归测试 |
| 渲染/动画/性能 | `ISSUE-014`, `028-029`, `035-036`, `043-044`, `073`, `085`, `096`, `108`, `111`, `113`, `117`, `127` | 脏区、跳帧、Skia 释放、DPI 采样、动画迁移和次级引擎空闲判据是性能与正确性的共同契约 |
| 平台/资源/媒体 | `ISSUE-001`, `050-059`, `062-063`, `067-068`, `071`, `074`, `083` | 平台能力注入核心，资源 URI 和程序集显式注册，视频/图片生命周期不可阻塞 UI |
| 导航/滚动/overlay | `ISSUE-003-006`, `054`, `092`, `104`, `112`, `118`, `120` | 区分同树重建、Forward、Back、Root；safe area 和 overlay 不能互相破坏 |
| Ionic/Bootstrap 组件 | `ISSUE-011`, `015`, `030`, `052`, `064`, `068` 及 `issues/ion-*.md` | 组件结构、mode、host 样式和交互状态要与现有测试及样式工厂一致 |

问题记录中的 `*-RESOLUTION.md`、`*-SUMMARY.md` 和 benchmark 报告适合追溯根因与验证数据；修复新问题时应链接对应 issue，并在代码/测试中固化行为，而不是只更新说明文字。

## 提交前检查

- [ ] 新行为有最小回归测试，测试不依赖错误的默认 `display` 或只覆盖理想化 DOM。
- [ ] DOM/布局/渲染/动画/资源对象的生命周期和线程边界清晰。
- [ ] 规则索引与全表匹配结果一致；性能优化没有改变级联顺序。
- [ ] 涉及导航时验证 Forward 从顶部、Back 恢复、Root 清空，以及稳定 Layout 容器仍能保留滚动。
- [ ] `dotnet build miko.slnx`、受影响测试项目和必要的 benchmark 已执行并记录结果。
- [ ] 仅提交源码、测试、文档和必要资源；不提交 `bin/`、`obj/`、生成文件或本地 IDE 状态。

