# Miko 渲染引擎基准测试报告

> 本报告是**性能基线（baseline）**，下次基准测试时用于回归对比。
> 对比方法：用相同命令重新运行后，按「基准方法名（Benchmark）」逐行比对 Mean 与 Allocated。
> 注意：本报告使用 BenchmarkDotNet `ShortRun`（仅 3 次迭代），测量噪声较大（部分用例 Error ≥ Mean），
> **单次 ±20% 以内的波动应视为噪声，不构成性能变化**。判定回归/优化需关注 ≥1.5× 的稳定偏移，并最好以更长 job 复测确认。

> 📌 **本基线包含 ISSUE-036 性能优化**（文本测量缓存 + 字形缓存 + 增量渲染脏区域阈值），
> 相对上一基线（GitHash `798682b8`）含文本的布局/帧管线用例耗时与分配显著下降，详见各节「与上一基线对比」。

---

## 测试元数据（对比时需核对环境一致性）

| 项 | 值 |
|----|----|
| 报告生成日期 | 2026-06-01 |
| CPU | AMD Ryzen 7 8700G w/ Radeon 780M Graphics（16 逻辑核 / 8 物理核） |
| 操作系统 | Windows 11 (10.0.26200.8457) |
| .NET SDK | 10.0.300 |
| 运行时 | .NET 10.0.8 (10.0.826.23019), X64 RyuJIT AVX-512 |
| BenchmarkDotNet | v0.14.0 |
| Job | ShortRun（IterationCount=3, LaunchCount=1, WarmupCount=3） |
| GitHash | `1c98a49c`（ISSUE-036 优化后；上一基线 `798682b8`） |

> ⚠️ 跨报告对比时，若 CPU / 运行时大版本 / Job 配置不同，数值不可直接比较，仅可比较「相对关系」（如增量 vs 全量倍率、模块间排序）。

---

## 1. 每帧管线（Full Frame Pipeline）

完整帧 = 样式解析 + 布局 + 渲染，端到端测量。60fps 预算为 16.6 ms。

| 基准方法 | 场景 | 每帧耗时 (Mean) | 内存分配 | 60fps 预算占比 |
|---------|------|---------------|---------|--------------|
| `FullFrame_RealisticPage` | 真实页面 ~90 元素（全量） | **0.48 ms** | 838 KB | 2.9% |
| `FullFrame_LargePage` | 大页面 500 元素（全量） | **2.33 ms** | 4.03 MB | 14% |
| `IncrementalFrame_RealisticPage_SingleDirty` | 真实页面（增量，1 脏区域） | **0.23 ms** | 701 KB | 1.4% |
| `IncrementalFrame_LargePage_SingleDirty` | 大页面（增量，1 脏区域） | **0.99 ms** | 3.29 MB | 6.0% |

**结论**：真实页面每帧 ~0.48 ms，60fps 预算内极充裕。增量渲染（单脏区）在真实页面省约 52% 帧时间、在大页面省约 57%。

**与上一基线对比**：真实页面全量帧 1.40 ms → **0.48 ms（↓66%）**，大页面全量帧 4.63 ms → **2.33 ms（↓50%）**。
收益主要来自 ISSUE-036 的文本测量缓存——每帧不再对相同文本重复测量。

---

## 2. 局部重绘 vs 全量重绘 — 拐点分析

500 元素树，`DirtyRegionTippingPointBenchmarks`，不同脏区域数量下增量渲染相对全量渲染的比值（Ratio）。

| 脏区域数 | 增量渲染 (Mean) | Ratio（相对全量） | 结论 |
|---------|---------------|-----------------|------|
| 1 | 321 μs | 0.13 | 快 ~8 倍 |
| 2 | 349 μs | 0.16 | 快 ~6 倍 |
| 3 | 375 μs | 0.18 | 快 ~6 倍 |
| 5 | 448 μs | 0.20 | 快 ~5 倍 |
| 8 | 570 μs | 0.26 | 快 ~4 倍 |
| 10 | 675 μs | 0.32 | 快 ~3 倍 |
| 15 | 1,033 μs | 0.47 | 快 ~2 倍 |
| 20 | 1,632 μs | 0.79 | 快 ~1.3 倍 |
| **30** | **2,674 μs** | **1.24** | **比全量慢 ~24%** |
| **50** | **5,398 μs** | **2.65** | **比全量慢 ~2.6 倍** |

