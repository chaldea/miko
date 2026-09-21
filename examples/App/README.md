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

# 包含所有平台项目；移动端需要相应 workload、SDK
dotnet build examples/App/App.slnx
```

Android 通过 Android workload 构建并部署到模拟器或设备。iOS 设备打包、签名和运行需要 macOS / Xcode。Windows 上的 iOS 编译通过不代表已经验证设备运行。

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
