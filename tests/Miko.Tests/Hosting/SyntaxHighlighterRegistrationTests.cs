using Microsoft.Extensions.DependencyInjection;
using Miko.Highlight;
using Miko.Hosting;
using Shouldly;

namespace Miko.Tests.Hosting;

/// <summary>
/// ISSUE-098 内容重构：<see cref="ISyntaxHighlighter"/> 的 DI 注册与覆盖。
/// 默认注册内置 <see cref="SyntaxHighlighter"/>；应用重新注册该接口即可替换高亮实现。
/// </summary>
public class SyntaxHighlighterRegistrationTests
{
    private sealed class CustomHighlighter : ISyntaxHighlighter
    {
        public SyntaxTheme Theme => SyntaxTheme.Default;
        public IReadOnlyList<CodeToken>? Tokenize(string text, string? language) => null;
    }

    [Fact]
    public void CreateDefault_RegistersBuiltInHighlighter()
    {
        var builder = MikoAppBuilder.CreateDefault();
        using var provider = builder.Services.BuildServiceProvider();

        provider.GetService<ISyntaxHighlighter>().ShouldBeOfType<SyntaxHighlighter>();
    }

    [Fact]
    public void CustomRegistration_AfterCreateDefault_OverridesBuiltIn()
    {
        // 后注册者优先：应用在 CreateDefault 之后注册自己的实现即可覆盖。
        var builder = MikoAppBuilder.CreateDefault();
        builder.Services.AddSingleton<ISyntaxHighlighter, CustomHighlighter>();
        using var provider = builder.Services.BuildServiceProvider();

        provider.GetService<ISyntaxHighlighter>().ShouldBeOfType<CustomHighlighter>();
    }

    [Fact]
    public void UseSyntaxHighlighter_RegistersCustomImplementation()
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.UseSyntaxHighlighter<CustomHighlighter>();
        using var provider = builder.Services.BuildServiceProvider();

        provider.GetService<ISyntaxHighlighter>().ShouldBeOfType<CustomHighlighter>();
    }
}
