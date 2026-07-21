# Miko 渲染引擎基准测试报告（2026-07-21）

> 本报告为第 3 次基准测量，与上一基线 [`BENCHMARK_REPORT.md`](BENCHMARK_REPORT.md)（2026-06-01，GitHash `1c98a49c`）逐项对比。
> 对比方法：相同机器、相同命令（见 §8），按「基准方法名」逐行比对 Mean 与 Allocated。
> 注意：本报告使用 BenchmarkDotNet `ShortRun`（仅 3 次迭代），**耗时**噪声较大（部分用例 Error ≥ Mean）；
> 但 **Allocated 是确定性测量**（由代码路径决定，不受噪声影响），本报告的回归判定主要以分配量为准、耗时为辅。

---

## ⚠️ 结论摘要（TL;DR）

**当前版本（`8a2dea8`）相对上一基线（`1c98a49c`）存在严重的样式/布局回归：**

| 维度 | 变化 | 幅度 |
|------|------|------|
| 样式解析+布局（每次布局全量重算） | 变慢 / 分配暴增 | **耗时 2.2×–21.6×，分配 3×–8.3×** |
| 每帧管线（含样式+布局） | 变慢 | 真实页面 **0.48 → 1.93 ms（4.0×）**；大页面 **2.33 → 27.0 ms（11.6×）** |
| 60fps 预算 | 大页面**超出预算** | 大页面全量帧 27.0 ms = 预算的 **163%**（基线时仅 14%） |
| 纯渲染（不含样式/布局） | 基本持平，局部改善 | 全量渲染 ±35%（噪声级）；20 脏区增量渲染 **↓36%** |
| 工具类（脏区域合并 / 树遍历） | 无变化 | ≈ |

**根因（已用分配探针定位）**：一次布局中 **98.6% 的堆分配来自 `StyleResolver.Resolve`**——
每个节点每次布局新建 1 个 `Style`（实测 **12,688 B**）+ 1 个 `ComputedStyle`（实测 **~14,928 B**），
每节点合计 **~28 KB**（基线时 ~6.7 KB）。再叠加 ISSUE-086 的 TextNode 模型使**节点数翻倍**
（1001 元素 → 2001 节点），1000 子元素树单次布局分配从 6.7 MB 暴涨到 **55.4 MB**，
每次布局触发 ~1 次 Gen2 GC。详见 §5。

---

## 1. 测试元数据（与基线环境差异）

| 项 | 本报告 | 上一基线 | 可比性 |
|----|--------|---------|--------|
| 报告生成日期 | 2026-07-21 | 2026-06-01 | — |
| CPU | AMD Ryzen 7 8700G（16 逻辑核 / 8 物理核） | 同左 | ✅ 相同 |
| 操作系统 | Windows 11 (10.0.26200.**8875**) | 10.0.26200.8457 | ✅ 小版本更新 |
| .NET SDK | 10.0.**302** | 10.0.300 | ✅ 补丁级 |
| 运行时 | .NET 10.0.**10**, X64 RyuJIT AVX-512 | .NET 10.0.8 | ✅ 补丁级 |
| BenchmarkDotNet | v0.14.0 | v0.14.0 | ✅ 相同 |
| Job | ShortRun（IterationCount=3, LaunchCount=1, WarmupCount=3） | 同左 | ✅ 相同 |
| GitHash | `8a2dea8` | `1c98a49c` | 相隔 44 个提交 |
| 基准代码 | 与基线一致（期间仅删除 `TableBenchmarks.cs`，其余未动） | — | ✅ 逐方法可比 |

> 运行时补丁（10.0.8→10.0.10）无法改变托管分配模式；下文分配量的 3–8 倍增长全部来自代码变化。

基线以来的主要相关变更（44 个提交中筛选）：
- `42bcd72` **ISSUE-080 样式系统重写**：引入 `StyleProperty<T>` 联合结构（值/var()/关键词/calc 四选一）+ 源生成器属性赋值，`Style`/`ComputedStyle` 对象体积膨胀
- `e2fd891` **ISSUE-086 TextNode 模型**：文本成为一等子节点，含文本树的节点数翻倍
- FlexLayout 重写（+1163 行）、BlockLayout（+422）、LayoutEngine（+290，含 absolute 定位后处理）、新增 TableLayout/TextLayout/TextWrapper
- `015c108` Table 布局、`64f81a2` vw/vh 单位、`912e915` 新样式属性（outline/visibility/user-select 等，ComputedStyle 属性 71 → 89 个）

