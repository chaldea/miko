using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Miko.SourceGenerators.Models;

namespace Miko.SourceGenerators;

[Generator]
public class StylePropertyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 查找 Style 类
        var styleProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsStyleClass(node),
                transform: static (ctx, _) => GetStyleInfo(ctx))
            .Where(static info => info != null);

        context.RegisterSourceOutput(styleProvider, static (spc, styleInfo) => Execute(spc, styleInfo!));
    }

    private static bool IsStyleClass(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax cls &&
               cls.Identifier.Text == "Style";
    }

    private static StyleInfo? GetStyleInfo(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

        if (classSymbol == null)
            return null;

        // 检查是否是 Miko.Styling.Style
        if (classSymbol.ContainingNamespace?.ToDisplayString() != "Miko.Styling")
            return null;

        // 收集 ComputedStyle 以 new 遮蔽的属性名：这些属性有已解析的计算值，可支持 inherit/unset
        // 读取父元素计算值；未遮蔽者无计算值可继承（读取会命中基类的 StyleProperty<T>? 类型）。
        var shadowedNames = new HashSet<string>();
        var computedStyleSymbol = classSymbol.ContainingNamespace?.GetTypeMembers("ComputedStyle").FirstOrDefault();
        if (computedStyleSymbol != null)
        {
            foreach (var member in computedStyleSymbol.GetMembers())
            {
                // ComputedStyle 中直接声明的属性即为其 new 遮蔽的（非空计算值）属性。
                if (member is IPropertySymbol csProp)
                    shadowedNames.Add(csProp.Name);
            }
        }

        var properties = new List<PropertyInfo>();

        foreach (var member in classSymbol.GetMembers())
        {
            if (member is not IPropertySymbol property)
                continue;

            // 跳过简写属性（Padding, Margin, Border 等）
            if (IsShorthandProperty(property.Name))
                continue;

            var propertyType = property.Type;
            var isNullable = propertyType.NullableAnnotation == NullableAnnotation.Annotated;

            // 只处理可空属性和集合属性
            if (!isNullable && !IsCollectionType(propertyType))
                continue;

            properties.Add(new PropertyInfo
            {
                Name = property.Name,
                Type = propertyType.ToDisplayString(),
                IsNullable = isNullable,
                IsValueType = propertyType.IsValueType,
                ShadowedByComputedStyle = shadowedNames.Contains(property.Name)
            });
        }

        return new StyleInfo { Properties = properties };
    }

    private static bool IsShorthandProperty(string name)
    {
        return name is "Padding" or "Margin" or "Border" or "BorderTop" or "BorderRight"
            or "BorderBottom" or "BorderLeft" or "BorderRadius" or "Overflow" or "Flex"
            or "Outline"
            // Vars（自定义变量字典）不参与通用的合并/应用逻辑：它按键手动合并
            // （见 Style.Merge），且不是 StyleProperty<T>?，不能走统一的解析路径。
            or "Vars";
    }

    private static bool IsCollectionType(ITypeSymbol type)
    {
        var typeName = type.ToDisplayString();
        return typeName.StartsWith("System.Collections.Generic.List<");
    }

    /// <summary>
    /// 从后备属性类型 <c>...StyleProperty&lt;T&gt;?</c> 中取出内层 <c>T</c> 的显示字符串，
    /// 用于在生成的解析调用上显式提供泛型实参（消除 <c>default</c> 分支导致的类型推断歧义）。
    /// </summary>
    private static string ExtractInnerType(string propertyType)
    {
        // 去掉末尾的可空标记 '?'
        var type = propertyType.TrimEnd('?');
        int lt = type.IndexOf('<');
        int gt = type.LastIndexOf('>');
        if (lt >= 0 && gt > lt)
            return type.Substring(lt + 1, gt - lt - 1);
        return type;   // 兜底（正常不应触发）
    }

    /// <summary>
    /// CSS 可继承属性集合，用于生成 inherit/unset/revert 关键词消解时的走向判定。
    /// 必须与 <c>StyleResolver.InheritFromParent</c> 保持一致。
    /// </summary>
    private static readonly HashSet<string> InheritableProperties = new()
    {
        "Color",
        "FontFamily",
        "FontSize",
        "FontWeight",
        "TextAlign",
        "LineHeight",
        "PointerEvents",
        "WhiteSpace",
        // 文本相关的 CSS 可继承属性
        "TextTransform",
        "LetterSpacing",
        "OverflowWrap",
        "WordBreak",
        // visibility 在 CSS 中可继承（子元素可通过 visibility:visible 覆盖被隐藏的父元素）
        "Visibility",
        // user-select 规范上非继承，但实际会向下传播；此处按继承处理以贴合作者预期
        "UserSelect",
    };

    private static void Execute(SourceProductionContext context, StyleInfo styleInfo)
    {
        // 生成 HasAnyProperty 方法
        var hasAnyPropertySource = GenerateHasAnyProperty(styleInfo);
        context.AddSource("Style.HasAnyProperty.g.cs", SourceText.From(hasAnyPropertySource, Encoding.UTF8));

        // 生成 Merge 辅助方法
        var mergeSource = GenerateMergeHelper(styleInfo);
        context.AddSource("Style.MergeHelper.g.cs", SourceText.From(mergeSource, Encoding.UTF8));

        // 生成 ComputedStyle.FromStyle 辅助方法
        var fromStyleSource = GenerateFromStyleHelper(styleInfo);
        context.AddSource("ComputedStyle.FromStyleHelper.g.cs", SourceText.From(fromStyleSource, Encoding.UTF8));
    }

    private static string GenerateHasAnyProperty(StyleInfo styleInfo)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace Miko.Styling;");
        sb.AppendLine();
        sb.AppendLine("public partial class Style");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 判断样式对象是否有任何非空属性（自动生成）");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public bool HasAnyProperty()");
        sb.AppendLine("    {");
        sb.Append("        return ");

        var conditions = styleInfo.Properties
            .Select(p => $"{p.Name} != null")
            .ToList();

        for (int i = 0; i < conditions.Count; i++)
        {
            if (i > 0)
                sb.Append(" ||\n               ");
            sb.Append(conditions[i]);
        }

        sb.AppendLine(";");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GenerateMergeHelper(StyleInfo styleInfo)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace Miko.Styling;");
        sb.AppendLine();
        sb.AppendLine("public partial class Style");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 合并样式的自动生成部分");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    partial void MergeGenerated(Style other)");
        sb.AppendLine("    {");

        foreach (var prop in styleInfo.Properties)
        {
            sb.AppendLine($"        {prop.Name} ??= other.{prop.Name};");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GenerateFromStyleHelper(StyleInfo styleInfo)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace Miko.Styling;");
        sb.AppendLine();
        sb.AppendLine("public partial class ComputedStyle");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 应用样式属性的自动生成部分");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    partial void ApplyStylePropertiesGenerated(Style style)");
        sb.AppendLine("    {");

        foreach (var prop in styleInfo.Properties)
        {
            // 每个后备属性现为 StyleProperty<T>?（联合“具体值 | 变量引用 | 全局关键词”）。
            // 先解出变量引用/关键词（针对当前变量作用域与父上下文），成功取得具体值后再写入
            // ComputedStyle 的非空遮蔽。未解析（作用域缺失且无 fallback，或 initial/无父的 inherit）
            // 则不写入 → 保持 ComputedStyle 默认/继承值。
            var local = "__sp_" + prop.Name;
            var value = "__v_" + prop.Name;
            var inner = ExtractInnerType(prop.Type);   // StyleProperty<T>? 的内层 T
            // 父属性值：inherit/unset 关键词消解时读取。仅 ComputedStyle 遮蔽的属性有已解析计算值可读；
            // 未遮蔽的属性无计算值可继承，恒传 default!（关键词只会退回默认）。
            // 显式提供泛型实参 <inner> 消除 default 分支带来的类型推断歧义；default! 抑制可空告警。
            var parentValue = prop.ShadowedByComputedStyle
                ? $"_keywordResolutionParent != null ? _keywordResolutionParent.{prop.Name} : default!"
                : "default!";
            var inheritable = InheritableProperties.Contains(prop.Name) ? "true" : "false";
            sb.AppendLine(
                $"        if (style.{prop.Name} is {{ }} {local} && TryResolveStyleProperty<{inner}>({local}, {parentValue}, {inheritable}, out var {value})) {prop.Name} = {value};");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
