# MikoVnc 实现方案（issues/feat-components-vnc.md）

## 结论先行：协议自己实现，不用 NuGet 包

issue 要求跨平台、不依赖 C/C++。2026-11 nuget.org 上的候选：

| 包 | 问题 |
| --- | --- |
| `Community.MarcusW.VncClient` 2.0.8（较活跃） | Tight 的 JPEG 走 `TurboJpegDecoder`（DllImport libturbojpeg，原生库）；zlib 包的是 BCL `ZLibStream`（各平台背后是原生 zlib-ng）；依赖 BouncyCastle（5 MB，只为 VeNCrypt） |
| `Quamotion.RemoteViewing` 1.1.211 | 2022 年后无更新；依赖 `TurboJpegWrapper`（原生）与 sharpcompress |
| `VncLib` 2.0.0 / `Vnc.Net.Std` 1.0.4 | 下载量几百，前者无 Tight JPEG，后者只有 Raw / ZRLE |
| `VncSharp*` | `net6.0-windows`，WinForms |

因此按 issue 的第二条路自己实现，全部托管代码，不加任何包：

- **zlib**：`ZlibInflater`，RFC 1950/1951 流式解压。VNC 的 Zlib / ZRLE / Tight（4 路）都是**整个连接一条不结束的 zlib 流**，
  按矩形分段发送，每段以 sync flush 结尾。BCL 的 `DeflateStream` 要一个能预读的源，读到段尾就当流结束，
  而且背后是原生库，不满足「纯 C#」。这里按段喂入：每次只解完整的块，块头处打检查点，一段在块中间断开就回滚，
  等下一段到了接着解（partial flush 的服务器也能用）。32 KB 历史窗口跨段保留。解码速度在 260 MB/s（噪声数据）
  到 8 GB/s（大块重复）之间，1080p 整帧（4.5 MB）十几毫秒。
- **DES**：`VncDes`，只做 VNC 认证要的单块 ECB 加密。没有用 `System.Security.Cryptography.DES`：OpenSSL 3 把单 DES
  挪进了 legacy provider，浏览器（WASM）上根本没有。用 FIPS 46-3 向量和 `openssl enc -des-ecb` 的结果校验。
- **JPEG**（Tight）：交给引擎已有的图像解码器（`IGraphicsFactory.DecodeImage`，即渲染后端 Skia），协议层只认
  `IVncJpegDecoder` 接口，不新增原生依赖。没有解码器时不向服务器申请 JPEG。

## 结构

```
Vnc/
  MikoVnc.cs          参数、状态、连接生命周期（密码重试、断线）、公开 API
  MikoVnc.Input.cs    鼠标 / 触摸手势 / 滚轮 / 键盘 → RFB
  MikoVnc.Chrome.cs   界面：远端画面、状态层、工具栏、按键栏、设置、连接表单、密码对话框、提示
  VncView.cs          VncScreen（画布 + ITextEditable 键盘目标）；VncView（适应 / 缩放 / 平移、分块绘制、远端指针）
  VncModels.cs        VncState / VncQuality / VncScaling / VncDesktopInfo / VncAddress
  VncTexts.cs         文案（英文 + VncTexts.Chinese），异常 → 文案
  Protocol/           VncClient（握手、安全、收发线程）、VncDecoders、VncFramebuffer、ZlibInflater、VncDes、VncKeys
```

- **像素格式**固定要求 32 bpp、depth 24、小端、红 16 / 绿 8 / 蓝 0：线上一个像素就是 B、G、R、X，等于
  `ImagePixelFormat.Bgrx8888`，Raw 直接拷贝；ZRLE 的 CPIXEL 是 B、G、R，Tight 的 TPIXEL 是 R、G、B（两者不同，容易写反）。
- **帧缓冲无锁**：接收线程解码写入，渲染线程读取。每次写完一个矩形，给覆盖的 128×128 块各加一次版本号
  （`Interlocked`）；渲染时版本变了的块才重新建图像。打字只重传 64 KB，而不是整个桌面 8 MB。读写竞争最多撕裂一块，
  下一帧版本号不同又会补上。