---

## 2. 每帧管线（Full Frame Pipeline）对比

完整帧 = 样式解析 + 布局 + 渲染。60fps 预算 16.6 ms。

| 基准方法 | 基线 Mean | 本次 Mean | 耗时比 | 基线分配 | 本次分配 | 分配比 | 预算占比（基线→本次） |
|---------|----------|----------|-------|---------|---------|-------|---------------------|
| `FullFrame_RealisticPage`（~90 元素） | 0.48 ms | **1.93 ms** | **4.0×** ⚠️ | 838 KB | 5.11 MB | 6.2× | 2.9% → **11.6%** |
| `FullFrame_LargePage`（500 元素） | 2.33 ms | **26.99 ms** | **11.6×** 🔴 | 4.03 MB | 27.9 MB | 6.9× | 14% → **163%（超预算）** |
| `IncrementalFrame_RealisticPage_SingleDirty` | 0.23 ms | **1.70 ms** | **7.4×** ⚠️ | 701 KB | 4.96 MB | 7.2× | 1.4% → 10.3% |
| `IncrementalFrame_LargePage_SingleDirty` | 0.99 ms | **23.70 ms** | **23.9×** 🔴 | 3.29 MB | 27.1 MB | 8.2× | 6.0% → **143%（超预算）** |

**分析**：
1. 真实页面（~90 元素）每帧 1.93 ms，60fps 下仍充裕，但已从「极充裕」变为「占 1/8 预算」；在低端移动设备上折算后可能吃紧。
2. **大页面全量帧 27 ms 已超出 60fps 预算**（基线时 2.33 ms 仅占 14%）。
3. **增量渲染的相对优势被样式/布局成本淹没**：基线时增量帧省 52–57%；本次大页面增量帧（23.7 ms）相对全量帧（27.0 ms）仅省 12%——因为帧成本中渲染只占 ~1.8 ms，其余 ~25 ms 全是每帧重算的样式+布局。脏区域优化省的是渲染，救不了样式重算。

---

## 3. 局部重绘 vs 全量重绘 — 拐点分析

500 元素树，`DirtyRegionTippingPointBenchmarks`（直接调 `RenderEngine.RenderDirty`，不含样式/布局，故不受 §2 回归影响）：

| 脏区域数 | 基线增量 Mean / Ratio | 本次增量 Mean / Ratio | 变化 |
|---------|---------------------|---------------------|------|
| 1 | 321 μs / 0.13 | 509 μs / 0.15 | ≈ |
| 2 | 349 μs / 0.16 | 445 μs / 0.13 | ≈ |
| 3 | 375 μs / 0.18 | 436 μs / 0.16 | ≈ |
| 5 | 448 μs / 0.20 | 513 μs / 0.19 | ≈ |
| 8 | 570 μs / 0.26 | 650 μs / 0.24 | ≈ |
| 10 | 675 μs / 0.32 | 743 μs / 0.27 | ≈ |
| 15 | 1,033 μs / 0.47 | 1,453 μs / 0.52 | ≈（噪声） |
| 20 | 1,632 μs / 0.79 | 2,080 μs / **0.56** | 相对全量**变优** |
| 30 | 2,674 μs / 1.24 | 3,091 μs / **0.91** | 相对全量**变优** |
| 50 | 5,398 μs / 2.65 | 6,629 μs / 2.03 | 相对全量**变优** |

全量渲染基线：基线 ~2.0–2.4 ms / 777 KB → 本次 ~2.7–3.7 ms / ~880 KB（~1.4×，Pain­ter 功能增加的代价，噪声边缘）。

**拐点从「20–30 之间」后移到「30–50 之间」**：增量渲染路径本身有所改善（见 §4.3 `IncrementalRender_ManyDirty` ↓36%），脏区域 ≤30 时增量仍占优。
`RenderEngine.MaxIncrementalDirtyRegions = 30` 的阈值设定依然合理（30 时 Ratio 0.91 尚未越界）。

