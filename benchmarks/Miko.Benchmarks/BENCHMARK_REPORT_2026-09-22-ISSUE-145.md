# 基准测试报告：真实应用布局场景（ISSUE-145）

> 本报告回答 ISSUE-145 的两个问题：
> 1. 为什么既有基准报告「大页面能稳 60 帧」，而 `examples/App/Anime` 的 `/home` 在
>    Android 模拟器上实测 60–70 ms？
> 2. 复现之后能优化什么？

---

## 测试元数据

| 项 | 值 |
|----|----|
| 报告日期 | 2026-09-22 |
| CPU | AMD Ryzen 7 8700G（16 逻辑核 / 8 物理核） |
| 操作系统 | Windows 11 Pro (10.0.26200) |
| .NET SDK | 10.0.401 |
| BenchmarkDotNet | v0.14.0 |
| GitHash（优化前基线） | `d73c47b` |
| 新增基准项目 | `benchmarks/Miko.Ionic.Benchmarks` |

---

## 1. 既有基准为什么测不出问题

**结论：元素数量不预测帧耗时，「元素数 × 样式表复杂度」才预测。**既有基准只变第一个因子。

`Miko.Benchmarks` 里的页面是手搭的 `DivElement` 树，配一张 1–7 条规则的样式表。
`FullFrame_LargePage`（500 元素）报 2.33 ms，看着很宽裕——但它的样式阶段几乎不花钱，
耗时几乎全在绘制。

真实应用是另一种形状：

| | 既有「大页面」基准 | Anime `/home` |
|---|---|---|
| 元素数 | 500 | 242 |
| 样式表规则数 | 1 | 527 |
| 每个元素的候选规则 | ~1 | ~10 |
| 作者写下的控件数 | 500 个 div | 约 30 个 Ionic 控件 |
| 控件展开后的元素数 | 1:1 | 约 1:8 |

组件库把「作者写的一个 `<IonButton>`」展开成宿主 + native + inner + slot 等多层结构，
而这些结构又要在一张上百条复合/后代选择器的表里做级联。**这两个因子只有在引入真实组件库
之后才会同时出现**，所以合成树无论堆到多少元素都复现不出来。

### 新增：`benchmarks/Miko.Ionic.Benchmarks`

按用户要求**独立成一个项目**，不污染核心基准套件的「零组件库依赖」定位。
它引用真实的 `Miko.Ionic`，用真实 Razor 组件搭出一个 Anime `/home` 形状的页面
（`Pages/CatalogHomePage.razor`），并对真实 Ionic 样式表跑布局。

形状核对（`dotnet run -c Release -- --census`）：

```
elements = 157
rules    = 427
```

对照真机应用：242 元素 / 527 条规则。同一量级、同一结构比例。

> `--census` 存在的意义：基准页一旦悄悄偏离应用形状，它报出的数就没人该据以行动。
> 核对形状要能一条命令跑出来。

---

## 2. 一个必须先纠正的测量陷阱：JIT 预热

调查过程中发现的最重要的一件事，**它同时推翻了 issue 里「样式占大头」的初步归因**：

**预热 40 帧远远不够。**样式阶段是「每个 CSS 属性一个分支」的巨量直线生成代码
（`ApplyStylePropertiesGenerated` / `ResetComputedGenerated` / `Style.MergeGenerated`），
分层编译要几百次调用才升到 tier-1。

同一份代码、同一棵树，只改预热帧数（Anime `/home`，桌面 Release）：

| 预热帧数 | 整帧 | build | style | layout | paint |
|---|---|---|---|---|---|
| 40 | 18.6 ms | 0 | **10.4 ms** | 3.6 ms | 4.4 ms |
| 400 | 5.8 ms | 0 | **1.5 ms** | 0.6 ms | 3.7 ms |

样式阶段差 6.7 倍。基准项目里同样成立（`--stages`：12.6 ms → 2.5 ms）。

**含义**：
- 探针初测给出的「盒布局和样式消耗」是**冷代码假象**。稳态下样式只有 1.5 ms，
  不是瓶颈，照着它优化级联会打空。
- BenchmarkDotNet 自带充分预热，所以它的数可信；手写计时循环必须自己预热 ≥300 帧，
  且预热循环要和被测循环做**完全相同**的工作（预热时也要 `InvalidateLayoutCache()`，
  否则走布局缓存快路径，样式代码一次都没跑到）。

---

## 3. 复现：真正贵的是哪一帧

排除预热假象后，把 `/home` 的三类帧分开测（桌面，充分预热）：

| 帧类型 | 整帧 | build | style | layout | paint | 分配 |
|---|---|---|---|---|---|---|
| 稳态空闲帧 | 3.6 ms | 0 | 0 | 0 | 3.6 ms | 38 KB |
| 状态变更重排帧 | 5.8 ms | 0 | 1.5 ms | 0.5 ms | 3.7 ms | 642 KB |
| **路由导航重建帧** | **16.7 ms** | **7.5 ms** | 3.0 ms | 1.1 ms | 4.9 ms | **5,478 KB** |

**贵的是重建帧，贵在 build 阶段**——正是用户感知为「切页面卡一下」的那一帧，
也正是既有基准套件完全没有覆盖的阶段（它只测稳态帧）。在 CPU/内存受限、
且代码更冷的 Android 模拟器上，这一帧放大到 60–70 ms 完全合理。

---

## 4. 优化：组件不再各自解析主题

### 病灶

每个 Ionic 组件 `Build()` 都会走 `IonicComponentBase.ApplyThemeScope` →
`ResolveTheme`，而它做的是：

