# 基准测试报告：路由跳转卡顿的综合分析（ISSUE-146）

> 本报告回答 ISSUE-146 的三个问题：
> 1. Anime 示例在 Android 上 Tab 切换、打开/返回详情页仍有顿挫，瓶颈在哪？
> 2. 是不是 JIT 造成的？Android 上改用 AOT 能不能消除？
> 3. 还有没有优化空间？有的话做了什么、效果如何？

---

## 测试元数据

| 项 | 值 |
|----|----|
| 报告日期 | 2026-09-23 |
| 桌面 CPU | AMD Ryzen 7 8700G（16 逻辑核 / 8 物理核） |
| 桌面系统 | Windows 11 Pro (10.0.26200) |
| Android 设备 | Android Emulator `sdk_gphone64_x86_64`，API 36，8 vCPU / 2 GB，1080×2400 @ 420dpi，60 Hz，GLES 走主机 Radeon 780M |
| .NET SDK / Android workload | 10.0.401 / android 36.1.69 |
| BenchmarkDotNet | v0.14.0 |
| GitHash（优化前基线） | `4ba271d` |
| Android 默认构建 | Release、Mono、**Profiled AOT**（`AndroidEnableProfiledAot=true`，SDK 默认）、SdkOnly 裁剪 |

**真机场景**（`examples/App/Anime/Anime.Android/frame-probe-scenario.sh`，可一条命令复现）：
冷启动 → 首页/福利/我的 三个 Tab 来回 3 轮 → 打开详情页/返回 3 轮。探针 `--ez frameprobe-verbose true`
逐帧记录，每一步在 logcat 里打标记，所以每一帧都能对应到触发它的那次点击。每个构建至少跑两遍。

---

## 1. 真机现状：卡的是哪一帧

优化前（基线 `4ba271d`，默认构建，两轮中位数，单位 ms）：

| 场景 | 重建帧 | 其中样式阶段 |
|---|---:|---:|
| Tab 切换，首次进入（冷） | 108 | 19 |
| Tab 切换，之后（热） | **41** | **32** |
| 打开详情页，首次（冷） | **480** | 142 |
| 打开详情页，之后（热） | **198** | **139** |
| 从详情页返回 | **84** | 52 |
| 停在详情页上（视频播放中）每秒一次的重排帧 | **79** | **68** |

稳态绘制帧只有 2–5 ms（ISSUE-144 之后已经很便宜）。**卡顿全部集中在「重建帧」与「重排帧」**，
而其中最大的一项是**样式阶段**——热路径上 Tab 切换 32 ms、打开详情 139 ms。

最后一行是 issue 里没有提到、但同样影响观感的：停留在详情页时，**每秒都有一帧 70–90 ms**。

---

## 2. 是 JIT 吗？AOT 能不能解决？

为此构建了同一份代码的四种运行时变体，跑同一个场景：

| 变体（优化前代码） | 包体 | Tab 热切换 | 打开详情（热） | 返回 | 详情页每秒重排帧 |
|---|---:|---:|---:|---:|---:|
| Mono + Profiled AOT（**默认**） | 17.9 MB | 41 (样式 32) | 198 (样式 139) | 84 | 79 (样式 68) |
| Mono + **完全 AOT** | 26.2 MB | 38 (样式 30) | 179 (样式 146) | 65 | 76 (样式 66) |
| Mono + 完全 AOT + **LLVM** | 25.4 MB | 16 (样式 5) | 66 (样式 38) | 30 | 21 (样式 11) |
| **CoreCLR**（实验性，R2R + 分层 JIT） | 32.5 MB | 11 (样式 4) | 42 (样式 13) | 28 | 16 (样式 6) |

**结论：瓶颈不是 JIT 预热，是 Mono 生成的代码质量。**

- Profiled AOT 的默认配置下，热路径代码早已是 AOT 过的；换成**完全 AOT** 对稳态几乎没有影响
  （样式阶段 139 → 146 ms）。所以「冷代码」不是主因。
- 同样是 AOT，打开 **LLVM** 后端，样式阶段快了 4–6 倍；CoreCLR 的 RyuJIT 再快一倍。
  样式阶段是「每个 CSS 属性一个分支」的巨量直线代码加上选择器匹配里的逐字符循环，
  Mono 的默认代码生成器对这类代码尤其吃亏。
