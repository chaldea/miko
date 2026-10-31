# MikoPlayer 实现方案

## 边界

Miko 是原生 Skia UI 引擎。播放器复用 `VideoElement` 与 `IVideoSession`，不引入浏览器、DPlayer 或 JavaScript。`Miko.Components` 只引用核心引擎，绘制只经 `Miko.Graphics` 抽象（`IDrawingContext` / `IGraphicsFactory`），不引用 SkiaSharp（ISSUE-162）；`AddMikoPlayer()` 仅注册播放器配置。系统视频后端仍由宿主选择。

## 结构

- `Player/MikoPlayer`：稳定的元素树、触屏控制栏、设置、错误重试和视口全屏。公开播放、暂停、定位、音量、静音和倍速 API。
- `Player/PlayerController`：绑定引擎拥有的视频会话；向 UI 提供状态快照，隔离后台事件与 UI 线程，保留加载期间的操作意图。
- `Player/Danmaku`：按时间排序、二分定位、限制同时可见数量；滚动、顶部、底部三种模式。按媒体时间计算位置，暂停、倍速、定位和循环自然同步。轨道检查避免追尾；拥塞时丢弃过期弹幕，不累积队列。
- 引擎扩展：公开只读视频会话及会话绑定通知；允许自定义元素在内容盒中绘制，沿用布局、裁剪、变换和透明度；绘制失效不触发布局。

## 媒体管线

HLS 播放列表、分段、直播刷新和音视频同步由平台媒体库负责，不自行下载拼接 m3u8。Windows 需要完整媒体播放管线，因为现有 SourceReader 只输出视频，无音频且不能保证 HLS。其他平台保留系统后端，能力差异明确反映在组件状态与文档中。

控制器接收真实缓冲和错误信息；未报告缓冲区范围的后端不显示虚构的缓存进度。未知时长显示直播状态并禁止按总时长拖动。源改变和卸载时释放事件订阅及绘制资源缓存，后台过期事件不得修改新会话。

## UI 与性能

控制栏包含播放/暂停、进度拖动、时间、静音、设置、弹幕开关和全屏。设置中提供音量、倍速、循环、弹幕透明度、字号、速度、区域和密度。按钮触摸目标至少 44px，窄屏将次要设置收纳进设置面板。

弹幕只占一个绘制元素，不为每条弹幕创建 DOM 节点。文本尺寸/绘制资源缓存有明确上限；每帧只遍历可见集合。常规进度文本限频更新；连续动画只请求重绘，不重建 Razor 树。

## 验证

先增加 fake 后端测试，覆盖加载时操作、暂停、重播、定位边界、失败重试、换源、卸载与事件线程。弹幕测试覆盖时间跳转、循环、拥塞和同时可见上限。使用软件 Skia 在桌面/手机尺寸验证布局和实际像素；运行组件及核心回归测试并构建示例、解决方案。平台真机验证范围在完成记录中明确说明。

## 实施与验证记录（2026-09-07）

