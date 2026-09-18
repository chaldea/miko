using Microsoft.Extensions.DependencyInjection;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Ionic;
using Miko.Ionic.Components;
using Miko.Styling.Selectors;

namespace Miko.Ionic.AotSmoke;

/// <summary>
/// ISSUE-140 的裁剪后回归闸门。
///
/// <para>组件参数（<c>[Inject]</c> / <c>[CascadingParameter]</c>）由
/// <c>ComponentParameterCache</c> 在运行时反射发现。裁剪器看不见那层查找——要让它保留这些属性，
/// 必须在每个组件 <c>Type</c> 的<b>来源</b>（<c>RenderTreeBuilder.OpenComponent&lt;T&gt;</c>、
/// <c>Router.MapRoute&lt;T&gt;</c> 等）标注 <c>DynamicallyAccessedMembers</c>。</para>
///
/// <para>这件事普通单元测试测不出来：它们跑在<b>未裁剪</b>的程序集上，属性永远在。ISSUE-140
/// 报告的三个症状正是在 1900+ 条绿色用例的情况下发生的。所以本程序断言的是<b>裁剪后</b>的行为，
/// 必须 <c>PublishAot</c> 发布后运行才有意义。</para>
///
/// <para>走的是生产路径：<c>MikoAppBuilder</c> → <c>UseGeneratedRoutes</c> →
/// <c>Controller.BuildRoot()</c>，与桌面宿主构建首帧 DOM 的方式完全一致。</para>
/// </summary>
public static class Program
{
    private static int _failures;

    public static int Main()
    {
        Console.WriteLine("Miko.Ionic AOT smoke (ISSUE-140)");
        Console.WriteLine(new string('-', 56));

        var app = BuildApp();
        var root = app.Controller.BuildRoot();

        InjectedParameterReachesComponent(root);
        InheritedProtectedInjectReachesDerivedComponent(root);
        CascadedAccordionContextArrives(root);
        CascadedItemContextArrives(root);
        DynamicModalComponentKeepsItsParameters(app);
        AttributeSelectorStillMatches();
        GlobalStyleMergeProducesRules();

        Console.WriteLine(new string('-', 56));
        Console.WriteLine(_failures == 0
            ? "PASS — all checks succeeded"
            : $"FAIL — {_failures} check(s) failed");
        return _failures == 0 ? 0 : 1;
    }

    /// <summary>用真实的应用构建路径装配应用（与桌面宿主一致）。</summary>
    private static MikoAppContext BuildApp()
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.UseTitle("AotSmoke");
        builder.AddIonic();
        builder.AddResourceAssembly(typeof(Program).Assembly);
        builder.UseGeneratedRoutes();

