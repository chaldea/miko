# Miko Anime 场景示例

依据 `issues/example-anime.md` 及其截图实现，使用仓库 `examples/Multiplatform/MikoAppTabs` 对应的多平台三 Tabs 模板结构（模板源位于 `templates/MikoMultiplatformApp`）。

## 结构与运行

```text
examples/App/
├── App.slnx
└── Anime/
    ├── Anime/                 共享 Razor 页面、组件、样式、服务接口与嵌入资源
    ├── Anime.Desktop/         Windows / Linux / macOS 桌面宿主
    ├── Anime.Simulator/       设备、方向、安全区模拟器
    ├── Anime.Android/         Android 原生宿主
    ├── Anime.iOS/             iOS 原生宿主
    ├── Anime.Verification/    实际应用的渲染与交互验证程序
    └── prepare-assets.ps1     从 issue 截图重新生成本地图像资源
```

在仓库根目录执行，需要 .NET 10 SDK：

```bash
# 优先验证的 Windows 入口，初始内容区域为 390 × 844
dotnet run --project examples/App/Anime/Anime.Desktop

# 多设备预览，可切换 iOS / Android 样式和横竖屏
dotnet run --project examples/App/Anime/Anime.Simulator

# Xcode 27 / iOS 27 模拟器（可替换为本机的模拟器名称或 UUID）
dotnet run --project examples/App/Anime/Anime.iOS --device "iPhone 17"

# 包含所有平台项目；移动端需要相应 workload、SDK
dotnet build examples/App/App.slnx
```

Android 通过 Android workload 构建并部署到模拟器或设备。iOS 设备打包、签名和运行需要 macOS / Xcode。Windows 上的 iOS 编译通过不代表已经验证设备运行。

Anime.iOS 使用 `net10.0-ios27.0` 和 Xcode 27 对应的 .NET iOS 工具包，最低支持 iOS 15；当前工具包需要显式启用 `XCODE_27_0_PREVIEW`。窗口由 `SceneDelegate` 按 UIKit 场景生命周期创建，并注册 iOS 原生视频后端。渲染走 `Miko.iOS` 的 Metal 宿主 `MikoMetalView`，在模拟器中使用宿主 Mac 的 GPU（ISSUE-147）。可用 `xcrun simctl list devices available` 查看模拟器名称或 UUID。

升级 SDK 后若遇到 AOT 模块依赖 GUID 不匹配，先执行一次 `dotnet build examples/App/Anime/Anime.iOS -t:Rebuild`，再运行上述命令。未连接 IDE 时出现的调试端口 `10000` 连接提示不影响启动。

## 页面与组件

`MainLayout.razor` 保留标准 `IonApp → IonTabs → IonTabBar / IonTabButton` 结构，三个根页面是首页、福利、我的。详情、排行、排期、资料、收藏、历史使用独立路由和 Ionic 返回按钮。

页面使用 `IonPage`、`IonHeader`、`IonToolbar`、`IonContent`、`IonFooter`、`IonGrid`、`IonCard`、`IonList`、`IonItem`、`IonSegment`、`IonInput`、`IonSearchbar`、`IonModal` 等现有组件。应用不手写 `RenderTreeBuilder` 或 `OpenElement`；补充布局通过集中维护的 C# 样式表表达。

播放器使用 `Miko.Components.Player.MikoPlayer`，提供播放、暂停、进度、声音、速度、全屏和弹幕。`PlaybackSurface` 仅转接详情页持有的播放器实例，避免输入评论、弹幕或切换详情选项卡时重建原生视频会话；离开详情时释放播放器。Desktop / Simulator 使用 `UseSystemVideo`，Android 使用 `UseAndroidVideo`，iOS 使用 `UseIosVideo`。

## Mock 服务与资源

页面只依赖以下接口，默认实现通过 `App.CreateContext` 中的 DI 注册：