---

## 4. 各模块详细对比

### 4.1 Layout 布局引擎（`LayoutBenchmarks`）— 🔴 重灾区

| 基准方法 | 基线 Mean | 本次 Mean | 耗时比 | 基线分配 | 本次分配 | 分配比 |
|---------|----------|----------|-------|---------|---------|-------|
| `BlockLayout_SmallTree`（10 子） | 14.2 μs | 82.3 μs | **5.8×** | 74 KB | 581 KB | **7.9×** |
| `BlockLayout_LargeTree`（1000 子） | 2,193 μs | **37,353 μs** | **17.0×** 🔴 | 6.7 MB | **55.4 MB** | **8.3×** |
| `BlockLayout_DeepNesting`（50 层） | 72.2 μs | 231.7 μs | 3.2× | 344 KB | 1,438 KB | 4.2× |
| `FlexLayout_FewChildren`（10 子） | 15.1 μs | 79.2 μs | 5.2× | 75 KB | 583 KB | 7.8× |
| `FlexLayout_ManyChildren`（200 子） | 301 μs | **6,497 μs** | **21.6×** 🔴 | 1.4 MB | 11.1 MB | 8.1× |
| `InlineLayout_ManyElements`（100） | 136 μs | 1,013 μs | 7.5× | 680 KB | 5.6 MB | 8.2× |
| `MixedLayout_Realistic`（真实页面） | 143 μs | 987 μs | 6.9× | 690 KB | 5.1 MB | 7.3× |

**分析**：所有布局用例分配统一上涨 ~8 倍——这是「每节点分配 ×4 且节点数 ×2」的复合结果（见 §5）。
`BlockLayout_LargeTree` 单次操作 55.4 MB 分配、**每次操作触发 1.07 次 Gen2 GC**（基线为 0），
GC 停顿是 17× 耗时的主要构成。基线报告中「Block 大树是最重单项（2.2 ms）」的结论现在放大为 37 ms。

### 4.2 Style 样式解析（`StyleResolutionBenchmarks`）— 🔴 同为重灾区

> 注：这些用例执行的是「样式解析 + 完整布局」，与 §4.1 共享同一份回归来源。

| 基准方法 | 基线 Mean | 本次 Mean | 耗时比 | 基线分配 | 本次分配 | 分配比 |
|---------|----------|----------|-------|---------|---------|-------|
| `Resolve_FewRules`（5 规则/10 元素） | 14.1 μs | 77.6 μs | 5.5× | 74 KB | 581 KB | 7.9× |
| `Resolve_ManyRules`（100 规则/100 元素） | 1,008 μs | 3,027 μs | 3.0× | 1.1 MB | 6.0 MB | 5.3× |
| `Resolve_ComplexSelectors`（150 复杂选择器） | 2,185 μs | 5,972 μs | 2.7× | 1.6 MB | 6.5 MB | 4.1× |
| `Resolve_DeepInheritance`（30 层继承） | 312 μs | 697 μs | 2.2× | 353 KB | 1.0 MB | 2.9× |

**分析**：规则越多，选择器匹配（O(元素×规则)）在总成本中的占比越高，因此倍数随规则数增加而被「摊薄」——
但绝对增量（如 ComplexSelectors +3.8 ms / +4.9 MB）依然主要来自每节点的新建对象。
基线建议的「选择器索引/解析缓存」在未解决对象体积问题前收益有限。

### 4.3 Render 渲染引擎（`RenderBenchmarks`）— ✅ 基本稳定，局部改善

| 基准方法 | 基线 Mean | 本次 Mean | 耗时比 | 基线分配 | 本次分配 | 分配比 |
|---------|----------|----------|-------|---------|---------|-------|
| `FullRender_SmallTree`（10 元素） | 86.3 μs | 94.4 μs | 1.09× | 14 KB | 16 KB | ≈ |
| `FullRender_LargeTree`（500 元素） | 1,365 μs | 1,825 μs | 1.34× | 777 KB | 870 KB | ≈ |
| `IncrementalRender_SingleDirty` | 66.2 μs | 62.0 μs | 0.94× | 16 KB | 12 KB | ✅ 略优 |
| `IncrementalRender_ManyDirty`（20 脏区） | 1,787 μs | **1,141 μs** | **0.64×** ✅ | 867 KB | **357 KB** | **↓59%** ✅ |
| `RenderWithText` | 329 μs | 379 μs | 1.15× | 148 KB | 164 KB | ≈ |

