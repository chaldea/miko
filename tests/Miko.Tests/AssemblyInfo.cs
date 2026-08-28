// 测试集合并行化：**已启用**（ISSUE-129）。
//
// 此前本程序集显式关闭并行化，原因是变更版本号曾是 Element 上的进程级全局静态计数：引擎
// 交互测试（Platform/*、Core/*）会断言「无失效、无待呈现工作」（HasPendingVisualWork == false、
// 版本号不变），而其他测试类在并行集合中构造/修改元素就会并发递增那个全局计数，使这些断言
// 随机失败（ISSUE-104 问题1 的回归测试暴露了该隐患）。
//
// ISSUE-129 把版本号改为按引擎实例计（MikoEngine.Mutations），元素经 Element.Owner 归属到
// 自己的引擎，游离元素则完全不记账。各测试的引擎与 DOM 因此天然互不可见，上述断言重新
// 具备封闭性，并行化的阻碍随之消失。
//
// 仍然共享的进程级状态（有意为之，见 Hosting/EngineExtensions.cs 与 AGENT.md 的说明）：
// FontManager.Instance 的字体注册表与字形缓存、TextMeasurer 的度量缓存。它们都是**内容寻址**
// 的（键含字体族/字号/文本），因此并发的**只读**命中是安全的——最多重复计算，不会算错。
//
// 但写全局字体状态的测试确实存在，且不能与其他文本测量并发运行：FontManager.ResetInstance()
// 会释放当前字体对象（并发测量可能读到已释放的 typeface），RegisterFont/注销会改变全局度量
// 结果。这些测试类统一归入 GlobalFontStateCollection（DisableParallelization），彼此串行。
// 新增测试若调用这些 API，请一并加上 [Collection(GlobalFontStateCollection.Name)]。
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = false)]