| 接口 | 职责 |
| --- | --- |
| `IAnimeCatalog` | 首页推荐、分类、搜索、类型/年份/评分筛选、季度排行、星期排期 |
| `IUserLibrary` | 收藏、观看历史、选集进度、资料修改、演示登录状态 |
| `ICommunityService` | 评论及带颜色、模式、视频时间戳的弹幕 |
| `IRewardsService` | 每日任务次数、金币余额、下载额度、兑换记录 |
| `IMediaAssets` | 获取系统解码器可读取的本地演示视频 |

`MockAnimeStore` 从嵌入的 `Assets/catalog.json` 加载初始数据。JSON 使用源生成序列化上下文，兼容移动端 trimming/AOT。业务状态保存在内存，重启后恢复初始数据；登录、广告奖励、兑换和下载额度均为本地模拟，不提供账号认证、真实广告或下载功能。所有番剧共用仓库 Media 示例中的短视频，以便离线验证原生播放与弹幕。

更换后端时，在 `CreateContext` 的配置回调中替换相应接口的 DI 注册。页面不引用 `MockAnimeStore`，不直接读取 JSON。

海报、横幅和头像裁剪自本任务提供的 `issues/example-anime/*.jpg`，仅作为场景示例素材；海报与横向缩略图分别裁剪，避免共用比例。`sample.mp4` 来自 `examples/Media/MikoApp.Media/Assets/miko-local.mp4`。所有资源随共享程序集嵌入，运行不依赖网络或当前工作目录。Windows 下可执行 `examples/App/Anime/prepare-assets.ps1` 复现裁剪结果。

## 验证

```bash
dotnet run --project examples/App/Anime/Anime.Verification
dotnet run --project examples/App/Anime/Anime.Verification -- artifacts/anime/verification-ios --ios
```

程序加载真实共享应用，使用 Skia 渲染和平台输入控制器点击、输入，失败时抛出异常并返回非零状态。覆盖三 Tabs 导航、搜索与筛选、资料校验/保存、收藏/历史删除、奖励上限与兑换、选集、评论、彩色弹幕、横屏全屏、详情内推荐切换及视频会话的保留与释放。

每次生成 19 张 PNG 和对应布局记录，默认输出到 `artifacts/anime/verification`；视口包含 `390×844`、`320×740` 与横屏 `844×390`。`--ios` 验证 Ionic 的 iOS 样式，视频仍由当前桌面系统解码。截图与日志为本地产物，不提交到源码。

本次还运行了 Windows 桌面窗口，并检查了 Material / iOS 场景截图。Desktop、Simulator、Android、iOS 及仓库主解决方案已通过编译；移动端存在基础库与依赖的 API/原生库警告，尚未做 Android / iOS 真机验证。

2026-09-23：Anime.iOS 已在 Xcode 27 下完整重建通过，并使用 `dotnet run` 在 iPhone 17 / iOS 27 ARM64 模拟器中启动，确认首页内容和底部标签栏正常显示。本次验证覆盖构建与首页启动，未验证真机签名及原生视频播放。迁移到 Metal 宿主后，模拟器中的原生视频播放已通过下文探针的 `video` 场景验证，真机仍未验证。

### iOS 帧延迟探针

Anime.iOS 已接入共享 `FrameProbe`，默认关闭。先构建并安装应用，然后用以下命令开启探针，手动滚动首页即可在终端查看耗时：

```bash
dotnet build examples/App/Anime/Anime.iOS -t:Rebuild
xcrun simctl install booted examples/App/Anime/Anime.iOS/bin/Debug/net10.0-ios27.0/iossimulator-arm64/Anime.iOS.app
SIMCTL_CHILD_MIKO_FRAME_PROBE=1 xcrun simctl launch --console --terminate-running-process booted com.miko.anime
```

