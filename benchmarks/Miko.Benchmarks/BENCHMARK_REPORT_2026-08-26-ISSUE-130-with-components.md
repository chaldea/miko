# Miko 渲染引擎基准测试报告（2026-08-26，ISSUE-130 第三轮）

本轮在现有引擎和动画基准之上，增加了组件驱动的布局场景，并重新运行完整基准套件。
本次共执行 **70 个用例**：原有 52 个、动画 9 个、组件 9 个。组件场景通过
`ComponentBase.Build()`、`RenderTreeBuilder.OpenComponent<T>()` 和 `StateHasChanged()`
生成及更新树，再交给 `LayoutEngine` 和 `RenderEngine`，不是手工拼接 DOM。

对照基线为 `BENCHMARK_REPORT_2026-07-21-ISSUE-096-fix.md`。BenchmarkDotNet 使用
`ShortRun`（1 次启动、3 次预热、3 次迭代）；环境为 Windows 11、AMD Ryzen 7 8700G、
.NET 10.0.11、BenchmarkDotNet 0.14.0。当前提交：`30685dd`。

## 结论

组件状态变更是否引起 DOM/布局输入变化，是性能差异的主要分界：

- `StateHasChanged` 会重建组件子树并使布局缓存失效，组件数 300 时为 **14.16 ms / 9.04 MB**，约为首次组件帧的 1.84 倍耗时、1.13 倍分配。
- 仅修改组件内部状态、不调用 `StateHasChanged` 时，布局输入不变，布局缓存可复用；组件数 20/100 时耗时低于首次帧（1.065/3.013 ms），组件数 300 时主要成本来自仍需进行的绘制（11.42 ms / 7.49 MB）。
- 因此并非所有状态变化都会造成同等级性能下降；真正昂贵的是状态变化触发的组件树重建和布局重算。

## 组件基准结果

| 组件数 | 首次 Build + 布局 + 绘制 | StateHasChanged + 布局 + 绘制 | 状态变更但复用布局 + 绘制 |
|---:|---:|---:|---:|
| 20 | 1.394 ms / 538.9 KB | 1.780 ms / 610.37 KB | **1.065 ms / 505.25 KB** |
| 100 | 3.508 ms / 2,656.1 KB | 6.394 ms / 3,007.08 KB | **3.013 ms / 2,491.9 KB** |
| 300 | 7.683 ms / 7,966.34 KB | **14.162 ms / 9,035.85 KB** | 11.417 ms / 7,491.36 KB |

相对每个组件数的首次帧基线，`StateHasChanged` 的分配量约增加 **13%**；状态不触发布局
重建时分配量约为首次帧的 **94%**，但仍包含完整绘制成本。ShortRun 耗时误差较大，趋势应
优先以 Allocated 和相对量级判断。

## 动画场景摘要

动画基准仍验证 ISSUE-130 的另一条路径：动画帧会主动改变布局输入并触发完整布局。

| 同时动画元素 | 静态帧 | 动画帧 | 动画/静态 |
|---:|---:|---:|---:|
| 10 | 717.43 us | 52.55 ms | 73.5x |
| 50 | 1.54 ms | 266.75 ms | 173.7x |
| 100 | 2.73 ms | 512.00 ms | 187.8x |

## 常规帧管线摘要

最终完整套件中的常规帧结果与前一轮保持同一量级：真实页面约 **1.26 ms / 2.53 MB**，
大页面约 **15.7 ms / 10.6 MB**；仍显著优于 ISSUE-096 优化前的大页面约 27 ms / 27.9 MB，
但相比 ISSUE-096 优化后的 5.36 ms / 7.57 MB 存在回退。

## 运行方式与原始结果

```bash
dotnet run -c Release --project benchmarks/Miko.Benchmarks -- --filter "*" --job short
```

组件基准源码：`benchmarks/Miko.Benchmarks/Benchmarks/ComponentFrameBenchmarks.cs`。
动画基准源码：`benchmarks/Miko.Benchmarks/Benchmarks/AnimationFrameBenchmarks.cs`。
BenchmarkDotNet 原始 CSV、HTML 和 GitHub Markdown 位于仓库根目录的
`BenchmarkDotNet.Artifacts/results/`（已被 `.gitignore` 忽略）。