- **冷启动那一帧**（首次进入某个页面，build 阶段 45–190 ms）才是真正的「冷代码」成本，
  而它恰恰是完全 AOT 能改善的部分（480 → 283 ms）。

EventPipe 采样（`dotnet-dsrouter` + `dotnet-trace`，默认构建，详情页来回 4 轮）对上了这个结论：
渲染线程上 **30% 的 CPU 在样式阶段，其中约 80% 在后代选择器的祖先链遍历**
（`DescendantSelector.Matches` → `Element.HasClass` → `char.IsWhiteSpace`）。
`RuntimeHelpers.CompileMethod`（真正的 JIT）只占 3.6%。

### 关于 issue 里提到的 AOT 选项

| 选项 | .NET 10 状态 | 本次实测 | 建议 |
|---|---|---|---|
| Mono 完全 AOT（`AndroidEnableProfiledAot=false`） | 正式支持 | 冷启动帧 -40%，稳态无改善，包体 +8 MB | 只在冷启动敏感时用 |
| Mono AOT + LLVM（`EnableLLVM=true`） | 正式支持 | 稳态样式阶段快 4–6 倍，包体 +7.5 MB，构建慢约 1 分钟 | **目前最值得的发布配置** |
| CoreCLR（`UseMonoRuntime=false`） | **实验性** | 最快（样式再快 1 倍），包体 +14.6 MB | .NET 11 起为默认，现在可以开始验证 |
| NativeAOT（`PublishAot=true`） | **实验性**，需要 NDK 26.1+ | 本机未装 NDK，没有测 | 等 .NET 11 的 CoreCLR 默认后再评估 |

---

## 3. 框架侧找到并修复的问题

运行时只是放大器——**同样的冗余工作在任何运行时上都在做**。下面这些都是在 profile 和帧日志里
逐一定位出来的，与运行时无关。

### 3.1 后代选择器没有祖先过滤（样式阶段的主体）

Ionic 样式表 650 条规则里有 338 条后代组合器，典型形状是：

```
.ion-button.md.button-clear.ion-color-primary.ion-theme-xxx .button-native
```

`RuleIndex` 按最右侧的 `.button-native` 分桶，于是**每个** `.button-native` 元素都要测试整个桶的
上百条规则，每条失败的规则都要把祖先链一直走到根。在详情页上这一类规则占匹配耗时的 **89%**，
命中率不到 5%。

**修复**：`Styling/AncestorFilter.cs`——浏览器样式引擎的标准做法。样式阶段随 DOM 递归维护一个
祖先类名/ID/标签的计数布隆过滤器；每条规则预先算好「匹配时祖先链上必须出现的键」，缺键即否决，
免去整条祖先链的逐级测试。只做否决、不做肯定；`:not`、兄弟组合器左侧、分组一律不贡献键（保守）。

### 3.2 `HasClass` 的逐字符分词

改为整串 `IndexOf`（向量化）+ 两侧分隔符核对。绝大多数调用是「不含」，失败时几乎不进逐字符逻辑。
语义与分词逐一等价（含「整串相等即命中」这个既有快路径）。

### 3.3 只改文字的帧重跑整树级联

播放器每秒刷新一次时间文字（`0:01 / 0:03`）。`TextContent` setter 每次都删掉文本节点再重建，
被记成**结构变更**，于是 340 个元素的整树级联每秒跑一遍——这就是详情页上每秒一次的 70–90 ms。

**修复**：`MutationTracker` 区分三种影响范围：

| 记账方式 | 触发 | 样式阶段 |
|---|---|---|
| `Bump` | 结构、class/id、元素状态… | 整树级联 |
| `BumpInlineStyle(element)` | 替换某元素的行内样式对象 | **只重算该元素子树** |
| `BumpContent` | 已有文本节点的文字、图片/视频内禀尺寸 | **不级联，直接沿用计算样式** |

`TextContent` setter 在「恰好一个前置文本节点」时就地改写文字（结构完全相同）。
正确性由 `Selector.MayReadContentOrInlineStyle` 兜底：样式表里只要有可能读文字或行内样式的选择器
（`[text=…]`、`TypedStyleBuilder.Where` 的任意谓词、未知的自定义选择器），就一律退回整树重算。
`:empty` 的空↔非空翻转按样式变更记账。

