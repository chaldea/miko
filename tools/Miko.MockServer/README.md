# Miko Mock Server

ISSUE-168 的独立 .NET 11 演示服务，无需摄像头、远程桌面、云账号或运行时 FFmpeg。
包含原创画面、可重新生成的媒体、Docker 配置和网络协议集成测试。

## 启动

在仓库根目录运行：

```sh
dotnet run --project tools/Miko.MockServer --no-launch-profile
```

或者使用 Docker Compose（构建上下文完全位于此文件夹）：

```sh
docker compose -f tools/Miko.MockServer/compose.yaml up --build -d
docker compose -f tools/Miko.MockServer/compose.yaml down
```

也可独立构建：

```sh
docker build -t miko-mock tools/Miko.MockServer
docker run --rm -p 8080:8080 -p 5900:5900 -p 8554:8554 miko-mock
```

| 服务 | 默认地址 | 行为 |
| --- | --- | --- |
| HTTP | `http://localhost:8080/health` | 健康检查 |
| Player | `http://localhost:8080/media/demo.mp4` | H.264 MP4，支持 GET / HEAD / Range、拖动进度 |
| 地图 | `http://localhost:8080/api/map?longitude=116.397&latitude=39.916&zoom=14&width=640&height=480` | 根据中心、缩放级别（3–18）和宽高（1–1024 像素）返回 SVG 底图 |
| 弹幕 | `http://localhost:8080/api/danmaku` | GET 查询、POST 发送 |
| VNC | `localhost:5900` | 独立桌面、按钮、计数器、键盘、剪贴板回显 |
| VNC WebSocket | `ws://localhost:8080/vnc` | binary RFB，与 TCP 共用连接上限，各连接独立桌面 |
| Webcam | `rtsp://localhost:8554/webcam` | TCP interleaved H.264，全景循环播放 |
| Webcam HLS | `http://localhost:8080/media/webcam.m3u8` | 12 秒 VOD，6 个独立 2 秒分片，由客户端循环播放 |

地图服务按 Web Mercator 投影裁取随服务嵌入的 [北京示意图](Map/beijing.svg)。测试数据覆盖故宫周边，可离线运行。启动 `examples/Components/MikoApp.Map.Desktop` 即可通过默认的 `MikoProvider` 平移、缩放和查看标记。

容器以非 root 用户运行。若 Docker daemon 位于远程主机，应将上述 `localhost` 换成该主机地址；手机使用服务器的局域网地址。

配置通过命令行（如 `--Mock:VncPort=5901`）、环境变量或 ASP.NET 配置提供：

| 环境变量 | 默认值 | 作用 |
| --- | --- | --- |
| `ASPNETCORE_URLS` | `http://0.0.0.0:8080` | HTTP 监听 |
| `Mock__ListenAddress` | `0.0.0.0` | VNC / RTSP 监听 IP |
| `Mock__VncPort` | `5900` | VNC 端口 |
| `Mock__RtspPort` | `8554` | RTSP 端口 |
| `Mock__MaxConnections` | `64` | 每种 TCP 服务的连接上限 |
| `Mock__MediaDirectory` | 程序目录下的 `Media` | 媒体目录 |

这些是无认证、无 TLS 的演示接口，面向本地和受信任的演示网络。

## 浏览器 / WASM

启动 Mock 服务后运行 `dotnet run --project docs/Miko.Docs.Demo.Browser`。
在 Playground 的 Components 中选择 **VNC** 或 **Webcam**，填写上述 WebSocket / HLS
地址并点击 Connect。也可直接打开开发服务器的 `/#/vnc` 或 `/#/webcam`。
摄像头按住方向键改变全景取景位置，返回 Connection settings 会释放视频会话。

HTTP 接口支持跨域 GET / HEAD / POST / OPTIONS；HLS 分片支持 Range。
WebSocket 只连接本进程的 VNC Mock 服务，不接受任意代理目标；二进制消息允许任意分片。
浏览器客户端采用异步收发，无 TCP socket 或后台线程依赖，协商 Raw / CopyRect 和光标、桌面尺寸扩展。
TCP 客户端保留原来的压缩编码支持。