- **发送队列**：输入方法从 UI 线程调用，从不阻塞（写线程排空队列）。按钮不变的连续移动只保留最新一个；
  **按下 / 松开永远不被合并**（见下「踩坑」）。
- **刷新节奏**：收完一个更新立即请求下一个（增量），帧率跟随网络和服务器；重绘请求跨线程合并为一个 `Dispatcher.Post`。

## 核心库新增（输入）

远程桌面需要的输入 Miko 原先没有，按浏览器语义补在核心库里，任何组件都能用：

| 新增 | 原先 |
| --- | --- |
| 中键 / 右键：只向命中元素派发 pointerdown / mousedown 与 pointerup / mouseup（`Button` 标明），不产生 click、不动焦点 | `OnPointerDown` 对非左键直接 return，图表里判断 `Button == Right` 的代码其实从没执行过 |
| `wheel` 事件（`OnWheel` / `@onwheel`，`WheelEventArgs`）：先派发给指针下的元素，`PreventDefault` 后不再滚动容器 | 滚轮只滚动容器，元素收不到 |
| `keyup`（`OnKeyUp` / `@onkeyup`）；`KeyboardEventArgs.Code`（物理键）、`MetaKey`、`Repeat`；`RepeatKey` 先派发 `Repeat` 的 keydown | `OnKeyUp` 是空方法 |
| `MikoKey` 追加完整键位（方向、翻页、字母、数字、标点、小键盘、Meta、锁定键）；`MikoKeyModifiers.Meta`；Silk 键位映射 | 字母、上下方向键都是 `Unknown` |
| pointerdown / mousedown 被 `PreventDefault` 时，这次点按不改变焦点（浏览器规则） | 点任何非输入元素都会清掉焦点，手机上一点画面软键盘就收起 |
| `Cursor.None`（CSS `cursor: none`），Silk 宿主用 `CursorMode.Hidden` | 无法隐藏系统光标 |

既有组件里会被右键误触发的拖动（`IonRange`、刷新下拉、云台盘、播放器进度条与滑块）补了「只认左键」。
Ionic 其余拖动组件原本就判断了 `Button`。Android IME 的 `SendKeyEvent` 现在也转发松开与方向、Tab、Esc 等键；
iOS 的删除键补发 keyup。

## 键盘模型

- 远端画面元素（`VncScreen`）实现 `ITextEditable`，但从不保存文字：它因此能拿到焦点、键盘事件、输入法提交、
  软键盘（Android / iOS 现有的 IME 管道原样可用），收到什么就转成 keysym 发出去。`IsMultiline = true`，
  这样 iOS 回车提交的 `"\n"` 不会被单行过滤掉（发 Return）。
- 能打字的键（字母、数字、标点、空格）在 keydown 时不发，等随后的文本（布局、死键、输入法都已处理）按字符发
  （拉丁 1 原样，其余 `0x01000000 + 码点`）。不能打字的键与修饰键按 keydown / keyup 发按下 / 松开。
- 按住 Ctrl / Alt / ⌘ 时按「键」发（美式布局名，Ctrl+C 在任何布局下都是 Control + `c`），并吞掉平台随后可能补发的文字。
- 按键栏的 Ctrl / Alt / Shift / Win 是「粘滞」的：按下一次，作用于下一个键，然后松开。
- 失焦、断线、切到仅查看时松开所有按下的键；事件上的修饰位与记录不一致（漏掉的 keyup）时补发松开。

## 触摸

只有一根手指被宿主转发（Android `_primaryPointerId`、iOS `MultipleTouchEnabled = false`），所以没有双指：
轻点 = 左键单击；按住 0.5 秒 = 右键（松手时发，画圈提示，带触感）；按住后拖动 = 左键拖动（从按下处开始）；
直接拖动 = 放大时平移画面，适应窗口时滚动（每 24 px 一格滚轮，内容跟手）。缩放用工具栏。

