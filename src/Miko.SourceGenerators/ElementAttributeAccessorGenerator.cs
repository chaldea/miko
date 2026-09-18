using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Miko.SourceGenerators;

/// <summary>
/// 为 <c>Miko.Core.Element</c> 的每个子类生成按名称读取「HTML 属性」的访问器，供 CSS 属性选择器
/// （<c>[type="checkbox"]</c>、<c>[href]</c>）使用。
///
/// <para>Miko 没有属性字典——HTML 属性就是元素的 CLR 属性——所以
/// <c>AttributeSelector.Matches</c> 原本用 <c>GetType().GetProperty(name, IgnoreCase)</c> 反射查找。
/// 裁剪器看不见那次查找（IL2075），Native AOT 下属性被裁掉后查找恒返回 null，于是所有属性选择器
/// 静默失配：规则还在样式表里，却永远不命中。本生成器把这层反射替换为按具体类型分派的
/// switch（ISSUE-140）。</para>
///
/// <para>只生成本程序集内声明的元素类型。外部程序集自定义的 <c>Element</c> 子类可以重写
/// <c>Element.TryGetAttributeValue</c> 自行提供。</para>
/// </summary>
[Generator]
public class ElementAttributeAccessorGenerator : IIncrementalGenerator
{
    /// <summary>
    /// 不作为「HTML 属性」暴露的成员：引擎内部状态（布局盒、父子引用、脏标记、滚动位置、
    /// 解码后的位图等），在真实 DOM 里也不是属性，不该被 <c>[attr]</c> 匹配到。
    ///
    /// <para><c>Id</c> 与 <c>Class</c> <b>不</b>在此列——它们是货真价实的 HTML 属性，
    /// CSS 里 <c>[class~="x"]</c>、<c>[id^="x"]</c> 都是合法且有用的写法，
    /// 只是通常更常用 <c>.x</c> / <c>#x</c> 的简写形式而已。</para>
    /// </summary>
    private static readonly HashSet<string> ExcludedNames = new()
    {
        "Style", "Children", "Parent", "TagName", "IsDirty", "LayoutBox",
        "TextContent", "Owner", "SupersededBy", "DisposeCallback", "IsSelectable", "State",
        "ScrollTop", "InitialScrollTop", "OffsetWidth", "OffsetHeight", "Bitmap",
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var elementTypes = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                transform: static (ctx, _) => GetElementInfo(ctx))
            .Where(static info => info != null)
            .Collect();

        context.RegisterSourceOutput(elementTypes, static (spc, infos) => Execute(spc, infos!));
    }

    private static ElementInfo? GetElementInfo(GeneratorSyntaxContext context)
    {
        var declaration = (ClassDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(declaration) is not INamedTypeSymbol symbol)
            return null;

        if (symbol.IsAbstract || !InheritsFromElement(symbol))
            return null;

        // 同名属性可能在派生类中被 new 遮蔽——取最派生的那一个（先遍历自身，再向上）。
        var properties = new List<(string Name, string Type)>();
        var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        for (var current = symbol; current != null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is not IPropertySymbol property) continue;
                if (property.DeclaredAccessibility != Accessibility.Public) continue;
                if (property.IsStatic || property.IsIndexer || property.GetMethod == null) continue;
                if (ExcludedNames.Contains(property.Name)) continue;
                // 事件处理器槽（OnClick 等）是委托，不是 HTML 属性——CSS 无从比较其值。
                if (property.Type.TypeKind == TypeKind.Delegate) continue;
                if (!seen.Add(property.Name)) continue;

                properties.Add((property.Name, property.Type.ToDisplayString()));
            }
        }

        if (properties.Count == 0)
            return null;

        properties.Sort(static (a, b) => string.CompareOrdinal(a.Name, b.Name));
        return new ElementInfo(symbol.ToDisplayString(), properties, GetInheritanceDepth(symbol));
    }

    private static bool InheritsFromElement(INamedTypeSymbol symbol)
    {
        for (var baseType = symbol.BaseType; baseType != null; baseType = baseType.BaseType)
        {
            if (baseType.ToDisplayString() == "Miko.Core.Element")
                return true;
        }
        return false;
    }

    /// <summary>到 <c>Miko.Core.Element</c> 的继承层数，用于给 switch 的类型模式排序。</summary>
    private static int GetInheritanceDepth(INamedTypeSymbol symbol)
    {
        var depth = 0;
        for (var baseType = symbol.BaseType; baseType != null; baseType = baseType.BaseType)
        {
            depth++;
            if (baseType.ToDisplayString() == "Miko.Core.Element")
                break;
        }
        return depth;
    }

    private static void Execute(SourceProductionContext context, System.Collections.Immutable.ImmutableArray<ElementInfo?> infos)
    {
        var elements = infos.Where(static i => i != null).Select(static i => i!)
            .GroupBy(static i => i.TypeName)
            .Select(static g => g.First())
            // 派生类型必须排在基类型之前：switch 的类型模式按顺序匹配，基类模式在前会吞掉派生类型
            // （如 ThElement : TdElement 就会永远走不到）。继承深度降序即可保证这一点。
            .OrderByDescending(static i => i.Depth)
            .ThenBy(static i => i.TypeName, System.StringComparer.Ordinal)
            .ToList();

        if (elements.Count == 0)
            return;

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace Miko.Core;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// CSS 属性选择器用的元素属性访问表，由 ElementAttributeAccessorGenerator 生成。");
        sb.AppendLine("/// 取代 <c>GetType().GetProperty(name, IgnoreCase)</c>，使属性选择器在 Native AOT 下");
        sb.AppendLine("/// 仍然可用（ISSUE-140）。");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("internal static class ElementAttributeAccessor");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>按名称（忽略大小写）读取元素属性值；元素没有该属性时返回 false。</summary>");
        sb.AppendLine("    internal static bool TryGetValue(global::Miko.Core.Element element, string name, out object? value)");
        sb.AppendLine("    {");
        sb.AppendLine("        switch (element)");
        sb.AppendLine("        {");

        foreach (var element in elements)
        {
            sb.AppendLine($"            case global::{element.TypeName} __e:");
            sb.AppendLine("                switch (name.ToLowerInvariant())");
            sb.AppendLine("                {");
            foreach (var (propertyName, _) in element.Properties)
            {
                sb.AppendLine($"                    case \"{propertyName.ToLowerInvariant()}\": value = __e.{propertyName}; return true;");
            }
            sb.AppendLine("                }");
            sb.AppendLine("                break;");
        }

        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        value = null;");
        sb.AppendLine("        return false;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("ElementAttributeAccessor.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private sealed class ElementInfo
    {
        internal ElementInfo(string typeName, List<(string Name, string Type)> properties, int depth)
        {
            TypeName = typeName;
            Properties = properties;
            Depth = depth;
        }

        internal string TypeName { get; }
        internal List<(string Name, string Type)> Properties { get; }

        /// <summary>到 <c>Element</c> 的继承层数；生成 switch 时按其降序排列。</summary>
        internal int Depth { get; }
    }
}
