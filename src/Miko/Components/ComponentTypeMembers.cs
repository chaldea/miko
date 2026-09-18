using System.Diagnostics.CodeAnalysis;

namespace Miko.Components;

/// <summary>
/// 组件类型在被裁剪/AOT 编译时必须保留的成员集合（ISSUE-140）。
///
/// <para><see cref="ComponentParameterCache"/> 用
/// <c>GetProperties(Public | NonPublic | Instance)</c> 发现 <see cref="InjectAttribute"/> 与
/// <see cref="CascadingParameterAttribute"/> 属性。裁剪器看不见这层反射，除非每一个组件
/// <see cref="Type"/> 的<b>来源</b>都声明了这个需求——所以凡是组件类型进入该反射路径的入口
/// （<see cref="RenderTreeBuilder.OpenComponent{T}()"/>、<see cref="Routing.Router.MapRoute{T}"/> 等）
/// 都要标注本常量，需求才能沿调用点回流到具体组件类型上。</para>
///
/// <para>带上 <see cref="DynamicallyAccessedMemberTypes.NonPublicPropertiesWithInherited"/>
/// 是为了把需求说全：<c>NonPublicProperties</c> 按定义只保留<b>该类型自己声明</b>的非公开属性，
/// 不上溯基类；而 <c>Miko.Ionic.Components.IonicComponentBase</c> 把 <c>PlatformInfo</c>
/// （决定 md/ios 模式）、<c>CascadingTheme</c> 等五个注入点声明为 <c>protected</c>，由几十个组件继承。
/// 实测当前 ILC 即使不加这一位也保住了它们（基类本身被保留，其成员随之可达），但那是实现细节
/// 而非契约——显式声明才不至于哪天随裁剪策略收紧而失效。公开属性本就含继承成员，
/// 故无需对应的 WithInherited 位（框架也没有这个枚举值）。</para>
/// </summary>
internal static class ComponentTypeMembers
{
    /// <summary>组件参数发现所需保留的成员。</summary>
    internal const DynamicallyAccessedMemberTypes Parameters =
        DynamicallyAccessedMemberTypes.PublicProperties
        | DynamicallyAccessedMemberTypes.NonPublicProperties
        | DynamicallyAccessedMemberTypes.NonPublicPropertiesWithInherited;

    /// <summary>在 <see cref="Parameters"/> 之上再保留构造函数——用于需要实例化组件的入口。</summary>
    internal const DynamicallyAccessedMemberTypes Activation =
        Parameters | DynamicallyAccessedMemberTypes.PublicConstructors;
}