全量渲染基线（baseline）在各脏区域数下稳定在 ~2.0–2.4 ms / 777 KB。

**拐点在 20–30 个脏区域之间**：脏区域 ≤20 时增量渲染仍占优，达到 30 时已劣于全量（Ratio 1.24），50 时退化为全量的约 2.6 倍。

✅ **已实现**：`RenderEngine.MaxIncrementalDirtyRegions`（默认 **30**），`MikoEngine.Update` 在脏区域超过该阈值时回退全量渲染，避免 N 次全树遍历退化。
> 注：本基准直接调用 `RenderEngine.RenderDirty`、绕过 `MikoEngine.Update`，故仍展示未回退时的退化曲线；阈值回退本身由单元测试 `IncrementalRenderThresholdTests` 覆盖。

---

## 3. 各模块详细基准

### 3.1 Layout 布局引擎（`LayoutBenchmarks`）

| 基准方法 | 规模 | 耗时 (Mean) | 内存分配 |
|---------|------|------------|---------|
| `BlockLayout_SmallTree` | 10 子元素 | 14.2 μs | 74 KB |
| `BlockLayout_LargeTree` | 1000 子元素 | 2,193 μs | 6,703 KB |
| `BlockLayout_DeepNesting` | 50 层嵌套 | 72.2 μs | 344 KB |
| `FlexLayout_FewChildren` | 10 子元素 | 15.1 μs | 75 KB |
| `FlexLayout_ManyChildren` | 200 子元素 | 301 μs | 1,373 KB |
| `InlineLayout_ManyElements` | 100 元素 | 136 μs | 680 KB |
| `MixedLayout_Realistic` | 真实页面 | 143 μs | 690 KB |

**分析**：含文本的用例受 ISSUE-036 文本测量缓存影响显著下降——
`InlineLayout_ManyElements` 661 μs → **136 μs（↓79%）**，`MixedLayout_Realistic` 843 μs → **143 μs（↓83%）**，
`BlockLayout_SmallTree` 37.9 μs → **14.2 μs（↓63%）**。文本测量曾是 Inline 布局主要开销，现已基本消除。
Flex 线性扩展依旧良好。Block 大规模（1000 子元素）2.2 ms / 6.7 MB 仍是最重单项（受 ShortRun 噪声影响，Error≥Mean），是大型静态列表的主要成本来源。

### 3.2 Style 样式解析（`StyleResolutionBenchmarks`）

> 注：这些用例实际执行的是「样式解析 + 完整布局」，因此也受布局算法变化影响。

| 基准方法 | 规模 | 耗时 (Mean) | 内存分配 |
|---------|------|------------|---------|
| `Resolve_FewRules` | 5 条规则 / 10 元素 | 14.1 μs | 74 KB |
| `Resolve_ManyRules` | 100 条规则 / 100 元素 | 1,008 μs | 1,148 KB |
| `Resolve_ComplexSelectors` | 150 条复杂选择器 / 100 元素 | 2,185 μs | 1,583 KB |
| `Resolve_DeepInheritance` | 100 条规则 / 30 层继承 | 312 μs | 353 KB |

**分析**：样式解析是 O(元素数 × 规则数) 复杂度，复杂选择器场景内存分配 >1.5 MB，是大型应用的主要瓶颈之一。
（`Resolve_FewRules` 36.5 μs → 14.1 μs 的下降来自其内联文本测量被缓存。）
可考虑选择器索引（按 tag/class/id 分桶）或解析结果缓存。

### 3.3 Render 渲染引擎（`RenderBenchmarks`）

