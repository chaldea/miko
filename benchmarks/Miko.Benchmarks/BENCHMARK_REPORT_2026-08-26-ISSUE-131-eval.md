# ISSUE-131 优化效果评估（2026-08-26）

对工作区当前未提交的 ISSUE-130/131 优化实现做基准验证。

## 测试环境

- Windows 11 (10.0.26200)，AMD Ryzen 7 8700G，16 逻辑核
- .NET 10.0.11，BenchmarkDotNet 0.14.0，Job=`ShortRun`（1 启动 / 3 预热 / 3 迭代）
- 基线提交 `30685dd` + 工作区未提交改动
- 测试通过情况：`Miko.Tests` 1778 passed，`Miko.Ionic.Tests` 1354 passed，0 failed

## 结论摘要

1. **paint-only 路径是有效的**，但原有基准无法观测到它，必须换用走 `MikoEngine` 的基准。
2. **ISSUE-130 对 512 ms 的归因是错的**。真正原因不是布局失效，而是 `Painter.SaveLayerAlpha`
   调用无 bounds 的 `SKCanvas.SaveLayer`，每个 `opacity < 1` 元素每帧分配一块整个裁剪区大小
   （800×5000）的离屏缓冲。这是 native 内存，不出现在 BenchmarkDotNet 的 `Allocated` 列。
3. **按需帧调度效果显著**：静态页空闲探测 11 ns / 0 B。

## 一、原有基准为何测不出优化

`AnimationFrameBenchmarks` 的两个对照臂都调用 `_layoutEngine.InvalidateCache()`，并直接驱动
`LayoutEngine`/`RenderEngine` 而不经过 `MikoEngine`：

- 布局缓存被强制失效 → paint-only 跳过布局的逻辑不可能生效；
- 不经过 `MikoEngine` → `RenderEngine.AnimatedStyles` 从未被赋值，overlay 完全不参与。

因此两臂的布局成本恒等，差值一开始就不可能来自布局。带上本次优化重跑该基准，
100 元素仍是 **526.5 ms**（原报告 512.0 ms），即该基准对本次改动完全不敏感。

## 二、走引擎的动画基准（新增 `EngineAnimationFrameBenchmarks`）

经 `MikoEngine.Tick`（宿主真实入口），由引擎自行决定是否需要布局：

| 动画元素数 | PaintOnly=true（transform+opacity） | PaintOnly=false（margin-left） |
|---:|---:|---:|
| 10 | 44,648 μs / 28.52 KB | 156.0 μs / 249.36 KB |
| 50 | 222,332 μs / 144.66 KB | 857.3 μs / 1,210.12 KB |
| 100 | 438,971 μs / 290.13 KB | 2,198.6 μs / 2,414.95 KB |

分配量方向完全符合预期：paint-only 臂**少分配约 8.7 倍**，证明布局重算确实被跳过了。
但耗时反而高 200 倍，且分配接近于零——「耗时爆炸 + 托管分配为零」不可能是 GC 或布局，
指向 native 侧。

## 三、按属性拆分定位（新增 `AnimationPropertyCostBenchmarks`）

固定 50 个动画元素，只改被动画的属性：

| 被动画属性 | 类别 | 耗时 | 分配 |
|---|---|---:|---:|
| `Transform` | paint-only | **407.5 μs** | 140.10 KB |
| `BackgroundColor` | paint-only | **367.7 μs** | 129.16 KB |
| `MarginLeft` | layout | 803.4 μs | 1,210.12 KB |
| `Opacity` | paint-only | **221,018.8 μs** | 133.63 KB |

这是本次评估的关键结果：

- `Transform`（407 μs）和 `BackgroundColor`（368 μs）都**优于** layout 动画 `MarginLeft`
  （803 μs），且分配少约 9 倍。**paint-only 优化确实带来了约 2 倍耗时、9 倍分配的改善。**
- 221 ms 的爆炸**只来自 `Opacity` 一个属性**，与分层失效模型无关。

## 四、根因：无 bounds 的 SaveLayer（新增 `SaveLayerBoundsBenchmarks`）

