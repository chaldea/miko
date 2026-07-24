// Element.MutationVersion 是进程级全局静态计数：任何元素的结构/样式/状态写入都会递增它。
// 引擎交互测试（Platform/*）会断言"无失效、无待呈现工作"（HasPendingVisualWork == false、
// MutationVersion 不变），若其他测试类在并行集合中同时构造/修改元素，全局版本号就会被
// 并发递增，使这些断言随机失败（ISSUE-104 问题1 回归测试暴露了该隐患）。
// 因此本程序集关闭测试集合并行化，保证版本号断言的封闭性；套件仅秒级，串行代价可忽略。
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