**分析**：渲染模块（脏区域遍历、裁剪、Painter）未受回归影响；20 脏区增量渲染耗时 ↓36%、分配 ↓59%，
是本次唯一的显著改善项（与 RenderEngine 626 行重构中的遍历/裁剪优化一致）。
全量渲染 1.34× 在 ShortRun 噪声边缘（Error 463 μs），Painter 新增圆角裁剪/文本裁剪等功能可能带来小幅真实开销。

### 4.4 DirtyRegion 脏区域管理 — ✅ 无变化

| 基准方法 | 基线 | 本次 | 分配 |
|---------|------|------|------|
| `AddRegions_NonOverlapping` | 16.9 μs | 12.6 μs | 0 |
| `AddRegions_Overlapping` | 10.7 μs | 14.9 μs | 0 |
| `AddRegions_Adjacent` | 11.2 μs | 11.9 μs | 0 |

零分配保持，耗时差异为噪声（该组 Error 普遍 ≥ Mean）。

### 4.5 TreeTraversal 树遍历 — ✅ 无变化

| 基准方法 | 基线 | 本次 | 分配（基线→本次） |
|---------|------|------|-----------------|
| `PreOrder_LargeTree` | 2.58 μs | 2.66 μs | 88 B → 88 B |
| `PostOrder_LargeTree` | 1.81 μs | 1.96 μs | 88 B → 88 B |
| `LevelOrder_LargeTree` | 5.94 μs | 7.03 μs | 33 KB → 33 KB |
| `FindById_LargeTree` | 4.38 μs | 4.37 μs | 0 → 0 |
| `FindByClass_LargeTree` | 31.1 μs | 36.1 μs | 123 KB → 123 KB |

遍历工具与引擎演进无关，全部持平。基线提到的 `FindByClass` 123 KB 分配优化仍未做（低优先级）。

---

## 5. 回归根因分析（分配探针实测）

为定位 §4.1/§4.2 的分配暴涨，用 `GC.GetAllocatedBytesForCurrentThread` 对基准同款树（`CreateFlatTree`，含文本）分阶段实测（Release，预热后）：

| 测量项 | 结果 | 说明 |
|--------|------|------|
| 节点总数（1001 元素树） | **2001 节点** | ISSUE-086：每个含文本元素派生 1 个 TextNode，节点数翻倍 |
| `LayoutEngine.Layout` 全程 | 28,336 B/节点 | 即基准的一次操作 |
| 仅 `StyleResolver.Resolve`（全部节点） | **27,956 B/节点（占 98.6%）** | 布局算法+建树仅占 ~380 B/节点 |
| `new Style()` 单对象 | **12,688 B** | 基线时估算 ~2–3 KB |
| `ComputedStyle.FromStyle` 产物 | **~14,928 B** | 含源生成属性赋值 |
| `Resolve` 单次合计 | 28,040 B | = 12,688（baseStyle）+ ~14,928（ComputedStyle）+ ~400（匹配列表/排序） |
| `TextMeasurer.MeasureTextWithWrap` | 248 B/次 | 换行测量无缓存，但量级可忽略 |

**根因链**：

1. **ISSUE-080（`42bcd72`）样式表示重写**：`Style` 的每个属性从「裸可空值」（如 `Length?`，~24 B）
   变为 `StyleProperty<T>?` 联合结构（具体值 + `VarReference` + `StyleKeyword` + `CalcValue<T>` 四个字段并列，~40–64 B/槽），
   且属性数量随新功能增至 ~100 个 → **单个 `Style` 12.7 KB、`ComputedStyle` ~15 KB**。
   `Resolve` 每节点新建这两对象（baseStyle 用于级联合并，ComputedStyle 为产物）。