- 已新增独立 `Miko.Components`、桌面 Razor 示例和 Android 示例；系统后端分别使用 Windows Media Engine、Android MediaPlayer、Linux playbin 和 Apple AVPlayer。没有新增第三方原生解码器。
- 控制栏、缓冲提示、失败重试、设置、视口全屏及三种弹幕模式已实现。暂停、跳转、倍速和循环均使用媒体时间同步弹幕；设置与全屏操作保留原视频会话。
- `dotnet build miko.slnx` 成功；播放器 20 项测试通过。覆盖真实输入分发器触屏拖动、下拉选项与滑块重叠命中、窄屏布局、带 padding 父容器下全屏、后台事件隔离、错误状态保持、换源、MIME 变更和卸载释放。
- 全量共 3,327 项：核心 1,842、Ionic 1,434、Razor 12、MCP 19、播放器 20，最终各项目均通过。复测期间既有 `EventCallbackAsyncTests` 和 `IonLoadingAutoDismissTests` 出现间歇性时序失败；单独复测对应 4 项和 7 项、随后核心和 Ionic 整个项目均通过。此任务未修改这些用例及其定时器实现。
- 稳态弹幕时间线测试确认每帧托管分配为 0；可见数量、追赶扫描数量和文本缓存均有上限。暂停后引擎进入空闲状态，不持续请求重绘。
- Windows 实际系统解码：本地 MP4 和公网 HLS 均获取到有效视频帧；桌面、手机尺寸、设置和全屏截图已检查；暂停后 `HasPendingVisualWork` 为 false。
- Android API 36 x86_64 模拟器：安装并运行 APK，验证本地 MP4、HLS、暂停、进度拖动、音量、倍速菜单、弹幕开关和全屏；截图确认视频及弹幕可见、全屏铺满视口，暂停前后采样区域像素不变。
- Android 暂停定位使用 SurfaceTexture 实际到帧通知更新预览，避免 `SeekComplete` 先到而停留在旧帧；模拟器已验证时间和目标帧一致。API 26 及以上使用最近帧定位。
- Linux、macOS、iOS 仅完成编译验证，本机无对应运行环境。系统媒体功能依赖设备支持的编码格式；Linux 还需要 GStreamer HLS/音频插件。
- 全屏为宿主视口全屏，不隐藏系统栏或切换操作系统窗口模式。Windows 当前通过 D3D11 staging 读回 BGRA 帧交给 Skia，尚非 GPU 零拷贝。构建仍有仓库已有的平台 API 警告；Android 所用 HarfBuzz 原生包仍报告 16 KB 页对齐警告。

## 视口纵横比（ISSUE-150，2026-10-15）

- 非全屏视口改用 `aspect-ratio: 16 / 9` 定高，由布局在同一遍内按宽度推导。此前首帧以 240px 占位，`PlaybackUpdated` 拿到上一帧的 `OffsetWidth` 后写回像素高度：打开页面要排两遍布局，每次改宽（旋转、窗口缩放）都要多排一遍且滞后一帧。
- 实测（`MikoInteractionController.RenderFrame` + `FrameProfiler`，会话尚未报告尺寸，与系统解码器一致）：打开页面那一帧布局 2 遍 → 1 遍，与无播放器的纯 div 树相同；旋转 2 帧布局 → 1 帧。
- 全屏仍为 `flex-grow: 1; height: 0`，铺满控制栏以外的高度。

## 绘制抽象（ISSUE-162，2026-10-29）

- 核心库不再向外暴露 Skia：`CanvasElement.Paint` 改为 `Action<IDrawingContext, RectF>`，`DanmakuItem.Color` 改为 `Miko.Common.Color?`。进度条、缓冲转圈、图标与弹幕全部改用 `IDrawingContext` 的图元；图标经 `IGraphicsFactory.DecodeImage` 解码为矢量图，以 `tint` 着白色；弹幕文本经 `IGraphicsFactory.CreateText` 排版。`Miko.Components` 已无 SkiaSharp / Svg.Skia 引用，核心库的 Skia 包对外只提供运行时资产，回到旧写法会编译失败。
- 弹幕文本改走引擎的文本管线（字体回退 + 双向 + 整形）后，修复了一个旧缺陷：此前整条弹幕只取一个字体（按首个非 ASCII 字符选），`日本語のコメント` 的假名显示为方框，`Scrolling comment 弹幕` 的拉丁字母用的是 CJK 字体。字形改为子像素定位，与页面其它文本一致，滚动时更平滑。
- 回归验证（同一组固定状态逐像素对比改动前后）：控制栏（进度、缓冲区间、全部图标、时间文本）、视频帧、空闲 / 错误 / 直播 / 暂停各状态逐像素一致；弹幕只有抗锯齿级差异（字体、字号、位置一致）。macOS 桌面 Metal 与 OpenGL 窗口、Android 模拟器（OES 零拷贝，暂停 / 全屏横屏 / 返回 / 切后台恢复）、iOS 模拟器（Metal 零拷贝）实机播放正常。

## 覆盖式控制栏、手势与沉浸式全屏（issues/feat-components-player.md 问题列表，2026-10-31）

