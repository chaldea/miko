using System.Reflection;
using Microsoft.AspNetCore.Razor.Language.Components.Css;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;

namespace Miko.Razor.Compiler.Tests;

/// <summary>Drives the CSS translator and compiles what it emits against the real Miko assembly.</summary>
internal static class CssTestHelpers
{
    /// <summary>A declaration list (the content of a style attribute or a block).</summary>
    public static (CssStyleBlock Block, List<CssDiagnostic> Diagnostics) Declarations(string css, bool allowDynamic = false)
    {
        var diagnostics = new List<CssDiagnostic>();
        var declarations = CssParser.ParseDeclarationList(css, diagnostics);
        var block = CssPropertyTranslator.TranslateBlock(new CssTranslationContext(css, diagnostics, allowDynamic), declarations);
        return (block, diagnostics);
    }

    /// <summary>The C# expression a single-longhand declaration assigns to <paramref name="property"/>.</summary>
    public static string Expression(string css, string property)
    {
        var (block, diagnostics) = Declarations(css);
        diagnostics.ShouldBeEmpty(string.Join("; ", diagnostics.Select(d => d.Message)));
        block.TryGet(property, out var expression).ShouldBeTrue($"'{css}' does not assign {property}; it assigns {string.Join(", ", block.Properties)}.");
        return expression;
    }

    /// <summary>The messages of a declaration list that fails or is approximated.</summary>
    public static List<string> Problems(string css, bool allowDynamic = false)
        => Declarations(css, allowDynamic).Diagnostics.Select(d => d.Descriptor.Id + " " + d.Message).ToList();

    public static List<string> Selectors(string selector, string? scope = null, bool allowPseudoElements = true)
        => CssSelectorTranslator.Translate(selector, 0, selector.Length, scope, allowPseudoElements, new List<CssDiagnostic>());

    public static List<string> SelectorProblems(string selector, string? scope = null)
    {
        var diagnostics = new List<CssDiagnostic>();
        CssSelectorTranslator.Translate(selector, 0, selector.Length, scope, allowPseudoElements: true, diagnostics);
        return diagnostics.Select(d => d.Descriptor.Id + " " + d.Message).ToList();
    }

    /// <summary>Translates a stylesheet into the body of the generated build method.</summary>
    public static (string Body, List<CssDiagnostic> Diagnostics) StyleSheet(string css, string? scope = null)
    {
        var emitter = new CssStyleSheetEmitter();
        var diagnostics = new List<CssDiagnostic>();
        emitter.Add(css, scope, diagnostics);
        return (emitter.Body, diagnostics);
    }

    /// <summary>
    /// Compiles <paramref name="members"/> into a class <c>Generated</c> against Miko and returns
    /// its type. Fails with the compiler errors.
    /// </summary>
    public static Type Compile(string members)
    {
        var source = $$"""
            #nullable disable
            public static class Generated
            {
            {{members}}
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Append(typeof(Miko.Styling.Style).Assembly.Location)
            .Distinct()
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("CssTest" + Guid.NewGuid().ToString("N"),
            new[] { CSharpSyntaxTree.ParseText(source) }, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString()).ToList();
        errors.ShouldBeEmpty("generated code:\n" + source + "\nerrors:\n" + string.Join("\n", errors));
        return Assembly.Load(stream.ToArray()).GetType("Generated")!;
    }

    /// <summary>Compiles a declaration list into <c>new Style { … }</c> and builds it.</summary>
    public static Miko.Styling.Style BuildStyle(string css)
    {
        var (block, diagnostics) = Declarations(css);
        diagnostics.Where(d => d.Kind != CssDiagnosticKind.Approximated).ShouldBeEmpty(string.Join("; ", diagnostics.Select(d => d.Message)));
        var type = Compile($"public static global::Miko.Styling.Style Build() => {block.ToInitializer("global::Miko.Styling.Style", "    ")};");
        return (Miko.Styling.Style)type.GetMethod("Build")!.Invoke(null, null)!;
    }

    /// <summary>Compiles a stylesheet into <c>Build()</c> and builds the sheet.</summary>
    public static Miko.Styling.StyleSheet BuildStyleSheet(string css, string? scope = null)
    {
        var (body, diagnostics) = StyleSheet(css, scope);
        diagnostics.Where(d => d.Kind != CssDiagnosticKind.Approximated).ShouldBeEmpty(string.Join("; ", diagnostics.Select(d => d.Message)));
        var type = Compile($$"""
            public static global::Miko.Styling.StyleSheet Build()
            {
                var sheet = new global::Miko.Styling.StyleSheet();
            {{body}}
                return sheet;
            }
            """);
        return (Miko.Styling.StyleSheet)type.GetMethod("Build")!.Invoke(null, null)!;
    }
}
