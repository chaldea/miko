# Miko 渲染引擎基准测试报告（2026-08-26，ISSUE-130 第二轮）

本报告是在第一轮现有基准之后加入 `AnimationFrameBenchmarks`，再执行完整基准套件（共
61 个用例）的结果。对照基线为 `BENCHMARK_REPORT_2026-07-21-ISSUE-096-fix.md`；
BenchmarkDotNet 使用 `ShortRun`（1 次启动、3 次预热、3 次迭代）。当前提交：`30685dd`。

## 结论

静态帧与第一轮结果处于同一量级：当前大页面完整帧 **15.74 ms / 10.57 MB**，接近
60 fps 的 16.6 ms 预算。动画管理器本身更新很轻，但动画帧会使布局输入每帧失效并触发
完整样式解析、布局和绘制；同时运行 10/50/100 个动画元素时，动画帧分别是静态帧的
**73.5x / 173.7x / 187.8x**，100 个动画元素达到 **512.0 ms / 2.30 MB**。
这验证了 ISSUE-130 所述的动画导致渲染性能急剧下降问题。

## 动画场景

`AnimationFrameBenchmarks` 构造固定页面和无限循环的关键帧动画（Opacity + Translate），
用 `AnimatedElementCount` 参数模拟单页中的多个同时播放组件。每个 AnimatedFrame 包含
一次 60 Hz 动画推进、布局缓存失效、完整布局和 Skia 离屏绘制；`AnimationUpdateOnly`
用于隔离动画插值/样式写入本身的成本。

| 动画元素数 | 静态帧 | 动画帧 | 动画/静态 | 仅 Update |
|---:|---:|---:|---:|---:|
| 10 | 717.43 us / 238.79 KB | **52,546.69 us / 243.49 KB** | **73.5x** | 6.23 us / 3.44 KB |
| 50 | 1,543.56 us / 1,161.31 KB | **266,748.75 us / 1,183.13 KB** | **173.7x** | 30.69 us / 17.19 KB |
| 100 | 2,732.22 us / 2,310.01 KB | **512,002.40 us / 2,353.66 KB** | **187.8x** | 62.67 us / 34.38 KB |

> ShortRun 的动画帧耗时误差较大，但数量级差异远超误差；Allocated 列显示主要额外成本
> 来自布局/渲染路径，而不是动画管理器对象分配。

## 第二轮常规帧管线

| 基准方法 | ISSUE-096 优化后 | 第二轮当前分支 | 当前/基线（耗时，分配） |
|---|---:|---:|---:|
| FullFrame_RealisticPage | 0.83 ms / 1.43 MB | **1.263 ms / 2.53 MB** | 1.52x / 1.77x |
| FullFrame_LargePage | 5.36 ms / 7.57 MB | **15.737 ms / 10.57 MB** | 2.94x / 1.40x |
| IncrementalFrame_RealisticPage_SingleDirty | 0.55 ms / 1.28 MB | **0.965 ms / 2.39 MB** | 1.76x / 1.87x |
| IncrementalFrame_LargePage_SingleDirty | 3.53 ms / 6.71 MB | **11.020 ms / 9.74 MB** | 3.12x / 1.45x |

## 运行方式与原始结果

```bash
dotnet run -c Release --project benchmarks/Miko.Benchmarks -- --filter "*" --job short
```

动画基准源码：`benchmarks/Miko.Benchmarks/Benchmarks/AnimationFrameBenchmarks.cs`。
BenchmarkDotNet 原始 CSV、HTML 和 GitHub Markdown 位于仓库根目录的
`BenchmarkDotNet.Artifacts/results/`（已被 `.gitignore` 忽略）。