[Painter.cs:909](../../src/Miko/Rendering/Painter.cs#L909)：

```csharp
public int SaveLayerAlpha(byte alpha) => _canvas.SaveLayer(new SKPaint { ... });
```

不传 bounds 时 Skia 按当前裁剪区（此处即整个 800×5000 表面）分配离屏层。纯 Skia 对照，
50 次 save/draw/restore：

| 方式 | 耗时 | 相对 |
|---|---:|---:|
| `SaveLayer(paint)`（现状） | 225,768.0 μs | 1.00 |
| `SaveLayer(bounds, paint)` | **565.5 μs** | **0.003** |

**399 倍差距**，与 §3 中 221 ms 的 Opacity 结果吻合。这就是真凶。

修复方向：`SaveLayerAlpha` 接收元素 bounds 并透传给 Skia（调用点在
[RenderEngine.cs:249](../../src/Miko/Rendering/RenderEngine.cs#L249) 和
[RenderEngine.cs:101](../../src/Miko/Rendering/RenderEngine.cs#L101)，
`box.BoxModel.BorderBox` 即可，需按 box-shadow/outline 外扩）。顺带每帧 `new SKPaint`
未 Dispose，属于既有 per-frame native 泄漏。

## 五、按需帧调度（新增 `EngineIdleFrameBenchmarks`）

静态页面（无动画、无脏区）：

| 元素数 | `HasPendingVisualWork` 探测 | 空闲 `Update` |
|---:|---:|---:|
| 200 | **10.99 ns / 0 B** | 1,568 ns / 112 B |
| 1000 | **11.29 ns / 0 B** | 7,615 ns / 112 B |

探测成本与页面规模无关且零分配，Android `RenderMode.WhenDirty` + iOS `HasPendingWork` 判据
可直接消除静态页的全部无效帧。`EnsureOwnersAssigned` / `ScanAnimationsIfNeeded` 的版本号门控
生效：空闲 `Update` 已无整树遍历，仅 112 B 常量分配。

## 六、修复与验证

按 §4 的结论修复 `Painter.SaveLayerAlpha`：

- 新增带 bounds 的重载，逐元素 opacity 一律传入合成层包围盒
  （[RenderEngine.cs:258](../../src/Miko/Rendering/RenderEngine.cs#L258)）；
  转场整层淡入淡出保留无 bounds 重载（层本就是整个画布，按裁剪区分配即正确尺寸）。
- 新增 `RenderEngine.ComputeLayerBounds`：并集本盒及整棵子树的边框盒/逐行片段，外扩
  box-shadow（offset ± spread + blur）与 outline（width + offset），在裁剪盒
  （`overflow != visible`）处停止下探，跳过 `display:none` 与 fixed 后代，最后按
  `BuildTransformMatrix` 映射四角取变换后的轴对齐上界，并留 1px 抗锯齿余量。
  opacity 建立层叠上下文，故带 z-index 的后代也在层内，同样计入。
- `SKPaint` 由 `RenderEngine` 持有复用（`_layerPaint`）。原先每元素每帧 `new SKPaint`
  且不 Dispose 是既有 native 泄漏；`SetCanvas` 每帧调用，故不能挂在 `Painter` 上。

### 修复效果（按属性，50 元素）

| 被动画属性 | 修复前 | 修复后 | 提升 |
|---|---:|---:|---:|
| `Opacity` | 221,018.8 μs | **986.4 μs** | **224x** |
| `Transform` | 407.5 μs | 416.9 μs | 持平 |
| `BackgroundColor` | 367.7 μs | 395.8 μs | 持平 |
| `MarginLeft` | 803.4 μs | 904.3 μs | 持平 |

### 修复效果（走引擎的完整动画帧）

| 动画元素数 | PaintOnly 修复前 | PaintOnly 修复后 | 提升 | 分配（paint-only / layout） |
|---:|---:|---:|---:|---|
| 10 | 44,648.3 μs | **193.1 μs** | **231x** | 28.38 KB / 249.36 KB |
| 50 | 222,332.5 μs | **983.4 μs** | **226x** | 144.01 KB / 1,210.12 KB |
| 100 | 438,971.5 μs | **2,318.8 μs** | **189x** | 288.49 KB / 2,414.95 KB |

100 个同时动画的元素现在是 **2.32 ms / 288 KB**，落在 60 fps 的 16.6 ms 预算内，
且分配量比 layout 动画低 **8.4 倍**——paint-only 与按需调度的收益至此才真正显现。

原有 `AnimationFrameBenchmarks`（即使结构上测不到 overlay）也一并受益，因为它同样绘制
半透明元素：100 元素动画帧 526,547 μs → **3,789.9 μs（139x）**，已与静态帧持平
（ratio 0.76~1.09），动画帧不再是静态帧的 187 倍。

### 回归验证

- `Miko.Tests` **1783 passed**（含新增 5 个用例）、`Miko.Ionic.Tests` **1354 passed**，0 failed。
- 新增 [OpacityLayerBoundsTests.cs](../../tests/Miko.Tests/Rendering/OpacityLayerBoundsTests.cs)
  覆盖 bounds 裁剪风险：溢出子元素、box-shadow、变换后几何、alpha 仍生效、
  `overflow:hidden` 仍裁剪。**已反向验证**：把 bounds 改回 border box（故意过小）后
  5 个用例中 3 个变红，确认这些断言真的能抓住裁剪缺陷。

## 七、结论与建议

已实现部分的评价：

- P0 分层失效模型 + animated-value overlay：**有效**，transform/color 动画约 2 倍提速、
  9 倍降分配；`ComputedStyle` 每次布局新建、`Prepare()` 按引用变化重捕基值，设计安全。
- P1 减少每帧整树扫描：**有效**，空闲帧零遍历。
- P0 移动端按需帧调度：**有效**，11 ns 零分配探测。

本次已修复（§6）：

- **`SaveLayerAlpha` 缺 bounds**——曾是 opacity 动画的唯一瓶颈，修复后 224 倍提速。
  这解释了 ISSUE-131 中「Android 上动画几乎无法播放」「Segment 切换动画卡住」的现象：
  凡涉及 `opacity < 1` 的动画（IonLoading、IonToast、overlay 淡入淡出、Segment indicator）
  此前每帧每元素都在分配整屏离屏缓冲。
- 顺带消除逐元素每帧 `new SKPaint` 的 native 泄漏。

后续建议：

- ISSUE-131 中「纯 transform/opacity/color 动画帧不触发完整布局」这一验收标准已达成，
  可勾选；但 P0「分层失效模型」的实际收益是 **2 倍耗时 / 9 倍分配**，而非报告曾暗示的
  百倍级——百倍那部分来自本次的 SaveLayer 修复，二者应分开记账。
- 剩余项按原顺序推进即可。建议优先 P1「组件状态变更局部化」：`ComponentFrameBenchmarks`
  显示 300 组件的 `StateHasChanged` 为 14.2 ms / 9.0 MB，是目前最接近帧预算上限的路径。
- 大页面静态帧相对 ISSUE-096 基线（5.36 ms / 7.57 MB）仍有回退，本次未涉及，需单独定位。
  修复后常规管线无回退，且略有改善：

  | 基准方法 | ISSUE-130 第二轮 | 修复后 |
  |---|---:|---:|
  | `FullFrame_RealisticPage` | 1.263 ms / 2.53 MB | 1.378 ms / 2.53 MB |
  | `FullFrame_LargePage` | 15.737 ms / 10.57 MB | **12.939 ms / 10.53 MB** |
  | `IncrementalFrame_RealisticPage_SingleDirty` | 0.965 ms / 2.39 MB | 0.926 ms / 2.39 MB |
  | `IncrementalFrame_LargePage_SingleDirty` | 11.020 ms / 9.74 MB | **10.459 ms / 9.74 MB** |

基准可复现命令：

```bash
dotnet run -c Release --project benchmarks/Miko.Benchmarks -- \
  --filter "*Engine*Frame*|*AnimationPropertyCost*|*SaveLayerBounds*" --job short
```
