# MikoWebcam 实现方案（issues/feat-components-webcam.md）

## 结论先行：萤石取流为什么不走 SDK

萤石 SDK（Android / iOS / Windows 三端）的接口分两类：

- **控制类**（设备信息、能力集、云台、截图、隐私遮蔽、画面翻转、清晰度、取流地址……）全部是开放平台 HTTP 接口
  `https://open.ys7.com/api/lapp/…`，SDK 只是包了一层。这一类用 C# 直接调用即可（`EzvizOpenApiClient`），没有跨平台问题。
- **取流类**（EZOpen 协议：内网直连 → P2P → 流媒体转发，按此顺序尝试）走萤石私有协议。实现在闭源原生库里
  （`libezstreamclient.so`、`libNPClient.so`、`libstunClient.so`、`libPlayCtrl.so`……Android 每个 ABI 约 60 MB），
  协议本身没有公开，也无法用托管代码对照实现。

实测（2026-11-05）：

| 路径 | 结果 |
| --- | --- |
| `/api/lapp/live/url/ezopen` 解析 EZOpen 地址 | 得到 `wss://wxjsdecoder.ys7.com:20006/live?…`（EZUIKit 网页播放器的 WebSocket 网关，云端转发） |
| 该 WebSocket 网关 | `{"errorCode": 9048, "errorMsg": "get stream error"}`，即账号并发上限（免费账号） |
| `/api/lapp/v2/live/address/get` 的 HLS / RTMP / FLV | HLS 播放列表只有一段 `…/ErrCode/9048_0.ts`，同样是 9048 |
| 局域网设备 `192.168.1.64:554` | 标准 RTSP：用户 `admin`、密码为设备验证码，Digest 鉴权。主码流 3840×2160 H.265 15fps，子码流 1280×720 H.265 10fps |

因此默认 Provider 选择：**同网段时直连摄像头的 RTSP**（等价于 SDK 的「内网直连」，不经服务器、不耗流量），
云端转发（HLS）作为显式开启的回退（`EnableCloudRelay`）。P2P 没有托管等价物，不实现。按 issue 的约束，
这一方案无需原生库，因此作为内置默认实现放在 `Miko.Components` 中。

## 结构

```
Webcam/
  MikoWebcam.cs            组件：画面 + 工具栏 + 功能区 + 云台盘；全屏横屏覆盖层
  WebcamPtzPad.cs          云台盘（一个 canvas，按住转动、松开停止）
  WebcamModels.cs          IWebcamProvider 抽象与模型（WebcamDevice / WebcamInfo / WebcamStream …）
  WebcamTexts.cs           文案（英文默认 + WebcamTexts.Chinese），错误码 → 文案
  WebcamServiceExtensions  AddMikoWebcam() / AddMikoRtsp()
  Ezviz/                   EzvizOpenApiClient（token 签发与续期）+ EzvizWebcamProvider
  Streaming/               托管 RTSP 客户端：RtspConnection、SdpSession、RtpVideoDepacketizer、RtspVideoSource
```

核心库（`src/Miko/Platform/Video/`）新增「编码流」通道，供任何直播 / 私有协议复用：

- `IEncodedVideoSource`：逐个读出 H.264 / H.265 Annex-B 访问单元；`IEncodedVideoSourceFactory` 按 scheme 认领 `<video src>`。
- `IVideoStreamDecoderFactory` / `IVideoStreamDecoder`：由视频后端实现，用平台硬件解码器解码推送的码流，
  帧仍走原有的零拷贝 `IVideoFrameSource`（macOS / iOS：VideoToolbox → `CVPixelBuffer` → Metal；Android：MediaCodec → `SurfaceTexture` OES）。
- `EncodedVideoSession`：把两者接成一个 `IVideoSession`。直播语义：时长为 0，`Pause` 断开连接、保留最后一帧，`Play` 重连并从下一个关键帧继续。
  断线重连 5 次（1、2、4、8、8 秒），`VideoStreamException.IsPermanent`（401/403/404）立即失败。整个会话共用一个解码器，重连期间画面不黑。
- 引擎在把源交给 `IVideoBackend` 之前先问 `EncodedVideoSources`（DI 中注册的全部工厂）。后端没有流解码器时，`<video>` 收到错误事件，不会崩溃。
- `NalUnits`：Annex-B 拆分、参数集提取、AVCC/HVCC 长度前缀转换，供各平台解码器共用。