设置 `SIMCTL_CHILD_MIKO_FRAME_PROBE=verbose` 可逐帧输出。`[frame-probe]` 是引擎内部的构建、样式、布局、绘制耗时；`[ios-draw]` 是 `MikoMetalView` 一次绘制的 CPU 耗时，包含取可绘制纹理（GPU 落后时会在此等待）、引擎帧、Skia flush 与呈现提交，不包含随后的 GPU 执行和系统显示合成；`[ios-tick]` 是主线程收到显示回调的间隔。`interval` 包含主动停帧的空闲时间，不能把静止页面的低出帧数视为性能问题。初始 DOM 构建也不在共享探针的 `RenderFrame` 计时范围内。

可执行约 37 秒的自动对照场景：启动预热、持续重绘首页、8 次上下交替拖动及惯性滚动、等待。自动输入通过与 iOS 触摸相同的引擎入口，在主线程注入；它不测量操作系统触摸事件投递延迟。测量日志先缓存在内存，结束后统一输出，避免终端逐行输出干扰帧耗时。`[ios-phase]` 记录阶段和滚动位置，`[ios-renderer]` 记录 Metal 设备及可绘制纹理的像素尺寸，`[ios-summary]` 按阶段汇总帧数、帧间隔中位数对应的 FPS 与绘制耗时（帧间隔只统计同一阶段内相邻两帧，惯性停止后的空闲不计入）。

```bash
SIMCTL_CHILD_MIKO_FRAME_PROBE=verbose SIMCTL_CHILD_MIKO_IOS_PROBE_SCENARIO=scroll \
  xcrun simctl launch --console --terminate-running-process booted com.miko.anime > /tmp/anime-ios-frames.log 2>&1
```

把场景换成 `video` 则执行约 15 秒的视频对照：启动 3 秒后打开第一部番剧的详情页，播放器自动静音循环播放内置视频，经 iOS 原生视频后端（AVPlayer + `CVMetalTextureCache` 零拷贝）出帧。

出现 `[ios-probe] complete` 后本轮测量完成，应用继续运行；重新以不带这些环境变量的命令启动即可恢复正常配置。

2026-09-23 的 iPhone 17 / iOS 27 模拟器实测中，原 OpenGL 宿主的渲染器标识为 `Apple Software Renderer`。默认 Debug 首页持续重绘时，引擎内部平均约 1.1 ms，含 Skia flush 的 GL 绘制约 594 ms，出帧约 1.6 FPS；Release 仍约 1.6 FPS；仅关闭 4× MSAA 后约 6 FPS。原生采样定位到 `SKCanvas.Flush → OpenGLES → GLRendererFloat`，即模拟器的 OpenGL ES 没有硬件加速。

同日迁移到 Metal（ISSUE-147）后复测，`[ios-renderer]` 为 `Apple iOS simulator GPU`、`1206x2622`，单采样。Debug 下 `scroll` 场景各阶段（持续重绘、拖动、惯性）均稳定在显示上限 60 FPS，帧间隔 p95 约 16.8 ms，`[ios-draw]` 平均 4–6 ms；首帧（初始化）约 170–230 ms。首次运行时第一次拖动出现过一帧约 280 ms，耗时在 Skia flush 而非引擎，是新绘制组合首次编译 Metal 管线；系统着色器缓存生效后再次运行不再出现（拖动阶段最大约 14 ms）。`video` 场景确认走零拷贝路径、画面色彩正确，播放期间 60 FPS、绘制平均约 5.8 ms；打开详情页那一帧约 510 ms，其中引擎重建约 200 ms。播放中约每秒一帧出现 70–80 ms 的样式重算（`[frame-probe]` 中 `style` 约 68 ms），属于引擎 CPU 开销，与 GPU 后端无关。以上为模拟器数据，不代表真机帧率。

### 桌面帧延迟探针与 macOS 渲染路径

Anime.Desktop 接入了桌面探针 `DesktopFrameProbe`，默认关闭，用于在同一场景下对比桌面呈现后端（ISSUE-147）。macOS 上默认走 Metal，设置 `MIKO_GRAPHICS_BACKEND=opengl` 可退回 OpenGL 做对照：

