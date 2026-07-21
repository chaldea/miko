using Microsoft.Extensions.DependencyInjection;
using Miko.Highlight;

namespace Miko.Hosting;

/// <summary>
/// 语法高亮的 DI 注册扩展。<c>&lt;code language="..."&gt;</c> 的高亮由
/// <see cref="ISyntaxHighlighter"/> 提供，默认注册内置的 <see cref="SyntaxHighlighter"/>；
/// 应用可重新注册该接口以替换高亮实现（见 ISSUE-098）。
/// </summary>
public static class HighlightingExtensions
{
    /// <summary>
    /// 注册默认的语法高亮服务（内置 <see cref="SyntaxHighlighter"/>，正则驱动、
    /// 支持常见语言）。多次注册时后注册者优先，因此应用可在
    /// <c>MikoAppBuilder.CreateDefault</c> 之后用自己的实现覆盖。
    /// </summary>
    public static IServiceCollection AddSyntaxHighlighter(this IServiceCollection service)
    {
        service.AddSingleton<ISyntaxHighlighter, SyntaxHighlighter>();
        return service;
    }

    /// <summary>
    /// 注册自定义的 <see cref="ISyntaxHighlighter"/> 实现，覆盖内置高亮器。
    /// </summary>
    public static MikoAppBuilder UseSyntaxHighlighter<THighlighter>(this MikoAppBuilder builder)
        where THighlighter : class, ISyntaxHighlighter
    {
        builder.Services.AddSingleton<ISyntaxHighlighter, THighlighter>();
        return builder;
    }
}