        return builder.Build();
    }

    /// <summary>
    /// 症状 1：主页列表里的 <c>component-icon.svg</c> 只剩一个蓝底圆。
    ///
    /// <para><c>IonIcon</c> 用 <c>[Inject] protected IResourceAssemblyProvider?</c> 解析
    /// <c>res://</c> 图标源。属性被裁掉后注入落空，<c>IconResolver</c> 遍历空的程序集列表、
    /// 返回 null，于是只剩 CSS 背景色——正是「蓝底圆」。图标解析成功的可观测证据就是
    /// 元素内联样式上挂到了 BackgroundImage。</para>
    /// </summary>
    private static void InjectedParameterReachesComponent(Element root)
    {
        var icon = FindByClass(root, "smoke-icon");

        Check("IonIcon resolved its res:// source via [Inject] IResourceAssemblyProvider",
            icon?.Style?.BackgroundImage is not null,
            icon is null ? "smoke-icon element not found" : "BackgroundImage is null — provider was not injected");
    }

    /// <summary>
    /// 基类上的非公开注入点。<c>IonicComponentBase</c> 把 <c>PlatformInfo</c> 声明为
    /// <c>protected</c>，几十个组件继承它——而 <c>NonPublicProperties</c> 只保留「自身声明的」
    /// 非公开属性、不上溯基类，必须靠 <c>NonPublicPropertiesWithInherited</c> 才保得住。
    /// 它决定 md/ios 模式：裁掉后每个组件的根 class 都会缺少模式前缀。
    /// </summary>
    private static void InheritedProtectedInjectReachesDerivedComponent(Element root)
    {
        var icon = FindByClass(root, "smoke-icon");
        var classes = icon?.Class?.Split(' ') ?? [];

        Check("IonicComponentBase [Inject] PlatformInfo reached a derived component (mode class present)",
            classes.Contains("md") || classes.Contains("ios"),
            $"class = '{icon?.Class ?? "<not found>"}'");
    }

    /// <summary>
    /// 症状 2：Accordion 点击展开无效果。展开态来自 group 级联下来的
    /// <c>IonAccordionGroupContext</c>；级联断了就恒为 collapsed。
    /// </summary>
    private static void CascadedAccordionContextArrives(Element root)
    {
        var accordion = FindByClass(root, "ion-accordion");

        Check("IonAccordion received the cascaded IonAccordionGroupContext",
            accordion?.Class?.Split(' ').Contains("accordion-expanded") == true,
            $"class = '{accordion?.Class ?? "<not found>"}'");
    }

    /// <summary>
    /// 症状 3：依赖 <c>IonItemContext</c> 等上下文的样式全部失效。
    /// <c>IonCheckbox</c> 在 item 内应加上 <c>in-item</c> 标记类。
    /// </summary>
    private static void CascadedItemContextArrives(Element root)
    {
        var checkbox = FindByClass(root, "smoke-checkbox");

        Check("IonCheckbox received the cascaded IonItemContext",
            checkbox?.Class?.Split(' ').Contains("in-item") == true,
            $"class = '{checkbox?.Class ?? "<not found>"}'");
    }

    /// <summary>
    /// 代码审查 P1：动态 modal 的内容组件。
    ///
    /// <para><c>IonModalController.CreateAsync&lt;TComponent&gt;</c> 把类型装进
    /// <c>IonModalOptions.Component</c>，再由 <c>RenderTreeBuilder.OpenComponent(int, Type)</c>
    /// 动态创建、<c>AddComponentParameter</c> 按<b>名字</b>反射写参数。这条链上任一环没有标注，
    /// 裁剪后传入的参数就会静默保持默认值——而首页那几个静态组件照样正常，所以光靠它们测不出来。</para>
    /// </summary>
    private static void DynamicModalComponentKeepsItsParameters(MikoAppContext app)
    {
        var registry = app.Services.GetRequiredService<IonOverlayRegistry>();
        var controller = new IonModalController(registry);

        var reference = controller.CreateAsync<ModalContent>(
            new Dictionary<string, object?> { [nameof(ModalContent.Text)] = "from-controller" })
            .GetAwaiter().GetResult();
        reference.PresentAsync().GetAwaiter().GetResult();

        // 覆盖层在 present 之后才进入 DOM，需要重新构建一次树。
        var root = app.Controller.BuildRoot();
        var content = FindByClass(root, "modal-content");

        Check("Dynamically presented modal component received its [Parameter] by name",
            content?.TextContent?.Contains("from-controller") == true,
            content is null
                ? "modal-content element not found — the modal component was not created"
                : $"TextContent = '{content.TextContent}' — parameter was trimmed away");
    }

    /// <summary>
    /// CSS 属性选择器。原实现走 <c>GetType().GetProperty(name, IgnoreCase)</c>，裁剪后恒失配
    /// ——规则还在样式表里却永不命中。现由源生成器产出的 <c>TryGetAttributeValue</c> 分派。
    /// </summary>
    private static void AttributeSelectorStillMatches()
    {
        var input = new InputElement { Type = InputType.Checkbox };

        Check("AttributeSelector [type=checkbox] matches after trimming",
            new AttributeSelector("type", AttributeMatchOperator.Equals, "checkbox").Matches(input));

        Check("AttributeSelector rejects an unknown attribute",
            !new AttributeSelector("nonexistent", AttributeMatchOperator.Exists).Matches(input));
    }

    /// <summary>
    /// <c>GlobalStyle</c> 曾用跨程序集非公开反射读取 <c>CssObject.Children</c> 合并样式片段：
    /// 裁剪后返回 null 会悄悄丢掉整张全局样式表（页面能渲染，只是完全没样式）。
    /// </summary>
    private static void GlobalStyleMergeProducesRules()
    {
        var sheet = new Styling.StyleSheet();
        sheet.Add(Styles.GlobalStyle.GenStyle());

        Check($"GlobalStyle merged its nested rule sets (rules={sheet.Rules.Count})", sheet.Rules.Count > 0);
    }

    // -----------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------

    private static Element? FindByClass(Element root, string className)
    {
        if (root.Class?.Split(' ').Contains(className) == true)
            return root;

        foreach (var child in root.Children)
        {
            var found = FindByClass(child, className);
            if (found != null) return found;
        }
        return null;
    }

    private static void Check(string description, bool condition, string? detail = null)
    {
        if (condition)
        {
            Console.WriteLine($"  ok    {description}");
            return;
        }

        _failures++;
        Console.WriteLine($"  FAIL  {description}");
        if (detail != null)
            Console.WriteLine($"          {detail}");
    }
}
