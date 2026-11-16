# MikoWebcam Demo

默认启动到 **摄像头连接** 页，默认类型为 **Miko Mock**。填写参数后点击“连接摄像头”才会创建视频会话。
页面使用 Miko.Ionic，Desktop、Simulator、Android、iOS 共用同一配置界面。
配置页路由为 `/`，连接后通过 `NavigateTo("/monitor", connection)` 把连接快照传入独立监控页。
返回配置页会停止播放、取消未完成请求并保留已填参数；直接进入缺少连接状态的监控路由会回到配置页。

在仓库根目录启动：

```sh
dotnet run --project examples/Components/MikoApp.Webcam.Desktop
dotnet run --project examples/Components/MikoApp.Webcam.Simulator
```

Simulator 提供设备尺寸、方向和安全区预览，并注册系统视频后端和模拟器原生服务。

## Miko Mock

先启动 [Mock 服务](../../../tools/Miko.MockServer/README.md)：

```sh
dotnet run --project tools/Miko.MockServer
```

- RTSP 地址默认 `rtsp://localhost:8554/webcam`。
- HTTP 服务地址默认 `http://localhost:8080`，填写服务根地址；应用自动追加 `/media/demo.mp4`。留空时使用 RTSP 主机的 HTTP 8080 端口。
- RTSP 用户名 / 密码可选，默认 Mock 服务无需认证。密码显示为掩码，凭据加入 RTSP URI 时会转义。
- 手机或远程服务请将 `localhost` 改为服务所在机器的局域网地址。
- Android / iOS / macOS 的流解码后端使用 RTSP；当前 Windows / Linux 使用相同全景的 HTTP 视频循环播放，仍可操作云台取景。

## 萤石云

切换到“萤石云”显示其配置项：设备序列号、通道、局域网地址（可带端口）、局域网用户名和设备验证码 / 密码。
认证方式可以选择：

- **Token**：填写 Access Token。
- **App Key**：填写 App Key / App Secret，由提供器获取 Token。
- **仅局域网**：填写设备地址、用户名和验证码，无需云凭据，不提供云台控制，需要支持 RTSP 的后端。

云认证支持配置 API 地址（默认 `https://open.ys7.com`）和允许云端中转。
云中转使用萤石云流量配额，默认关闭；当前 Windows / Linux 观看萤石云画面需要主动启用。
非法地址、无效通道和不完整凭据会在配置页提示；网络或设备故障由监控组件显示，可重试或返回修改参数。

## 返回与参数预填

监控页的“返回”按钮会停止云台、取消待完成请求、释放视频会话并回到配置页，保留本次填写的参数。
系统返回键、桌面 Escape 和模拟器返回按钮先退出全屏，再返回配置页。
配置仅存于内存，关闭应用后不保存密码或其他连接参数。

已有环境变量继续作为**预填值**，不会自动连接；即使预填了萤石云设备，默认类型仍为 Miko：

```powershell
$env:MIKO_MOCK_RTSP = 'rtsp://192.168.1.10:8554/webcam'
$env:MIKO_MOCK_HTTP = 'http://192.168.1.10:8080'
dotnet run --project examples/Components/MikoApp.Webcam.Simulator
```

萤石云预填项：`EZVIZ_DEVICE`（EZOpen 地址）、`EZVIZ_DEVICE_IP`、`EZVIZ_ACCESS_TOKEN`、`EZVIZ_APP_KEY`、`EZVIZ_APP_SECRET`、`EZVIZ_CLOUD_RELAY=1`。
Android 启动 extras 接受同名参数；也可以直接在界面填写。

## 验证

```sh
dotnet test tests/Miko.Webcam.Demo.Tests
dotnet run --project examples/Components/MikoApp.Webcam.Desktop -- --verify-config artifacts/webcam-config
dotnet run --project examples/Components/MikoApp.Webcam.Desktop -- --verify artifacts/webcam-live
```

`--verify-config` 只渲染配置页；`--verify` 在保存配置页后连接预填的 Mock 服务，验证竖屏 / 全屏播放及返回。
自动测试通过模拟视频后端和萤石 API 验证连接配置、凭据快照、输入绑定、切换、取消和会话生命周期，不需要真实云账号。
