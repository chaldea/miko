# Miko Media Demo

演示 Miko 通过**统一资源管理器**加载网络图片与视频并渲染（ISSUE-062）。

包含三个项目：

| 项目 | SDK | 作用 |
|------|-----|------|
| `MikoApp.Media.Api` | `Microsoft.NET.Sdk.Web` | ASP.NET Core 最小 API。离线生成 24 条产品记录、缩略图（SkiaSharp）与样例视频（FFmpeg），通过 http 提供。 |
| `MikoApp.Media` | `Microsoft.NET.Sdk.Razor` | 共享 Miko 客户端 UI（与宿主无关）。 |
| `MikoApp.Media.Desktop` | `Microsoft.NET.Sdk`（Exe） | 桌面宿主，注册 FFmpeg 视频后端并开窗运行。 |

## 运行

需要两个终端。先启动 API，再启动客户端：

```bash
# 终端 1：API（监听 http://localhost:5050）
dotnet run --project examples/Media/MikoApp.Media.Api

# 终端 2：桌面客户端
dotnet run --project examples/Media/MikoApp.Media.Desktop
```

样例视频用 H.264（libx264）离线编码为标准 MP4。API 以 `enableRangeProcessing` 提供该文件——
FFmpeg 的 HTTP 解复用器需 Range（206 Partial Content）才能 seek 到文件末尾的 `moov` atom，
否则会报 `partial file / Stream ends prematurely`。

自检（不开窗口）：

```bash
# 本地文件：生成 + 解码
dotnet run --project examples/Media/MikoApp.Media.Api -- --verify

# HTTP 链路：先启动 API（另一终端），再解码 URL（验证 Range 支持）
dotnet run --project examples/Media/MikoApp.Media.Api -- --verify-url http://localhost:5050/assets/sample.mp4
```

窗口中：

- 顶部 `<video>` 从 `http://localhost:5050/assets/sample.mp4` 加载样例视频并播放；
- 下方产品网格的每个 `<img>` 从 `http://localhost:5050/assets/thumb/{id}.png` 加载缩略图，
  加载完成前显示嵌入资源占位图 `res://Assets/spinner.png`，完成后自动切换（卡片显式定尺寸，无重排）。

全部资源本地生成，**不依赖外网**。

## 资源协议（统一资源管理器）

`<img>` / `<video>` 的 `src` 经 `MediaSource` 解析为统一协议，由 `ResourceManager` 选择加载方式：

| 协议 | 含义 | 示例 |
|------|------|------|
| `file://` 或裸路径 | 本地文件 | `file://C:/a.png`、`movie.mp4` |
| `res://` | 嵌入资源 | `res://Assets/spinner.png` |
| `http(s)://` | 网络资源 | `http://localhost:5050/assets/thumb/1.png` |
| `data:` | 内联 base64 | `data:image/png;base64,...` |

## `<img placeholder>`

标准浏览器的 `<img>` 没有 `placeholder` 属性，Miko 引擎扩展支持：当真实 `src` 尚未加载完成时，
先渲染 `placeholder`（本地文件 / `res://`）的内容，加载完成后切换为真实图片。