HLS 优先使用浏览器原生播放器，不支持时自动加载随 Miko.Browser 打包的 hls.js。
视频帧进入 Miko 的正常绘制管线，因此 PTZ 裁剪和全屏保持可用。
MP4 地址也可以直接填入摄像头配置。服务运行时不需要 FFmpeg。

HTTPS 文档站需要 HTTPS/WSS Mock 地址，避免浏览器阻止混合内容。
可以在反向代理上终止 TLS，并把 `/vnc` 的 WebSocket Upgrade 请求和 `/media/` 请求转发到 8080；
浏览器演示只需暴露 HTTP 端口，不需要公开 5900 / 8554。

自己的 WASM 应用注册：

```csharp
builder.UseBrowserNative().UseBrowserVideo(); // Miko.Browser.Native / Miko.Browser.Video
builder.Services.AddMikoMockWebcam("https://mock.example/media/webcam.m3u8");
// MikoWebcam.Device = new WebcamDevice("mock");
// MikoVnc.Host = "wss://mock.example/vnc";
```

## 连接现有 Demo

PowerShell 中，在启动各 Demo 前设置：

```powershell
$env:VNC_HOST = 'localhost::5900'
dotnet run --project examples/Components/MikoApp.Vnc.Desktop

$env:MIKO_MOCK_RTSP = 'rtsp://localhost:8554/webcam'
$env:MIKO_MOCK_HTTP = 'http://localhost:8080'
dotnet run --project examples/Components/MikoApp.Webcam.Desktop

$env:MIKO_MOCK_HTTP = 'http://localhost:8080'
dotnet run --project examples/Components/MikoApp.Player.Desktop
```

Player 自动打开服务端 MP4，加载弹幕，提供 Send 和 Refresh comments。
VNC 点 `CLICK ME` 增加计数，`RESET` 清零；键盘输入显示在标签上，空格/回车也增加计数。
Webcam 默认进入 Ionic 配置页，环境变量仅预填参数；点击“连接摄像头”后才播放，返回会断开并保留表单。
也可运行 `dotnet run --project examples/Components/MikoApp.Webcam.Simulator`，在设备模拟器中演示。
监控页按住方向盘平移取景；`StartPtz(ZoomIn/ZoomOut)` 也支持缩放。
配置项和萤石云切换说明见 [Webcam Demo](../../examples/Components/MikoApp.Webcam/README.md)。

应用可以直接注册：

```csharp
builder.Services.AddMikoMockWebcam(
    "rtsp://localhost:8554/webcam",
    "http://localhost:8080/media/demo.mp4");
// MikoWebcam.Device = new WebcamDevice("mock");
```

支持 H.264 流解码的后端（Android / iOS / macOS）使用 RTSP。
现有 Windows / Linux 后端尚未提供 `IVideoStreamDecoderFactory` 实现，注册方法自动使用 HTTP MP4 循环播放；仍使用相同全景和 PTZ。
省略第二个参数时，HTTP 地址取 RTSP 主机的 8080 端口。自定义 HTTP 端口请传入地址或设置 Demo 的 `MIKO_MOCK_HTTP`。
直接构造 `MockWebcamProvider` 默认始终返回 RTSP。
平台视频后端仍由应用注册（如 `UseSystemVideo()`）；HTTP 回退不额外引入原生解码库。

## 协议与并发

**VNC** 实现 RFB 3.3 / 3.7 / 3.8 的版本、安全类型 None、初始化和基础客户端消息：SetPixelFormat、SetEncodings、FramebufferUpdateRequest、KeyEvent、PointerEvent、ClientCutText，以及已废弃的 FixColourMapEntries 消费。
支持 8/16/32 位 true-color、大小端、调色板格式、区域更新和增量请求；使用标准必选 Raw 编码，不提供 Tight/ZRLE 等可选压缩扩展或 VNC 密码认证。
空闲增量请求等待画面变化，不忙循环发送空帧。每个连接都拥有独立背景副本和输入状态；shared 标志不会踢掉其他虚拟桌面。