### 3.4 行内样式替换重跑整树级联

打开详情页的那一帧级联了**两遍**：播放器在首次布局后按宽度写入 16:9 高度（替换 `_viewport.Style`），
被当成全局样式变更。选择器读不到行内样式，受影响的只有该元素子树（继承、em、变量作用域），
现在只重算这棵子树（见上表）。

### 3.5 重建帧画两遍

`MikoInteractionController.RenderFrame` 里 `Rebuild` → `Engine.Initialize` 先整树绘制一遍，
紧接着宿主回调**清屏**再 `Engine.Render` 一遍。第一遍被原样清掉。现在 `RenderFrame` 的重建不预绘
（`Initialize(..., paint: false)`），公开的 `Rebuild` 与 `Initialize` 行为不变。
探针 `passes=2/2/2` → `2/1/1`。

### 3.6 已缓存图片让刚算完的布局立即过期

`SyncImageSources` 在布局**之后**调用；解码缓存里已有的图片同步完成加载、写入内禀尺寸，
刚算完的布局随即过期，下一次绘制再整树重排一遍。挪到布局之前。

### 3.7 每个组件类型构建时抛一次异常

`ComponentParameterCache.CreateSetter` 先试 `CreateDelegate<Action<object, object?>>()`，失败再回退。
开放实例委托的首参必须是方法能接收的类型，`object` 永远不是组件类型——**每个** `[Inject]` /
`[CascadingParameter]` 属性都抛一次 `ArgumentException` 再被吞掉（桌面实测一轮 181 次，
Android 打开详情页一次 49 次）。行为测试永远是绿的，代价只落在「首次构建该组件类型」的那一帧上，
而 Mono 上抛异常远比 CoreCLR 贵。现在直接用 `PropertyInfo.SetValue`——这本来就是一直在跑的路径。

---

## 4. 效果

### 4.1 真机（默认构建，Mono Profiled AOT，两轮中位数，ms）

| 场景 | 优化前 | 优化后 | 变化 |
|---|---:|---:|---:|
| Tab 切换，首次进入（冷） | 108 (样式 19) | 101 (样式 13) | -6% |
| Tab 切换，之后（热） | **41** (样式 32) | **15** (样式 4) | **-63%** |
| 打开详情页，首次（冷） | 480 (样式 142) | 325 (样式 23) | -32% |
| 打开详情页，之后（热） | **198** (样式 139) | **66** (样式 18) | **-67%** |
| 从详情页返回 | **84** (样式 52) | **37** (样式 9) | **-56%** |
| 详情页每秒一次的重排帧 | **79** (样式 68) | **12** (样式 0.1) | **-85%** |

每一帧的绘制遍数：`passes=…/2` → `…/1`。

### 4.2 各运行时 × 优化后

| 变体（优化后代码） | Tab 热切换 | 打开详情（热） | 返回 | 详情页重排帧 |
|---|---:|---:|---:|---:|
| Mono Profiled AOT（默认） | 15 | 66 | 37 | 12 |
| Mono AOT + LLVM | 15 | 49 | 14 | 14 |
| CoreCLR | 14 | 35 | 32 | 10 |

优化后默认构建已经接近优化前的 LLVM/CoreCLR 水平；运行时之间的差距从 3–6 倍缩小到 1–2 倍，
因为被消掉的正是 Mono 代码生成最吃亏的那部分工作。

### 4.3 桌面基准

`benchmarks/Miko.Ionic.Benchmarks`（Anime `/home` 形状的真实 Ionic 页面，BenchmarkDotNet 默认 job）：

| 基准 | 优化前 | 优化后 | 变化 |
|---|---:|---:|---:|
| App page: style resolution only | 693 µs | 606 µs | -13% |
| App page: style + layout only | 1,175 µs | 958 µs | -18% |
| App page: full frame | 2,520 µs | 2,203 µs | -13% |
| App page: component build only | 62 µs | 66 µs | 噪声内 |

新增 `EngineFrameBenchmarks`——**必须经 `MikoEngine`** 才测得出「不跑某个阶段」的优化
（`AppPageFrameBenchmarks` 用的是禁用缓存的独立 `LayoutEngine`，每帧都是整树重算）：

