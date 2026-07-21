# Miko 渲染引擎基准测试报告（2026-07-21，ISSUE-096 优化后）

> 本报告是 ISSUE-096 性能优化的**效果验证**，与两份既有报告三方对比：
> - 基线 [`BENCHMARK_REPORT.md`](BENCHMARK_REPORT.md)（2026-06-01，GitHash `1c98a49c`）
> - 回归发现 [`BENCHMARK_REPORT_2026-07-21.md`](BENCHMARK_REPORT_2026-07-21.md)（同日上午，GitHash `8a2dea8`，下称「优化前」）
>
> 测试环境与前两份完全一致（同机 AMD 8700G、同 ShortRun 配置、同命令）。
> 注意：含布局的基准方法已改为每次调用前 `InvalidateCache()`——**测量的是缓存失效时的完整管线**
> （页面打开、Razor 重建、交互动画帧）。稳态帧走缓存复用路径，其成本不在这些数字内（见 §3 探针实测：稳态布局 0 B/op）。

---

## ✅ 结论摘要（TL;DR）

| 维度 | 优化前 → 优化后 | 相对基线 |
|------|----------------|---------|
| **稳态帧**（无变化时） | 每帧全量样式+布局+全绘 → **零布局分配、宿主不产帧** | 结构性修复，优于基线 |
| 每帧管线耗时 | **↓57%–85%**；大页面 27.0 → 5.36 ms | 基线 2.33 ms 的 2.3×（预算内，占 32%） |
| 每帧管线分配 | **↓72%–75%**；大页面 27.9 → 7.6 MB | 基线 4.03 MB 的 1.9× |
| 布局引擎分配 | **↓74%–75%**；1000 子树 55.4 → 13.7 MB | 基线 6.7 MB 的 2.0× |
| 每节点解析分配（探针） | 28.0 KB → **6.8 KB**（↓76%） | ≈ 基线（~6.7 KB） |
| 60fps 预算 | 全部用例回到预算内（最高 32%） | ✅ |

dotMemory 报告的「稳态每帧大量对象创建销毁、GC 锯齿」由两项结构性改动根除：
1. **布局输入版本化**（`Element.MutationVersion`）：输入未变时 `LayoutEngine` 直接复用布局树（实测 0 B/op）；
2. **宿主空闲跳过**：`MikoEngine.HasPendingVisualWork` 为 false 时桌面/模拟器宿主不产帧、不交换缓冲，稳态 CPU/GPU/分配全部归零。

---

## 1. 优化内容（对应回归根因）

| # | 改动 | 文件 | 针对根因 |
|---|------|------|---------|
| 1 | 全局变更版本号：结构/文本/Id/Class/行内样式替换/元素状态均递增；动画帧值、图片/视频内禀尺寸、AddStyleSheet 显式递增 | `Element.cs`、`TextNode.cs`、`MikoEngine.cs`、`AnimationManager.cs` | 稳态每帧全量重排 |
| 2 | `LayoutEngine` 布局树缓存：root/样式表/视口/安全区/版本号全同则复用；新增 `InvalidateCache()`（字体注册、就地改写规则内容等未追踪场景） | `LayoutEngine.cs`、`MikoEngine.cs`（`InvalidateLayoutCache()`） | 同上 |
| 3 | `MikoEngine.Render` 快速路径：布局未变时跳过样式解析、布局与 transition 整树扫描 | `MikoEngine.cs` | 稳态每帧全量管线 |
| 4 | `HasPendingVisualWork` / `HasPendingWork`；桌面与模拟器宿主空闲时跳过帧生产与缓冲交换（首帧/resize 兜底强制呈现；模拟器改为手动交换） | `MikoEngine.cs`、`MikoInteractionController.cs`、`SilkDesktopHost.cs`、`SimulatorHost.cs` | dotMemory 锯齿、稳态 CPU |
| 5 | `StyleProperty<T>` 瘦身：var/calc/keyword 共享单一对象槽（罕见路径装箱），常见路径零负载——`new Style()` 12,688 → **4,592 B** | `StyleProperty.cs` | 样式对象体积 |
| 6 | `StyleResolver` 池化：baseStyle 与匹配规则列表无锁池化复用（`Style.Reset()` 由源生成器新增）；排序改全序 `List.Sort`（零分配）；UA 默认样式的 `TagName.ToLower()` 分配消除 | `StyleResolver.cs`、`StylePropertyGenerator.cs`、`Style.cs` | 每节点 28 KB 分配 |
| 7 | `ComputedStyle` 的 Transitions/Animations 列表懒初始化（读取走 `*OrNull`） | `ComputedStyle.cs`、`MikoEngine.cs` | 每节点 2 个空 List |
| 8 | TextNode 换行测量缓存：以全部测量输入为键，重排时命中即零分配 | `TextNode.cs`、`TextLayout.cs` | 每文本节点每次布局的换行分配 |