**RTSP** 实现 OPTIONS、DESCRIBE/SDP、SETUP、PLAY、PAUSE、GET_PARAMETER、SET_PARAMETER、TEARDOWN，校验会话并支持保活。
使用 RTP/AVP/TCP interleaved，返回协商的通道号；UDP 请求返回 461。H.264 采用 RFC 6184 单 NAL / FU-A 分片及 marker，提供 SPS/PPS、RTCP SR/SDES，消费客户端 RTCP。
每个连接都有独立 session、SSRC、序列号、时间戳和播放任务；视频循环时 RTP 时间戳持续递增。暂停后从循环片头的关键帧继续，演示流不提供任意 seek；不支持的 Range 返回 457。
服务退出取消监听、关闭全部连接并等待任务回收。

**PTZ** 在客户端对 1280×360 全景进行归一化裁剪，默认视野 480×270（16:9），高度可在全景的 25%–100% 之间缩放。
移动连续按速度积分，在视频边缘钳制，松开立即停止。每个 `WebcamDevice` 实例保存独立视野；多个视图请各自创建设备实例。
运行时不用按连接转码，低帧率全景数据可被所有连接共享读取。

**弹幕** 是线程安全的共享内存时间线，最多保存 1000 条（包含初始示例），满后淘汰最早提交项；重启恢复初始内容。不同用户通过刷新看到其他人的评论。

```http
GET /api/danmaku?from=0&to=6
POST /api/danmaku
Content-Type: application/json

{"time":3.5,"text":"你好 Miko","mode":"scroll","color":"#ffffff"}
```

`time` 单位为秒，范围 0–12；`text` 为 1–200 字符；`mode` 为 `scroll` / `top` / `bottom`；`color` 为 `#RRGGBB`。
POST 返回 201、带 ID 的评论和 Location；GET 返回按时间、ID 排序的数组，`GET /api/danmaku/{id}` 查询单条。非法输入返回 400，未知 ID 返回 404。

## 媒体与验证

`Media/demo.mp4` / `panorama.h264` 为同一段原创城市全景：1280×360、5 FPS、12 秒、无音频、H.264 baseline、每两秒一个关键帧，MP4 约 106 KiB，平均约 72 kbit/s。
`Media/desktop.ppm` 是原创的 640×360 VNC 背景。仅重新生成时需要带 libx264、drawtext 的 FFmpeg：

```sh
cd tools/Miko.MockServer
dotnet run -- --generate-media /path/to/ffmpeg
```

生成器使用托管 RGB 栅格绘图创建背景，再调用 FFmpeg 编码、提取 Annex B 和打包 HLS 分片；运行服务不执行外部进程、不下载媒体。资源随 build / publish 自动复制。

```sh
dotnet test tools/Miko.MockServer/tests/Miko.MockServer.Tests.csproj
dotnet test tests/Miko.Components.Tests/Miko.Components.Tests.csproj
dotnet test tests/Miko.Tests/Miko.Tests.csproj
ffmpeg -rtsp_transport tcp -i rtsp://localhost:8554/webcam -frames:v 3 -f null -
```

网络测试在随机端口启动真实服务器，覆盖 RFB 版本/像素格式/剪贴板/双连接交互、RTSP 会话与暂停恢复/双连接/循环时间戳、HTTP Range 和并发弹幕；组件测试验证 PTZ 边界、停止、隔离和 HTTP 回退，渲染测试检查源区域裁剪与帧资源所有权。

启动 Mock 和 WASM Playground 后，可使用已安装的 Playwright 与 Chromium / Edge 执行浏览器验证：

```sh
node docs/Verify-MockMedia.mjs http://127.0.0.1:5210 http://localhost:8080 /path/to/playwright-core/index.mjs /path/to/browser
```

脚本检查实际 WASM VNC 输入和切页断开、原生 HLS 与强制 hls.js 播放、PTZ 像素变化、循环边界和会话释放，截图写入 `artifacts/issue168-browser`。