| 基准 | 优化后 | 说明 |
|---|---:|---|
| Engine frame: one text node changes | 1.53 ms（样式 ~10 µs） | 文字变化：只重排 |
| Engine frame: one class changes | 2.22 ms（样式 ~700 µs） | 对照组：仍整树级联 |
| Engine frame: route rebuild | 2.40 ms | `Initialize` + `Render`，一遍绘制 |

> 桌面上样式阶段的收益（-13%）远小于真机（-85% ~ -95%）。这与第 2 节一致：
> 桌面 RyuJIT 本来就能把逐字符循环和祖先遍历跑得很快，被消掉的冗余工作在 Mono 上贵得多。
> **桌面基准会系统性地低估 Android 上的收益（以及成本）**——这是 ISSUE-145 结论的延续。

`benchmarks/Miko.Benchmarks` 核心套件（ShortRun，100 项）：无回归（没有任何一项 ≥1.5× 变慢）。
变快超过 1.5× 的：

| 基准 | 优化前 | 优化后 | 对照组「强制全量重排」 |
|---|---:|---:|---:|
| Deep inline-style change, 50 行 | 1,340 µs | **518 µs** | 1,232 → 1,252 µs |
| Shallow change, 50 行 | 1,216 µs | **524 µs** | 〃 |
| Deep inline-style change, 200 行 | 5,758 µs | **1,986 µs** | 5,999 → 5,836 µs |
| Shallow change, 200 行 | 5,634 µs | **1,949 µs** | 〃 |

对照组不变，因此这 2.5–2.9 倍是真实收益（3.4 行内样式只重算子树的直接体现），不是机器状态差异。
另有 `DirtyRegionTippingPointBenchmarks` 的几项变快，但 ShortRun 下 Error ≥ Mean，视为噪声。

---

## 5. 仍然存在的瓶颈与建议

按对用户观感的影响排序：

1. **首次进入页面的冷帧（100–330 ms）**。build 阶段 45–190 ms，是真正的「冷代码」成本
   （首次执行的方法、首次构建的组件类型、首次解析的主题）。
   - 立即可做：发布构建启用 `EnableLLVM=true`（冷帧 -60%，稳态也更快，包体 +7.5 MB）。
   - 中期：收集 Anime 自己的 AOT profile（`AndroidEnableProfiledAot` + 自定义 `.aotprofile`），
     让 Profiled AOT 覆盖详情页与播放器路径。
   - 长期：.NET 11 CoreCLR 默认化后迁移；届时 NativeAOT 也会可用。
2. **打开详情页的冷帧绘制 60–140 ms**：首帧要解码海报图、创建视频纹理、栅格化 SVG 图标。
   可以考虑在导航前预热（预解码下一页的图片）或把首帧拆成两帧（先出骨架）。
3. **详情页打开仍有两遍布局**（`passes=2/2/1`）：第二遍来自播放器首次拿到宽度后写入高度。
   根治需要 `aspect-ratio`（或 padding-top 百分比）让它在首遍布局就确定——这是 Miko 目前缺的 CSS 能力。
4. **转场（返回）期间每帧整树画两个图层**（每帧 5–6 ms、488 个盒子）。可以考虑把 leaving 层
   栅格化成一张纹理再平移——一次绘制、之后只贴图。

## 复现

```bash
# 桌面基准
dotnet run -c Release --project benchmarks/Miko.Ionic.Benchmarks -- --filter "*"
dotnet run -c Release --project benchmarks/Miko.Benchmarks -- --filter "*" --job short

# 真机（模拟器）
dotnet build examples/App/Anime/Anime.Android/Anime.Android.csproj -c Release -p:RuntimeIdentifier=android-x64
examples/App/Anime/Anime.Android/frame-probe-scenario.sh after \
  examples/App/Anime/Anime.Android/bin/Release/net10.0-android/android-x64/com.miko.anime-Signed.apk

# 运行时对比：在 build 命令后追加
#   -p:AndroidEnableProfiledAot=false                  完全 AOT
#   -p:AndroidEnableProfiledAot=false -p:EnableLLVM=true   AOT + LLVM
#   -p:UseMonoRuntime=false                             CoreCLR（实验性）
```