2. **ISSUE-086（`e2fd891`）TextNode**：文本成为一等子节点后，样式解析对 TextNode 同样执行完整的
   规则匹配 + 新建 `Style`/`ComputedStyle`（文本节点的样式实际全部来自继承，走的是重路径）→ 节点数翻倍使成本再 ×2。
3. **每次布局全量重算**：`LayoutEngine.Layout` 第 1 步对整棵树重新 `Resolve`（无样式缓存/失效机制），
   因此该成本渗透进**每一次布局、每一帧**（含增量帧）。

复合效果：每节点 6.7 KB → 28 KB（×4.2），节点数 ×2 → 单次布局分配 ×8.3，GC（含 Gen2）主导耗时 → ×17–21.6。

**为什么帧管线比纯布局更惨（11.6×/23.9× vs 17×）**：帧管线在布局之外还有 RenderEngine 的
绘制（其文本绘制路径同样经过无缓存的 `TextWrapper`，且 Painter 功能增加），叠加后进一步放大。

---

## 6. 优化建议（按预期收益排序）

1. 🔴 **`Style`/`ComputedStyle` 瘦身**（最大杠杆，预估可回收 60–75% 分配）：
   - `StyleProperty<T>` 的四路字段改为「值 + 单引用字段 + 判别字节」（var/calc/keyword 共享一个 object 引用槽，常见路径只剩 `T` + 8 B + 1 B）；
   - 或对 var/calc 采用旁路稀疏存储（绝大多数属性是普通值）。
2. 🔴 **样式计算缓存 / 失效机制**：元素状态（hover/checked 等）或样式表未变时跳过 `Resolve`，
   直接复用上次的 `ComputedStyle`——这是帧级成本的根本解法（目前每帧全树重算）。
   收益：增量帧从 23.7 ms 降回渲染-only 的 ~1–2 ms。
3. 🟡 **TextNode 走继承快速路径**：文本节点不参与选择器匹配（无 tag/class 语义），
   其样式 = 父节点可继承属性，可跳过规则匹配与 baseStyle 新建，预估省掉一半节点的 Resolve 成本。
4. 🟡 **baseStyle 复用**：`Resolve` 内的级联用 `Style` 可改为池化/复位复用（每节点省 12.7 KB）。
5. 🟢 **选择器索引**（基线遗留）：`Resolve_ManyRules` 3.0 ms 仍为 O(元素×规则)，按 tag/class/id 分桶。
6. 🟢 **`FindByClass` 共享 List**（基线遗留，123 KB）。

修复验证：按 §8 命令重跑，目标恢复到基线量级（真实页面全量帧 ≤0.5 ms / ≤1 MB；BlockLayout_LargeTree ≤2.5 ms / ≤7 MB）。

---

## 7. 与基线对比的一图流

```
模块                     基线 → 本次           判定
─────────────────────────────────────────────────────
帧管线（真实页面）        0.48 → 1.93 ms       ⚠️ 4.0×
帧管线（大页面）          2.33 → 27.0 ms       🔴 11.6×，超 60fps 预算
布局引擎                 全面 3.2×–21.6×       🔴 分配 ×8，Gen2 GC 每次布局
样式解析                 2.2×–5.5×             🔴 与布局同源
渲染引擎                 ±35%，局部 ↓36%        ✅ 稳定/改善
脏区域管理 / 树遍历       持平                  ✅ 无变化
拐点（增量 vs 全量）      20–30 → 30–50        ✅ 增量路径改善
```

---

## 8. 运行方式（与基线相同命令）

```bash
# 运行全部基准测试（生成本报告所用命令）
dotnet run -c Release --project benchmarks/Miko.Benchmarks -- --filter "*" --job short

# 仅运行帧管线和拐点测试
dotnet run -c Release --project benchmarks/Miko.Benchmarks -- --filter "*FramePipeline*" "*TippingPoint*" --job short

# 运行特定类
dotnet run -c Release --project benchmarks/Miko.Benchmarks -- --filter "*LayoutBenchmarks*"
```

原始结果（CSV / HTML / GitHub-md）由 BenchmarkDotNet 导出至 `BenchmarkDotNet.Artifacts/results/`（本次位于仓库根目录，已被 .gitignore 忽略）。