| 基准方法 | 规模 | 耗时 (Mean) | 内存分配 |
|---------|------|------------|---------|
| `FullRender_SmallTree` | 10 元素全量 | 86.3 μs | 14 KB |
| `FullRender_LargeTree` | 500 元素全量 | 1,365 μs | 777 KB |
| `IncrementalRender_SingleDirty` | 1 脏区域增量 | 66.2 μs | 16 KB |
| `IncrementalRender_ManyDirty` | 20 脏区域增量 | 1,787 μs | 867 KB |
| `RenderWithText` | 真实页面含文本 | 329 μs | 148 KB |

**分析**：增量渲染单区域（66μs）比全量渲染 500 元素（1,365μs）快约 20 倍，脏区域优化有效。
`RenderWithText` 505 μs → **329 μs（↓35%）**，含文本渲染中的测量开销被缓存挡下。
20 个脏区域时退化到与全量持平，印证 §2 的拐点结论。

### 3.4 DirtyRegion 脏区域管理（`DirtyRegionBenchmarks`）

| 基准方法 | 场景 | 耗时 (Mean) | 内存分配 |
|---------|------|------------|---------|
| `AddRegions_NonOverlapping` | 50 个不重叠区域 | 16.9 μs | 0 |
| `AddRegions_Overlapping` | 50 个重叠区域 | 10.7 μs | 0 |
| `AddRegions_Adjacent` | 50 个相邻区域 | 11.2 μs | 0 |

**分析**：脏区域管理零堆分配，非常轻量，不是瓶颈。
（注：本组用例的 Error 普遍 ≥ Mean，绝对耗时仅供量级参考。）

### 3.5 TreeTraversal DOM 树遍历（`TreeTraversalBenchmarks`，1000 节点）

| 基准方法 | 场景 | 耗时 (Mean) | 内存分配 |
|---------|------|------------|---------|
| `PreOrder_LargeTree` | 前序遍历 | 2.58 μs | 88 B |
| `PostOrder_LargeTree` | 后序遍历 | 1.81 μs | 88 B |
| `LevelOrder_LargeTree` | 层序遍历 | 5.94 μs | 33 KB |
| `FindById_LargeTree` | 按 ID 查找 | 4.38 μs | 0 |
| `FindByClass_LargeTree` | 按 Class 查找 | 31.1 μs | 123 KB |

**分析**：前序/后序遍历几乎零分配。`FindByClass` 的 123 KB 分配来自递归创建 List 并 AddRange，可优化为传入共享 List 单次遍历收集。层序遍历的 33 KB 来自 Queue 分配。

---

## 4. 主要优化建议（按收益排序）

1. ✅ **文本测量缓存** — 已在 ISSUE-036 实现：`TextMeasurer` 缓存 (文本,字体,字号,字重)→尺寸，含文本布局耗时下降 60–83%。
2. ✅ **增量渲染阈值** — 已在 ISSUE-036 实现：`RenderEngine.MaxIncrementalDirtyRegions=30`，脏区域超过即回退全量渲染（§2）。
3. **Block 大树布局** — 1000 子元素 ~2.2 ms / 6.7 MB，是最重单项；考虑布局缓存 / 跳过未变化子树。
4. **样式解析内存** — 复杂选择器场景 >1.5 MB 分配，可加选择器索引或解析缓存（§3.2）。
5. **FindByClass** — 递归 List 合并导致 123 KB 分配，可优化为共享 List。

---

## 5. 运行方式（复测时请使用相同命令以保证可比性）

```bash
# 运行全部基准测试（生成本报告所用命令）
dotnet run -c Release --project benchmarks/Miko.Benchmarks -- --filter "*" --job short

# 仅运行帧管线和拐点测试
dotnet run -c Release --project benchmarks/Miko.Benchmarks -- --filter "*FramePipeline*" "*TippingPoint*" --job short

# 运行特定类
dotnet run -c Release --project benchmarks/Miko.Benchmarks -- --filter "*LayoutBenchmarks*"
```

原始结果（CSV / HTML / GitHub-md）由 BenchmarkDotNet 导出至 `BenchmarkDotNet.Artifacts/results/`。
