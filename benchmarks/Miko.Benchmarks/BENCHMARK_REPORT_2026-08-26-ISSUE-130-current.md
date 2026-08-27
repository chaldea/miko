# Miko 渲染引擎基准测试报告（2026-08-26，ISSUE-130 第一轮）

本报告记录当前分支在加入动画场景前的现有基准结果，并与
`BENCHMARK_REPORT_2026-07-21-ISSUE-096-fix.md` 对比。两次均使用 BenchmarkDotNet
`ShortRun`（1 次启动、3 次预热、3 次迭代）。当前环境为 Windows 11、AMD Ryzen 7 8700G、
.NET 10.0.11、BenchmarkDotNet 0.14.0。当前提交：`30685dd`。

## 结论

当前分支的完整管线仍明显优于 ISSUE-096 优化前的回归版本，但相较 ISSUE-096 优化后报告，
本次测量耗时和分配有所上升：大页面全帧为 **17.76 ms / 10.55 MB**，仍在 16.6 ms 的
60 fps 预算边缘（ShortRun 噪声较大）。布局与样式用例保持在优化后量级；渲染用例整体稳定。

## 逐项对比

### 每帧管线

| 基准方法 | ISSUE-096 优化后 | 当前分支 | 当前/基线（耗时，分配） |
|---|---:|---:|---:|
| FullFrame_RealisticPage | 0.83 ms / 1.43 MB | **1.394 ms / 2.53 MB** | 1.68x / 1.77x |
| FullFrame_LargePage | 5.36 ms / 7.57 MB | **17.764 ms / 10.55 MB** | 3.31x / 1.39x |
| IncrementalFrame_RealisticPage_SingleDirty | 0.55 ms / 1.28 MB | **1.128 ms / 2.39 MB** | 2.05x / 1.87x |
| IncrementalFrame_LargePage_SingleDirty | 3.53 ms / 6.71 MB | **11.922 ms / 9.74 MB** | 3.38x / 1.45x |

### 布局与样式

| 基准方法 | 当前 Mean | 当前 Allocated |
|---|---:|---:|
| BlockLayout_SmallTree | 63.54 us | 208.41 KB |
| BlockLayout_LargeTree | 31,414.25 us | 19,915.48 KB |
| BlockLayout_DeepNesting | 181.68 us | 489.98 KB |
| FlexLayout_FewChildren | 69.50 us | 210.02 KB |
| FlexLayout_ManyChildren | 1,610.82 us | 4,012.65 KB |
| InlineLayout_ManyElements | 712.29 us | 1,996.55 KB |
| MixedLayout_Realistic | 867.24 us | 2,431.72 KB |
| Resolve_FewRules | 65.31 us | 208.41 KB |
| Resolve_ManyRules | 2,358.32 us | 1,999.69 KB |
| Resolve_ComplexSelectors | 4,644.73 us | 1,999.69 KB |
| Resolve_DeepInheritance | 587.26 us | 301.85 KB |

### 渲染

| 基准方法 | 当前 Mean | 当前 Allocated |
|---|---:|---:|
| FullRender_SmallTree | 95.81 us | 15.56 KB |
| FullRender_LargeTree | 1,633.21 us | 862.63 KB |
| IncrementalRender_SingleDirty | 161.56 us | 12.43 KB |
| IncrementalRender_ManyDirty | 1,403.75 us | 392.33 KB |
| RenderWithText | 516.83 us | 161.23 KB |

## 运行与原始结果

```bash
dotnet run -c Release --project benchmarks/Miko.Benchmarks -- --filter "*" --job short
```

原始 CSV、HTML 和 GitHub Markdown 位于仓库根目录的
`BenchmarkDotNet.Artifacts/results/`（该目录已被 `.gitignore` 忽略）。
