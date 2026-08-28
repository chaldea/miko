namespace Miko.Tests;

/// <summary>
/// 把所有会改动<b>进程级</b>字体/文本度量状态的测试收进同一个 xUnit collection，使它们
/// 相互串行（ISSUE-129）。
///
/// <para>ISSUE-129 让变更版本号变成按引擎实例，于是 <c>Miko.Tests</c> 整体重新开启了并行化。
/// 但字体注册表与文本度量缓存<b>仍然是进程共享的</b>（有意为之：内容寻址，共享既正确又划算）。
/// 下面这些操作会改动或销毁那份共享状态：</para>
/// <list type="bullet">
/// <item><c>FontManager.ResetInstance()</c>——释放当前字体对象；若此刻另一个线程正在用它测量，
///   读到的就是已释放的 typeface；</item>
/// <item><c>RegisterFont</c> / 注销——改变全局文本度量结果，令并发测试的期望值失效；</item>
/// <item><c>TextMeasurer.ClearCache()</c>——清空共享度量缓存。</item>
/// </list>
///
/// <para>并发命中<b>只读</b>的度量缓存是安全的（内容寻址，最多重复计算），因此无需把所有做
/// 文本测量的测试都拉进来串行；真正危险的是上面这些<b>写全局</b>的操作。将它们归入同一
/// collection 后，xUnit 保证同一 collection 内不并行，写与写之间不再交叠。</para>
///
/// <para>新增测试若调用了上述任一 API，请给测试类加上
/// <c>[Collection(GlobalFontStateCollection.Name)]</c>。</para>
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class GlobalFontStateCollection
{
    public const string Name = "GlobalFontState";
}
