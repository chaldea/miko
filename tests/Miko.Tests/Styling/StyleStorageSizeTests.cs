using System.Runtime.CompilerServices;
using Miko.Common;
using Miko.Styling;
using Shouldly;

namespace Miko.Tests.Styling;

/// <summary>
/// 样式存储的体积预算（ISSUE-142）。
///
/// <para><see cref="Length"/> 是 <see cref="Style"/> 里出现最多的值类型（58 个槽），它的体积
/// 经 <c>StyleProperty&lt;Length&gt;?</c> 两级放大后直接决定每个元素每帧新建的
/// <see cref="ComputedStyle"/> 有多大——ISSUE-132 记录过那条路径上的 G2 暴涨。</para>
///
/// <para>这几个断言锁的是<b>字段布局</b>而非功能：往 <see cref="Length"/> 加一个字段、
/// 或仅仅改动字段顺序引入 8 字节对齐填充，都会让体积悄悄涨回去而没有任何测试变红
/// （ISSUE-142 的方案 A 比 D′ 差 8 B，正是纯填充所致）。</para>
/// </summary>
public class StyleStorageSizeTests
{
    [Fact]
    public void Length_ShouldStayAtSixteenBytes()
    {
        // float _value + uint _packed + object? _mix = 4 + 4 + 8，恰好无填充。
        Unsafe.SizeOf<Length>().ShouldBe(16);
    }

    [Fact]
    public void NullableStylePropertyOfLength_ShouldStayAtFortyBytes()
    {
        // Length(16) + object? _aux(8) + StyleKeyword(4) + Slot(1) + Nullable 的 hasValue(1)
        // → 按 8 字节对齐 = 40。Style 里有 58 个这样的槽。
        Unsafe.SizeOf<StyleProperty<Length>?>().ShouldBe(40);
    }

    /// <summary>
    /// 探针：<see cref="ComputedStyle"/> 单实例的托管堆占用。
    /// 分配若干实例后按分配字节差均摊，避免单次测量被其它分配噪声污染。
    /// </summary>
    [Fact]
    public void ComputedStyle_InstanceSize_ShouldStayWithinBudget()
    {
        // 预热：首次触发静态构造、模板实例等一次性分配。
        _ = new ComputedStyle();

        const int count = 64;
        var keep = new ComputedStyle[count];
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < count; i++) keep[i] = new ComputedStyle();
        long after = GC.GetAllocatedBytesForCurrentThread();

        long perInstance = (after - before) / count;
        keep[0].ShouldNotBeNull();   // 防止 keep 被优化掉

        // ISSUE-142 之前实测 9,368 B，之后 6,024 B（−36%）。
        //
        // 比 issue 预估的 7,456 B 更好：那个数字只算了 58 个直接的 Length 槽，而
        // Padding / Margin / BorderRadius / BackgroundSize / TransformOrigin 内嵌
        // Length，也随之一起瘦身。
        perInstance.ShouldBeLessThanOrEqualTo(6_200);
    }
}