```bash
dotnet build examples/App/Anime/Anime.Desktop
MIKO_DESKTOP_PROBE_SCENARIO=scroll dotnet run --no-build --project examples/App/Anime/Anime.Desktop
MIKO_GRAPHICS_BACKEND=opengl MIKO_DESKTOP_PROBE_SCENARIO=video dotnet run --no-build --project examples/App/Anime/Anime.Desktop
```

`scroll` 场景约 37 秒：启动、持续重绘、8 次交替滚轮滚动、等待；`video` 场景约 15 秒：打开第一部番剧的详情页，测量静音循环播放。自动输入经 `MikoDispatcher` 投递到渲染线程，与真实窗口输入同一线程。`[desktop-summary]` 按阶段汇总帧数、帧间隔中位数对应的 FPS 与引擎帧耗时；`frame` 是引擎整帧（含宿主清屏与 Skia flush），帧间隔另含呈现与垂直同步等待。`MIKO_FRAME_PROBE=verbose` 逐帧输出。场景结束后探针关闭窗口、进程正常退出，因此每次运行也覆盖宿主的关闭与资源释放。

2026-09-24 在 macOS 27 / Apple M6（1920×1080 60 Hz 外接屏，Debug）实测。迁移前桌面宿主经 GLFW 使用 OpenGL，渲染器为 `Apple M6`、版本 `4.1 Metal - 91.7`，即 Apple 在 Metal 之上实现的 OpenGL 转换层，有硬件加速，不存在 iOS 模拟器那样的软件渲染问题。同一探针下两种后端各运行 3–6 次：

| 场景 | OpenGL | Metal |
| --- | --- | --- |
| 持续重绘 | 59.3–59.7 FPS，帧 2.1–3.2 ms | 59.3–59.8 FPS，帧 2.4–3.1 ms |
| 滚轮滚动 | 59.3–59.9 FPS，帧 2.1–4.0 ms | 59.0–59.9 FPS，帧 3.1–3.9 ms |
| 视频播放 | 62–124 FPS，逐帧 CPU 拷贝上传 | 59.8–59.9 FPS，帧 3.2–3.5 ms，零拷贝 |
| 打开详情页的重建帧 | 81–113 ms | 87–97 ms |

静态页面与滚动两者持平。差别在视频：OpenGL 的呈现在本机没有稳定地按垂直同步节流，多数运行以约两倍于屏幕刷新率出帧，一半的帧屏幕根本显示不出来（帧转储强制连续呈现时甚至达到约 1800 FPS）；Metal 由 `nextDrawable` 提供背压，每次都稳定在 60 FPS。重建帧主要是引擎自身的构建、样式与布局（约 45 + 10 + 13 ms），与后端无关。首次运行 Metal 时第一次滚动与打开详情页各有一帧较慢（约 80 ms、约 420 ms），是 Metal 管线首次编译，系统着色器缓存生效后不再出现，与 iOS 上的观察一致。连续呈现 70 秒与连续播放 75 秒，两种后端的常驻内存均保持平稳（Metal 254 MB、视频 275→277 MB）。Metal 与 OpenGL 的帧转储逐像素比较，最大通道差为 2/255。

结论：macOS 同步迁移到 Metal，理由不在帧率，而在于：macOS 的 OpenGL 自 10.14 起已弃用、停在 4.1，且只是 Metal 上的转换层；视频可以像 iOS 一样经 `CVMetalTextureCache` 零拷贝，此前 macOS 一直逐帧 CPU 拷贝；Retina 屏按 backing scale 出图；模拟器在 macOS 上预览 iOS 界面时与真机走同一条 Skia Metal 渲染路径。Windows / Linux 仍为 OpenGL。本机只有 1× 外接屏，Retina 未经实机验证，仅以临时强制 backing scale = 2 验证过 2× 出图与缩放。