## 踩坑

- **锁屏的 Windows 会关显示器，TightVNC 随之抓到全黑。** 第一次离屏验证得到黑屏，帧缓冲 0% 有亮度；
  直接用 `VncClient` 对比七种编码时却有画面——因为前一次运行移动过指针把显示器唤醒了。不是解码问题：
  任何一次指针移动都会唤醒。验证脚本先移动一次指针再截图。
- **指针合并吞掉了「按下」。** 按住后拖动时，先发左键按下（起点）紧接着发移动（同为左键按下），两条消息按钮相同，
  被合并成一条，服务器看到的按下位置成了第一次移动的位置（120,80 而不是 80,80）。合并只能发生在「移动」之间：
  按钮与上一条不同的消息永远单独发送。有测试覆盖，回退修复后两个用例变红。
- 同一 TightVNC 服务器短时间内反复连接偶尔会在 ServerInit 之前断开（握手失败），界面显示「连接中断」，重连即可。

## 安全

- VNC 认证只是 DES 挑战应答，会话本身不加密：文档注明只在可信网络或隧道内使用。VeNCrypt / TLS、RA2、
  Apple DH 未实现，服务器只提供这些时按名称报出。
- 密码只在内存中（用户在对话框里输入的密码用于本组件的重连，不落盘），`VncConnectOptions.ToString()` 不含密码。
  示例从环境变量 / 启动参数读取服务器与密码，源码与文档中不出现 issue 里的测试凭据。

## 验证记录（2026-11-05）

- **真实服务器**：issue 提供的 Windows 主机（TightVNC，RFB 3.8，安全类型 2 / 16，1920×1080）。
  `MikoApp.Vnc.Desktop --verify` 用真实 App 上下文离屏渲染：约 250 ms 连上，首帧（Tight + JPEG）约 900 KB；
  工具栏、按键栏、设置面板、放大 2×、手机宽度（工具栏收起标题）、断开后的暗化画面与「重新连接」截图都正常。
- **编码逐一比对**：每种编码单独连接一次，解出的整屏与 Raw 逐像素比较。ZRLE、Tight（无损）、Hextile、Zlib、RRE
  与 Raw **完全一致**（0.000% 像素不同）；Tight + JPEG（质量 6）平均误差 4.78/255（照片上的正常 JPEG 损失），
  流量 521 KB 对 Raw 的 8104 KB。服务器回应 Zlib 时用的是 Raw（它的选择）。
- **只移动过指针**：远端是别人的桌面，验证没有点击、没有键入（锁屏界面按键会尝试 PIN）。键盘路径由测试覆盖。
- **自动化测试**：组件测试中的 `VncPrimitivesTests`（inflater 对比 BCL zlib，含块中断开；DES 向量与 openssl 结果；keysym；地址）、
  `VncClientTests`（回环上的脚本化服务器：3.3 / 3.7 / 3.8 / 3.889、None / VNC / 不支持的安全类型、密码错误与询问、
  每种编码及其子编码、LastRect、DesktopSize、指针形状、剪贴板、越界矩形、指针合并）、`MikoVncTests`（真实输入控制器：
  画面像素、鼠标三键与滚轮、文本与快捷键、失焦松键、粘滞修饰键、Ctrl+Alt+Del、仅查看、密码对话框与重试、取消、
  连接表单、缩放与收起工具栏、画质切换、桌面全屏下 Esc 交给远端、触摸单击 / 长按右键 / 长按拖动 / 滚动、软键盘按钮）；
  核心 `RawInputEventTests`（右键与中键、wheel、keyup 与键码、重复键、preventDefault 保留焦点、cursor: none）。
- **未覆盖**：Android / iOS 只编译通过，未在模拟器上连真实服务器；多指手势、iOS 硬件键盘的方向与修饰键未实现；
  VeNCrypt / TLS 未实现。