**基准适配**：含布局的 15 个基准方法每次调用前 `InvalidateCache()`，保证测量的是完整管线而非缓存命中。

**行为契约变化**：样式表对象图按**不可变**约定对待——就地改写已添加样式表的规则内容后，需调用 `engine.InvalidateLayoutCache()`（原先隐式生效）。相应测试已更新。

---

## 2. 每帧管线（Full Frame Pipeline）三方对比

| 基准方法 | 基线 | 优化前 | **优化后** | 优化前→后 | vs 基线 | 预算占比 |
|---------|------|--------|-----------|----------|--------|---------|
| `FullFrame_RealisticPage` | 0.48 ms / 838 KB | 1.93 ms / 5.11 MB | **0.83 ms / 1.43 MB** | ↓57% / ↓72% | 1.7× / 1.7× | **5.0%** ✅ |
| `FullFrame_LargePage` | 2.33 ms / 4.03 MB | 27.0 ms / 27.9 MB | **5.36 ms / 7.57 MB** | ↓80% / ↓73% | 2.3× / 1.9× | **32%** ✅ |
| `IncrementalFrame_RealisticPage_SingleDirty` | 0.23 ms / 701 KB | 1.70 ms / 4.96 MB | **0.55 ms / 1.28 MB** | ↓67% / ↓74% | 2.4× / 1.9× | **3.3%** ✅ |
| `IncrementalFrame_LargePage_SingleDirty` | 0.99 ms / 3.29 MB | 23.7 ms / 27.1 MB | **3.53 ms / 6.71 MB** | ↓85% / ↓75% | 3.6× / 2.0× | **21%** ✅ |

> 上表为缓存失效的完整管线（worst case）。真实稳态增量帧经 `MikoEngine.Update` 时布局复用，
> 实际成本仅剩脏区域绘制（≈ RenderDirty 单项）；无脏区域时宿主不产帧。

---

## 3. 分配探针实测（`GC.GetAllocatedBytesForCurrentThread`）

| 测量项 | 基线（估） | 优化前 | **优化后** | 变化 |
|--------|-----------|--------|-----------|------|
| 稳态缓存布局（第二次 Layout 同输入） | —（无缓存） | —（无缓存） | **0 B/op** | ✨ 结构性 |
| `new Style()` | ~2–3 KB | 12,688 B | **4,592 B** | ↓64% |
| `StyleResolver.Resolve`（每节点） | ~6 KB | 28,040 B | **6,768 B** | ↓76% |
| 完整布局（每节点，缓存失效） | ~6.7 KB | 28,336 B | **7,022 B** | ↓75% |
| 完整布局（1000 子树 / 2001 节点） | 6.7 MB | 55.4 MB | **14.1 MB** | ↓75% |

每节点分配已回到基线量级。剩余差距来自特性增长：TextNode 使节点数翻倍、样式属性 71→89 个、
flex/table 布局新算法——属功能演进而非纯回归。

---

## 4. 各模块详细对比

### 4.1 Layout 布局引擎

| 基准方法 | 基线 | 优化前 | **优化后** | 优化前→后（耗时/分配） |
|---------|------|--------|-----------|----------------------|
| `BlockLayout_SmallTree` | 14.2 μs / 74 KB | 82.3 μs / 581 KB | **38.1 μs / 144 KB** | ↓54% / ↓75% |
| `BlockLayout_LargeTree` | 2,193 μs / 6.7 MB | 37,353 μs / 55.4 MB | **15,847 μs / 13.7 MB** | ↓58% / ↓75% |
| `BlockLayout_DeepNesting` | 72.2 μs / 344 KB | 231.7 μs / 1,438 KB | **123.3 μs / 357 KB** | ↓47% / ↓75% |
| `FlexLayout_FewChildren` | 15.1 μs / 75 KB | 79.2 μs / 583 KB | **43.9 μs / 146 KB** | ↓45% / ↓75% |
| `FlexLayout_ManyChildren` | 301 μs / 1.4 MB | 6,497 μs / 11.1 MB | **1,017 μs / 2.78 MB** | ↓84% / ↓75% |
| `InlineLayout_ManyElements` | 136 μs / 680 KB | 1,013 μs / 5.6 MB | **466 μs / 1.43 MB** | ↓54% / ↓74% |
| `MixedLayout_Realistic` | 143 μs / 690 KB | 987 μs / 5.1 MB | **448 μs / 1.30 MB** | ↓55% / ↓74% |

分配全面降至优化前的 1/4、基线的 ~2×。耗时仍高于基线（2–7×）：布局算法与样式解析的
**CPU** 随特性增长（节点翻倍、属性增多、flex 重写），属结构性成本；大树的 Gen2 GC 已从
每次 1.07 次降到 0.78 次。