## 实测踩到的摄像头怪癖（均有测试覆盖）

- **marker 位打在 PPS 后面。** 海康 / 萤石在关键帧的 VPS/SPS/PPS 之后就置 marker，切片还没来。
  按 RFC 用 marker 切访问单元，会把参数集和切片拆成两帧，解码器只能解出约 1/55 帧。
  修正：marker 只在当前单元已含切片时结束单元，时间戳变化也结束单元。
- **FU 续片的类型字段是 38。** IDR 切片（类型 19）的分片续片里写的是 38，类型只能取起始分片的。
- **SDP 的 `sprop-parameter-sets` 是垃圾。** H.265 流标着 H.264 的 `profile-level-id`，sprop 里的内容不是合法参数集。
  这里会按类型校验后丢弃，真正的参数集在码流内。
- **会话超时 20 秒。** 播放期间按 `timeout` 的一半发 `GET_PARAMETER` 保活。

## Android 与按需绘制

Android 的解码输出 Surface 只能在渲染线程（GL 上下文）创建，而渲染线程只在绘制 `<video>` 时才调用 `AcquireCurrentFrame`。
解码器创建时网络已经往返过一次，此时宿主可能已经空闲不出帧。为此 `EncodedVideoSession` 在创建解码器后发一次 `Buffering(true)`：
该事件经调度器投递，唤醒按需绘制的宿主，并使 video 失效重绘。解码器的第一帧最多等 Surface 5 秒。

## 安全

- 设备验证码就是摄像头的本地密码：`WebcamDevice.ToString()`、`WebcamStream.ToString()` 都不含它；
  引擎日志里的 `<video src>` 经 `EncodedVideoSession.Redact` 去掉 user-info；RTSP 请求行不带凭据，只在 Digest 响应里出现。
- 示例从环境变量读取 AppKey / Secret / 设备地址，源码与文档中不出现 issue 里的测试凭据。文档提示不要在客户端分发 AppSecret，应由服务端签发 token。
- 云端转发默认关闭：它计入账号的付费流量和并发上限。

## 验证记录（2026-11-05）

- **桌面（macOS 27，Apple M6）**：`MikoApp.Webcam.Desktop --verify` 用真实 App 上下文（同一套 DI、Provider、`UseSystemVideo`）离屏渲染。
  局域网设备 AB1234567：674ms 出画（子码流），切换高清 1.2s 内重连出画（4K H.265），全屏横屏截图正常。
  VideoToolbox 解码统计：主码流 60/60 帧、子码流 37/37 帧。
- **云台（真机）**：云端在线设备 CD7654321 无法在本网段取流（`192.168.2.44` 不可达，转发报 9048），但云台可控。
  经真实输入控制器按住云台盘右键 1 秒、再按住左键 1 秒，三张设备截图依次显示：原位、右转后、转回原位，证明转动与停止都生效。
- **Android API 36 模拟器（arm64）**：MediaCodec `c2.android.hevc.decoder` 解码子码流 10fps、切高清 3840×2160；画面正立。
  全屏后 `mCurrentRotation=ROTATION_90`，状态栏 / 导航栏 `visible=false`；返回键退出全屏并恢复竖屏，Activity 保持前台。
- **iOS 27 模拟器（iPhone 18 Pro）**：VideoToolbox 解码局域网 H.265 实时画面（时间水印与当前时间一致）。iOS 真机还需要 `NSLocalNetworkUsageDescription`（已写入示例 Info.plist），本机无真机未验证。
- **自动化测试**：核心 `NalUnitsTests`、`EncodedVideoSessionTests`（含引擎路由）；组件 `RtpVideoDepacketizerTests`、`RtspVideoSourceTests`
  （回环上的脚本化 RTSP 服务器：Digest、SDP、TCP 交错、超时、取消）、`EzvizWebcamProviderTests`（响应体取自真实接口）、`MikoWebcamTests`（真实输入控制器驱动云台盘）。
  逐一回退 marker 规则、永久错误立即失败、释放时停止云台、引擎路由四处修复，对应用例都会变红。
- **未覆盖**：Windows / Linux 暂无流解码器（`SystemVideoBackend.CanDecode` 返回 false，界面显示原因）；音频与对讲未实现；
  隐私遮蔽与画面翻转只做了接口级测试，没有在真机上切换（会改变他人设备的状态）。