- **全屏没有铺满（问题 1）。** targetSdk 36 的应用在 Android 15+ 上本就 edge-to-edge，灰带并非窗口没铺满，而是播放器把安全区写成了自己的 padding（背景 `#151719`），状态栏也一直显示在上面。引擎新增组件面向的 `IPlatformService.SystemBars`（`ISystemBarsService.Hide/Show`，引用计数）：Android 用 `WindowInsetsController` 隐藏状态栏与导航栏（边缘滑动临时呼出），API 28+ 设 `LAYOUT_IN_DISPLAY_CUTOUT_MODE_SHORT_EDGES`，API 26–29 走 `IMMERSIVE_STICKY` 标志位且只撤销自己加的位；iOS 由 `MikoViewController` 声明隐藏状态栏、自动隐藏 Home 指示条；桌面与模拟器为空实现。全屏时根节点不再有 padding，画面铺满整屏（刘海区与黑边一样是黑的），只有控制栏、提示和设置抽屉用 `env(safe-area-inset-*)` 避开刘海。
- **控制栏挤占画面（问题 2）。** 播放器本身就是画面：内联为宽度的 16:9（`aspect-ratio` 移到根节点，`Style` 给出的高度优先），全屏为整屏；进度条与按钮行叠在画面底部，下面垫一块由透明到 60% 黑的渐变（绘制接口没有渐变，用 2px 色带画）。隐藏时 `opacity: 0` 渐隐 0.2s 并 `pointer-events: none`，点击穿透到画面。
- **手势。** 触屏单击在 300ms 双击窗口过后切换控制栏，双击播放/暂停；鼠标单击立即播放/暂停、移动鼠标呼出控制栏。播放中 `ControlsHideDelay`（默认 5s）后自动隐藏；暂停、结束、出错时自动显示；打开设置、拖动进度时不隐藏。控制栏空白处（时间文本、按钮间隙）按画面处理。计时器用注入的 `TimeProvider`，回调投递到 `MikoEngine.Dispatcher`（新公开，供未经 DI 注入的组件使用），在下一帧的渲染线程上执行并按代号丢弃过期回调——测试推进手动时钟，不依赖真实时间。
- **设置面板（问题 3）。** 按平台选择呈现：Android/iOS（含模拟器所模拟的手机）为右侧半透明抽屉，300px 宽、整高、44px 触控行，打开时让出控制栏；桌面为设置按钮上方的半透明弹出菜单，高度随内容、最多到画面顶部，外层不接收点击。点画面、返回键（Overlay 优先级，先于退出全屏）或 Escape 关闭。原生 `<select>` / 复选框 / range 换成为深色半透明面板绘制的倍速芯片、开关和画布滑块（拖动时阻止默认行为，不会滚动面板）。
- **长按倍速（问题 4）。** 引擎的 `longpress`（500ms 未移动）在播放中触发：记下当前倍速，切到 `LongPressPlaybackRate`（默认 2，限制在 0.25–4，0 关闭），顶部显示「» 2x」半透明提示，`Haptics.Impact(Light)` 震动一次；`pointerup` / `pointercancel` 恢复原倍速。长按期间 `pointermove` 阻止默认行为，页面不会滚动；引擎在长按后不再派发 click。长按中途父组件改了 `PlaybackRate`，松手后生效。
- **验证。** 播放器测试 62 项（新增 28 项，主要是 `PlayerGestureTests`：单/双击、点击穿透隐藏的控制栏、自动隐藏、鼠标、长按、两种设置呈现、开关与滑块经真实输入控制器、全屏刘海内边距、系统栏的获取与释放）。逐一回退点击穿透、刘海内边距、触屏单击语义、长按倍速与震动、隐藏系统栏这几处修复，对应用例都会变红。Android API 36 模拟器（开启 tall 刘海仿真）：全屏后 `dumpsys window` 显示状态栏与导航栏 `visible=false`、画面延伸进刘海、控制栏避开刘海；单击呼出、5s 自动隐藏、右侧设置抽屉、点画面关闭、长按显示「» 2x」并在 `vibrator_manager` 记录一次 `performHapticFeedback(constant=1)`、松手恢复、双击暂停、返回键退出全屏并恢复系统栏。iOS 仅完成编译。