```csharp
var resolved = (IonicOptions?.Value.Theme ?? new IonicTheme()).ResolveForMode(mode);
```

`ResolveForMode` = `IonicTheme.Create(mode)`（一个 540 行、几百个 token 赋值的方法）
**再**把应用主题的已声明值逐个拷贝过去（37 个 token 组）。之后
`IonicStyleRegistry.Register` 还要用 `GetStyleKey` 把该组件依赖的全部 token
指纹化再做一次 SHA-256——**只是为了查一个几乎总是已经注册好的 scope 类名**。

实测单次解析 **37.9 KB / 15.5 µs**。基准页上有 63 个 Ionic 组件：

- **2,332 KB / 2,773 KB = 84%** 的 build 阶段分配
- **978 µs / 1,712 µs = 57%** 的 build 阶段耗时

而这 63 次解析的结果**彼此完全相同**——输入只有（应用主题、mode、祖先 ConfigProvider
的局部主题）三项，一次页面构建里它们不变。

### 改动

1. `IonicComponentBase`：按（应用主题实例、mode、级联主题实例）缓存已解析主题，
   全页共享一个实例。按**引用**比较而非按值——主题可变，两个当下相等的实例日后可能分叉，
   而调用方实际保持稳定的正是实例身份。
2. `IonicTheme`：为 style key 加记忆化，但**opt-in**（`MarkImmutable`），只有
   `IonicComponentBase` 缓存的那些「此后不再被写」的已解析实例才启用。

第 2 点的 opt-in 是必须的，不是保守：style key 必须持续反映主题当前值——调用方改了自己
持有的主题，下一次 `Register` 就该给出不同的 key。这条契约由
`IonicThemeRegistryTests` 把守，我最初的「无条件记忆化」版本正是**被这两个既有测试测红**
才收敛成现在的形状。

另提供 `IonicComponentBase.InvalidateResolvedThemes()`，供「就地改主题后重渲染」的测试使用。

### 效果

组件构建（`benchmarks/Miko.Ionic.Benchmarks`，BenchmarkDotNet 默认 job）：

| 指标 | 优化前 | 优化后 | 变化 |
|---|---|---|---|
| Build 耗时 | 919 µs | **62 µs** | **↓ 93%（14.7×）** |
| Build 分配 | 2,770 KB | **258 KB** | **↓ 91%（10.7×）** |

真实应用 Anime `/home`（`--probe`，路由导航重建帧，桌面充分预热）：

| 指标 | 优化前 | 优化后 | 变化 |
|---|---|---|---|
| 重建帧整帧 | 16.73 ms | **11.05 ms** | ↓ 34% |
| build 阶段 | 7.51 ms | **1.97 ms** | **↓ 74%** |
| 每次重建分配 | 5,478 KB | **2,394 KB** | ↓ 56% |

稳态帧不受影响（它本来就不跑 build 阶段）。

### 验证

- `Miko.Ionic.Tests`：1460 + 2 新增，全绿
- `Miko.Tests`：2107 全绿
- `Anime.Verification`（真实应用端到端，19 张截图 + 导航/目录/资料/收藏/奖励/视频/弹幕断言）：PASS
- 新增回归测试两条：一条钉住「同配置组件共享同一已解析主题」，一条钉住
  「调用方持有的主题被改动后 style key 必须变」——后者正是记忆化不能越过的边界

---

## 5. 新基准数据

`benchmarks/Miko.Ionic.Benchmarks`，BenchmarkDotNet 默认 job，视口 390×844（手机竖屏）：

| 基准 | Mean | 分配 |
|---|---|---|
| App page: full frame (style + layout + paint) | 2,341 µs | 697 KB |
| App page: style + layout only | 1,199 µs | 645 KB |
| App page: style resolution only | 767 µs | 965 KB |
| App page: component build only (route navigation) | **62 µs** | **258 KB** |
| Build one complete Material Design theme | 8.8 µs | 37 KB |

分段视图（`--stages`，与真机探针同口径）：

```
elements=157 rules=427
  style  = 0.89 ms/frame  (5.7 us/element)
  layout = 0.43 ms/frame
  paint  = 1.24 ms/frame
  total  = 2.56 ms/frame
```

> 关于「没有 idle frame 基准」：独立构造的 `LayoutEngine` **刻意禁用**布局结果缓存
> （见其无参构造函数——树外元素没有 `Owner`，变更不递增任何计数器，缓存键恒定，
> 启用缓存会静默返回过期结果）。在这个 harness 上测「空闲帧」只会测到应用本来会跳过的
> 全量重排，报出一个应用从不支付的数。空闲帧由核心套件的
> `EngineIdleFrameBenchmarks` 覆盖，那里的缓存是真的。

---

## 6. 仍然敞着的部分

- **绘制是现在稳态帧的最大项**（3.6 ms / 3.6 ms 空闲帧）。ISSUE-144 已经处理过一轮
  （文本 run 缓存、字体与位图复用），进一步优化需要单独立项。
- **重建帧仍有 11 ms**，其中 style 3.0 ms / paint 4.9 ms。build 已从 7.5 降到 2.0，
  下一个值得看的是重建期间的重复绘制（`PaintPasses` 在转场期间为 2）。
- **移动端未实测**。本报告全部数字来自桌面。按 issue 的提示，模拟器 CPU/内存受限，
  绝对值会更大；但 build 阶段占比的结论是结构性的，不依赖平台。真机复测可用
  `MIKO_FRAME_PROBE=verbose`（见 `FrameProbe`）。
