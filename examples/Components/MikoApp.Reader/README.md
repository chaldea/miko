# 阅读器示例

Desktop、Android、iOS 共用本项目的书架与阅读页面。启动后先显示四本内置示例，支持全部／电子书／漫画筛选；点选条目进入对应的 `BookReader` 或 `ComicReader`。

正文默认沉浸显示，单击调出工具栏。底部提供进度、目录、外观、日夜切换与设置；双击漫画／PDF 图片可缩放。设置使用底部面板，支持触控、内容滚动和安全区。返回按钮回到书架；系统返回键依次关闭面板、隐藏工具栏、返回书架。内置样例无需网络和文件权限。

## 启动

从仓库根目录执行，需要 .NET 11 SDK。移动端还需要对应 .NET workload 和平台 SDK。

```sh
# 桌面
dotnet run --project examples/Components/MikoApp.Reader.Desktop

# 本地文件加入书架：CBZ/CBR 自动选择漫画，其余格式选择电子书
dotnet run --project examples/Components/MikoApp.Reader.Desktop -- /path/to/book.epub

# 也可把 PDF 放入漫画阅读器
dotnet run --project examples/Components/MikoApp.Reader.Desktop -- --comic /path/to/book.pdf

# Android：先启动模拟器或连接设备，再安装并运行
dotnet build examples/Components/MikoApp.Reader.Android -t:Run

# iOS：在装有 Xcode 的 Apple Silicon Mac 上，先启动模拟器
dotnet build examples/Components/MikoApp.Reader.iOS -t:Run -r iossimulator-arm64
```

iOS 多设备时可添加 `-p:_DeviceName=:v2:udid=<模拟器 UUID>` 指定目标，UUID 由 `xcrun simctl list devices available` 获取；Intel Mac 使用 `iossimulator-x64`。真机运行需自行配置开发签名。移动端启动项目目前展示内置样例，应用可用 `App.CreateContext(source: ..., comic: ..., fileTitle: ...)` 将自己的数据源加入书架。

## 组件参数与状态

`ReaderDemo.razor` 展示通过参数配置字体、主题、页边距、阅读方向等，并用 `BackRequested` 接回书架。阅读器自身带设置面板；`Immersive=false` 可初始显示工具栏，`Texts` 支持中文／英文及自定义文案。外观中的“阅读遮光”对应 `Dimness`，只给正文加遮光，不更改设备亮度。

`ShelfBook` 在返回时记录设置与目录索引，重新打开时恢复。状态只保留在当前应用会话，不保存到磁盘，也不记录文本章节内部的分页。文件数据源由调用方管理，桌面程序在窗口退出后释放它。

## 验证

```sh
dotnet run --project examples/Components/MikoApp.Reader.Desktop -- --verify /tmp/miko-reader-preview
dotnet test tests/Miko.Components.Tests/Miko.Components.Tests.csproj --filter FullyQualifiedName~Reader
```

`--verify` 不打开窗口，使用真实引擎在带安全区的 390 × 844 手机视口执行点击、书架往返、设置和目录导航，并输出 `shelf.png`、`book.png`、`book-controls.png`、`book-settings.png`、`book-appearance.png`、`book-dark.png`、`book-contents.png`、`comic.png`、`comic-settings.png`、`comic-zoom.png` 和 `shelf-desktop.png`。

数据源接口、支持格式与限制见 [阅读器文档](../../../docs/Miko.Docs.Web/Content/components/reader.md)。