### 4.2 Style 样式解析

| 基准方法 | 基线 | 优化前 | **优化后** | 优化前→后（耗时/分配） |
|---------|------|--------|-----------|----------------------|
| `Resolve_FewRules` | 14.1 μs / 74 KB | 77.6 μs / 581 KB | **40.5 μs / 144 KB** | ↓48% / ↓75% |
| `Resolve_ManyRules` | 1,008 μs / 1.1 MB | 3,027 μs / 6.0 MB | **1,561 μs / 1.48 MB** | ↓48% / ↓75% |
| `Resolve_ComplexSelectors` | 2,185 μs / 1.6 MB | 5,972 μs / 6.5 MB | **3,134 μs / 1.53 MB** | ↓48% / ↓76% |
| `Resolve_DeepInheritance` | 312 μs / 353 KB | 697 μs / 1.0 MB | **411 μs / 252 KB** | ↓41% / ↓76% |

`Resolve_ComplexSelectors`（1.53 MB）与 `Resolve_DeepInheritance`（252 KB）分配已**回到甚至优于基线**。

### 4.3 Render 渲染引擎（本组路径未改动，作对照）

| 基准方法 | 基线 | 优化前 | **优化后** |
|---------|------|--------|-----------|
| `FullRender_SmallTree` | 86.3 μs | 94.4 μs | 95.3 μs |
| `FullRender_LargeTree` | 1,365 μs | 1,825 μs | 1,762 μs |
| `IncrementalRender_SingleDirty` | 66.2 μs | 62.0 μs | 63.2 μs |
| `IncrementalRender_ManyDirty` | 1,787 μs | 1,141 μs | 1,245 μs |
| `RenderWithText` | 329 μs | 379 μs | 411 μs |

与优化前持平（差异在 ShortRun 噪声内），印证渲染模块未受本轮改动影响。

### 4.4 拐点分析（增量 vs 全量，500 元素树）

优化后 Ratio：1→0.16、2→0.16、3→0.18、5→0.19、8→0.24、10→0.28、15→0.47、20→0.54、30→1.05、50→2.26。
拐点仍在 **30 附近**，`MaxIncrementalDirtyRegions = 30` 阈值继续成立。

### 4.5 DirtyRegion / TreeTraversal

与基线、优化前一致（零分配保持，耗时为噪声级差异）。

---

## 5. 稳态行为（dotMemory 锯齿的修复验证方式）

基准无法直接测稳态（BenchmarkDotNet 每次调用都失效缓存）。验证路径：

1. **探针实测**（本报告 §3）：同输入第二次 `Layout` = **0 B/op**。
2. **宿主**：桌面（`SilkDesktopHost`）与模拟器（`SimulatorHost`）在 `HasPendingWork == false`
   时跳过帧生产与缓冲交换并休眠 1ms——稳态下无绘制、无交换、无分配，CPU 占用趋零。
   首帧/resize/输入事件/动画/视频/排队回调均会正确唤醒（`_needsPresent` 兜底 + 版本号检测）。
3. **移动端**（Android/iOS）：引擎内快速路径（布局复用 + Render 跳过样式/布局/transition 扫描）
   自动生效，帧成本降为纯绘制；整帧跳过未在移动端宿主实现（display-link 驱动，保持每帧回调），
   如需可后续按桌面同样模式接入 `HasPendingVisualWork`。

预期 dotMemory 表现：页面打开一次全量解析（分配已降至 1/4），之后内存曲线拉平；
交互（hover/点击/动画）期间有真实工作对应的真实分配，动画结束后回归零分配。

---

## 6. 已知边界与后续建议

1. **未追踪变更需显式失效**：就地改写样式表规则内容、运行时注册字体后，调用
   `engine.InvalidateLayoutCache()`。直接改写 `element.Style.X` 属性后调用
   `engine.InvalidateElement(element)`（引擎内动画已自动处理）。
2. **布局 CPU 仍高于基线 2–7×**（特性增长所致）：如需进一步压缩，可 profiling 后考虑
   样式解析的属性级增量缓存、`Length` 存储瘦身（48 B → 分量外置）、flex 算法复核。
3. **移动端宿主整帧跳过**未实现（见 §5.3）。
4. `FindByClass` 123 KB 分配（基线遗留建议）未处理。

---

## 7. 运行方式

```bash
# 运行全部基准测试（生成本报告所用命令；含布局用例已内置 InvalidateCache）
dotnet run -c Release --project benchmarks/Miko.Benchmarks -- --filter "*" --job short
```

原始结果由 BenchmarkDotNet 导出至 `BenchmarkDotNet.Artifacts/results/`（仓库根目录，gitignored）。
