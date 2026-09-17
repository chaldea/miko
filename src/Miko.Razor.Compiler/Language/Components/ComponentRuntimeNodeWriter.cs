// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Components;

/// <summary>
/// Generates the C# code corresponding to Razor source document contents.
/// </summary>
internal class ComponentRuntimeNodeWriter : ComponentNodeWriter
{
    private readonly ImmutableArray<IntermediateToken>.Builder _currentAttributeValues = ImmutableArray.CreateBuilder<IntermediateToken>();
    private int _sourceSequence;

    // <pre> 子树深度：pre 是预格式化元素，其内部的空白文本（换行、缩进）有语义，
    // 不能像普通标记那样跳过（见 WriteHtmlContent 与 ISSUE-098）。
    private int _preformattedDepth;

    // 当前开启的元素（ISSUE-136）。属性节点在中间树里不知道自己属于哪个标签，
    // 而强类型赋值既需要标签名（查属性槽位）也需要局部变量名（赋值目标），
    // 故由 WriteMarkupElement 在渲染其属性期间下传。
    private OpenElementInfo _currentElement;

    // 局部变量序号：同一方法体内的元素（含嵌套与同级）必须各有唯一名字。
    private int _elementVariableIndex;

    // 当前开启的组件的局部变量名，null 表示没有（或类型推断分支走旧路径）。
    private string? _currentComponent;

    private int _componentVariableIndex;

    /// <summary>
    /// The element whose attributes are currently being emitted. <see cref="VariableName"/> is
    /// null for a tag with no compile-time element type, which keeps the legacy emit.
    /// </summary>
    private readonly record struct OpenElementInfo(string? TagName, string? VariableName)
    {
        [MemberNotNullWhen(true, nameof(TagName), nameof(VariableName))]
        public bool IsTyped => VariableName is not null && TagName is not null;
    }

    /// <summary>
    /// Unique local name per element. The counter is per-writer (i.e. per generated document),
    /// so names never collide across nested elements, sibling elements, or lambda scopes.
    /// </summary>
    private string NextElementVariableName() => $"__e{_elementVariableIndex++}";

    /// <summary>Unique local name per component instance. See <see cref="NextElementVariableName"/>.</summary>
    private string NextComponentVariableName() => $"__c{_componentVariableIndex++}";


    public ComponentRuntimeNodeWriter(RazorLanguageVersion version) : base(version)
    {
    }

    public override void WriteCSharpCode(CodeRenderingContext context, CSharpCodeIntermediateNode node)
    {
        var isWhitespaceStatement = true;
        foreach (var child in node.Children)
        {
            if (child is not IntermediateToken token || !string.IsNullOrWhiteSpace(token.Content))
            {
                isWhitespaceStatement = false;
                break;
            }
        }

        if (node.Source is null && isWhitespaceStatement)
        {
            // If source is null, we won't create source mappings, and if we're not creating source mappings,
            // there is no point emitting whitespace
            return;
        }

        foreach (var child in node.Children)
        {
            if (child is CSharpIntermediateToken token)
            {
                WriteCSharpToken(context, token);
            }
            else
            {
                // There may be something else inside the statement like an extension node.
                context.RenderNode(child);
            }
        }

        context.CodeWriter.WriteLine();
    }

    public override void WriteCSharpExpression(CodeRenderingContext context, CSharpExpressionIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        var characterOffset = BuilderVariableName.Length // for "__builder"
            + 1 // for '.'
            + ComponentsApi.RenderTreeBuilder.AddContent.Length
            + 1 // for '('
            + _sourceSequence.CountDigits() // for the sequence number
            + 2; // for ', '

        // Sequence points can only be emitted when the eval stack is empty. That means we can't arbitrarily map everything that could be in
        // the node. Instead we map just the first C# child node by putting the pragma before we start the method invocation and offset it.
        // This is not a perfect mapping, but generally this works for most cases:
        // - Common case: there is only a single node and it is C#, so it maps correctly
        // - There is some C# followed by a render template: the C# gets mapped, and the render template issues a lambda call which conceptually
        //   is another method so a sequence point can be emitted. Unfortunately any trailing C# is not mapped, although in many cases it's uninteresting
        //   such as closing parenthesis.
        // - Error cases: there are no nodes, so we do nothing
        var firstCSharpChild = node.Children.OfType<CSharpIntermediateToken>().FirstOrDefault();
        using (context.BuildEnhancedLinePragma(firstCSharpChild?.Source, characterOffset))
        {
            context.CodeWriter
                .WriteStartMethodInvocation($"{BuilderVariableName}.{ComponentsApi.RenderTreeBuilder.AddContent}")
                .WriteIntegerLiteral(_sourceSequence++)
                .WriteParameterSeparator();

            if (firstCSharpChild is not null)
            {
                context.CodeWriter.Write(firstCSharpChild.Content);
            }
        }

        // render the remaining children. We still emit the #line pragmas for the remaining csharp tokens but
        // these wont actually generate any sequence points for debugging.

        foreach (var child in node.Children)
        {
            if (child == firstCSharpChild)
            {
                continue;
            }

            if (child is CSharpIntermediateToken csharpToken)
            {
                WriteCSharpToken(context, csharpToken);
            }
            else
            {
                // There may be something else inside the expression like a Template or another extension node.
                context.RenderNode(child);
            }
        }

        context.CodeWriter.WriteEndMethodInvocation();
    }

    public override void WriteCSharpExpressionAttributeValue(CodeRenderingContext context, CSharpExpressionAttributeValueIntermediateNode node)
    {
        // In cases like "somestring @variable", Razor tokenizes it as:
        //  [0] HtmlContent="somestring"
        //  [1] CsharpContent="variable" Prefix=" "
        // ... so to avoid losing whitespace, convert the prefix to a further token in the list
        if (!string.IsNullOrEmpty(node.Prefix))
        {
            _currentAttributeValues.Add(IntermediateNodeFactory.HtmlToken(node.Prefix));
        }

        foreach (var child in node.Children)
        {
            _currentAttributeValues.Add((IntermediateToken)child);
        }
    }

    public override void WriteMarkupBlock(CodeRenderingContext context, MarkupBlockIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        context.CodeWriter
            .WriteStartMethodInvocation($"{BuilderVariableName}.{ComponentsApi.RenderTreeBuilder.AddMarkupContent}")
            .WriteIntegerLiteral(_sourceSequence++)
            .WriteParameterSeparator()
            .WriteStringLiteral(node.Content)
            .WriteEndMethodInvocation();
    }

    public override void WriteMarkupElement(CodeRenderingContext context, MarkupElementIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        // 强类型发射（ISSUE-136）：标签名是源码里的字面量，元素类型因此在编译期已知。
        // 已知标签走 OpenElement<TElement>() + 直接属性赋值；未知标签回落到旧的字符串路径，
        // 行为与此前完全一致。
        var elementTypeName = MikoElements.GetElementTypeName(node.TagName);
        var openElement = default(OpenElementInfo);

        if (elementTypeName is not null)
        {
            var elementVariable = NextElementVariableName();
            // var __e0 = __builder.OpenElement<global::Miko.Core.DomElements.DivElement>();
            context.CodeWriter.Write("var ");
            context.CodeWriter.Write(elementVariable);
            context.CodeWriter.Write(" = ");
            context.CodeWriter.Write(BuilderVariableName);
            context.CodeWriter.Write(".");
            context.CodeWriter.Write(ComponentsApi.RenderTreeBuilder.OpenElement);
            context.CodeWriter.Write("<");
            context.CodeWriter.Write(elementTypeName);
            context.CodeWriter.WriteLine(">();");
            openElement = new OpenElementInfo(node.TagName, elementVariable);
        }
        else
        {
            context.CodeWriter
                .WriteStartMethodInvocation($"{BuilderVariableName}.{ComponentsApi.RenderTreeBuilder.OpenElement}")
                .WriteIntegerLiteral(_sourceSequence++)
                .WriteParameterSeparator()
                .WriteStringLiteral(node.TagName)
                .WriteEndMethodInvocation();
        }

        // 属性发射期间，把「当前开启的元素」交给 WriteHtmlAttribute / WriteComponentAttribute
        // ——它们通过节点树拿不到所属标签，只能由这里下传（见 _currentElement）。
        var previousElement = _currentElement;
        _currentElement = openElement;

        bool hasFormName = false;

        // Render attributes and splats (in order) before creating the scope.
        foreach (var child in node.Children)
        {
            if (child is HtmlAttributeIntermediateNode attribute)
            {
                context.RenderNode(attribute);
            }
            else if (child is ComponentAttributeIntermediateNode componentAttribute)
            {
                context.RenderNode(componentAttribute);
            }
            else if (child is SplatIntermediateNode splat)
            {
                context.RenderNode(splat);
            }
            else if (child is FormNameIntermediateNode formName)
            {
                Debug.Assert(!hasFormName);
                context.RenderNode(formName);
                hasFormName = true;
            }
        }

        foreach (var setKey in node.SetKeys)
        {
            context.RenderNode(setKey);
        }

        foreach (var capture in node.Captures)
        {
            context.RenderNode(capture);
        }

        // AddNamedEvent must be called after all attributes (but before child content).
        if (hasFormName)
        {
            // _builder.AddNamedEvent("onsubmit", __formName);
            context.CodeWriter.WriteLine($"{BuilderVariableName}.{ComponentsApi.RenderTreeBuilder.AddNamedEvent}(\"onsubmit\", {FormNameVariableName});");
            ScopeStack.IncrementFormName();
        }

        // 属性发射结束：本元素的属性都写完了，body 里的子元素各有自己的开启元素。
        _currentElement = default;

        // <pre> 子树的空白内容需要保留（预格式化），在渲染 body 期间跟踪深度。
        bool isPre = string.Equals(node.TagName, "pre", StringComparison.OrdinalIgnoreCase);
        if (isPre)
        {
            _preformattedDepth++;
        }

        try
        {
            // Render body of the tag inside the scope
            foreach (var child in node.Body)
            {
                context.RenderNode(child);
            }
        }
        finally
        {
            if (isPre)
            {
                _preformattedDepth--;
            }

            // 恢复外层元素：嵌套元素闭合后，外层可能还有属性要写（罕见但合法）。
            _currentElement = previousElement;
        }

        context.CodeWriter
            .WriteStartMethodInvocation($"{BuilderVariableName}.{ComponentsApi.RenderTreeBuilder.CloseElement}")
            .WriteEndMethodInvocation();
    }

    public override void WriteHtmlAttribute(CodeRenderingContext context, HtmlAttributeIntermediateNode node)
    {
        Debug.Assert(_currentAttributeValues.Count == 0);
        context.RenderChildren(node);

        if (node.AttributeNameExpression == null)
        {
            WriteAttribute(context, node.AttributeName, _currentAttributeValues.ToImmutableAndClear());
        }
        else
        {
            WriteAttribute(context, node.AttributeNameExpression, _currentAttributeValues.ToImmutableAndClear());
        }

        if (!string.IsNullOrEmpty(node.EventUpdatesAttributeName))
        {
            context.CodeWriter
                .WriteStartMethodInvocation($"{BuilderVariableName}.{ComponentsApi.RenderTreeBuilder.SetUpdatesAttributeName}")
                .WriteStringLiteral(node.EventUpdatesAttributeName)
                .WriteEndMethodInvocation();
        }
    }

    public override void WriteHtmlAttributeValue(CodeRenderingContext context, HtmlAttributeValueIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        var stringContent = ((IntermediateToken)node.Children.Single()).Content;
        _currentAttributeValues.Add(IntermediateNodeFactory.HtmlToken(node.Prefix + stringContent));
    }

    public override void WriteHtmlContent(CodeRenderingContext context, HtmlContentIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        // Text node
        var content = GetHtmlContent(node);

        // Miko renders to a box-model layout that does not collapse HTML whitespace the
        // way a browser does, so emitting empty or whitespace-only text (the newlines and
        // indentation between markup) would add spurious layout content. Skip it entirely
        // — and don't consume a sequence number, keeping the generated code clean.
        // Exception: inside <pre> whitespace is significant (preformatted text), so it
        // must be emitted for the engine's white-space: pre pipeline to preserve.
        if (string.IsNullOrEmpty(content))
        {
            return;
        }
        if (_preformattedDepth == 0 && string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        var renderApi = ComponentsApi.RenderTreeBuilder.AddContent;
        if (node.HasEncodedContent)
        {
            // This content is already encoded.
            renderApi = ComponentsApi.RenderTreeBuilder.AddMarkupContent;
        }

        context.CodeWriter
            .WriteStartMethodInvocation($"{BuilderVariableName}.{renderApi}")
            .WriteIntegerLiteral(_sourceSequence++)
            .WriteParameterSeparator()
            .WriteStringLiteral(content)
            .WriteEndMethodInvocation();
    }

    public override void WriteUsingDirective(CodeRenderingContext context, UsingDirectiveIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (node.Source is { FilePath: not null } sourceSpan)
        {
            using (context.BuildEnhancedLinePragma(sourceSpan, suppressLineDefaultAndHidden: true))
            {
                context.CodeWriter.WriteUsing(node.Content, endLine: node.HasExplicitSemicolon);
            }
            if (!node.HasExplicitSemicolon)
            {
                context.CodeWriter.WriteLine(";");
            }
            if (node.AppendLineDefaultAndHidden)
            {
                context.CodeWriter.WriteLine("#line default");
                context.CodeWriter.WriteLine("#line hidden");
            }
        }
        else
        {
            context.CodeWriter.WriteUsing(node.Content, endLine: true);

            if (node.AppendLineDefaultAndHidden)
            {
                context.CodeWriter.WriteLine("#line default");
                context.CodeWriter.WriteLine("#line hidden");
            }
        }
    }

    public override void WriteComponent(CodeRenderingContext context, ComponentIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (ShouldSuppressTypeInferenceCall(node))
        {
        }
        else if (node.TypeInferenceNode == null)
        {
            // If the component is not using type inference then we just write an open/close with a series
            // of add attribute calls in between.
            //
            // Writes something like:
            //
            // _builder.OpenComponent<MyComponent>(0);
            // _builder.AddComponentParameter(1, "Foo", ...);
            // _builder.AddComponentParameter(2, "ChildContent", ...);
            // _builder.SetKey(someValue);
            // _builder.AddElementCapture(3, (__value) => _field = __value);
            // _builder.CloseComponent();

            // 强类型发射（ISSUE-136）：把组件实例接到局部变量上，参数便可直接赋值，
            // 不再经 AddComponentParameter 的 GetProperty + SetValue（无缓存且值类型装箱）。
            // 显式写出类型参数的泛型组件（如 CascadingValue<T>）同样适用——类型实参就写在
            // OpenComponent<...> 上，var 能完整推断。只有类型推断分支（TypeInferenceNode
            // 非空，即本方法的 else 分支）不走这里。
            var componentVariable = NextComponentVariableName();

            // var __c0 = __builder.OpenComponent<global::Ns.TComponent>();
            context.CodeWriter.Write("var ");
            context.CodeWriter.Write(componentVariable);
            context.CodeWriter.Write(" = ");

            // _builder.OpenComponent<TComponent>(42);
            context.CodeWriter.Write(BuilderVariableName);
            context.CodeWriter.Write(".");
            context.CodeWriter.Write(ComponentsApi.RenderTreeBuilder.OpenComponent);
            context.CodeWriter.Write("<");

            var nonGenericTypeName = TypeNameHelper.GetNonGenericTypeName(node.TypeName, out _);
            TypeNameHelper.WriteGlobalPrefixIfNeeded(context.CodeWriter, nonGenericTypeName);
            WriteComponentTypeName(context, node, nonGenericTypeName);

            if (!node.OrderedTypeArguments.IsDefaultOrEmpty)
            {
                context.CodeWriter.Write("<");
                for (var i = 0; i < node.OrderedTypeArguments.Length; i++)
                {
                    var typeArg = node.OrderedTypeArguments[i];
                    WriteComponentTypeArgument(context, typeArg);
                    if (i != node.OrderedTypeArguments.Length - 1)
                    {
                        context.CodeWriter.Write(", ");
                    }
                }
                context.CodeWriter.Write(">");
            }

            context.CodeWriter.Write(">();");
            context.CodeWriter.WriteLine();

            // We can skip type arguments during runtime codegen, they are handled in the
            // type/parameter declarations.

            // 参数发射期间把开启的组件下传给 WriteComponentAttribute / WriteComponentChildContent。
            var previousComponent = _currentComponent;
            _currentComponent = componentVariable;

            bool hasRenderMode = false;

            try
            {
            // Preserve order of attributes and splats
            foreach (var child in node.Children)
            {
                if (child is ComponentAttributeIntermediateNode attribute)
                {
                    context.RenderNode(attribute);
                }
                else if (child is SplatIntermediateNode splat)
                {
                    context.RenderNode(splat);
                }
                else if (child is RenderModeIntermediateNode renderMode)
                {
                    Debug.Assert(!hasRenderMode);
                    context.RenderNode(renderMode);
                    hasRenderMode = true;
                }
            }

            foreach (var childContent in node.ChildContents)
            {
                context.RenderNode(childContent);
            }

            foreach (var setKey in node.SetKeys)
            {
                context.RenderNode(setKey);
            }

            foreach (var capture in node.Captures)
            {
                context.RenderNode(capture);
            }
            }
            finally
            {
                _currentComponent = previousComponent;
            }

            if (hasRenderMode)
            {
                // _builder.AddComponentRenderMode(__renderMode_0);
                WriteAddComponentRenderMode(context, BuilderVariableName, RenderModeVariableName);
                ScopeStack.IncrementRenderMode();
            }

            // _builder.CloseComponent();
            context.CodeWriter.Write(BuilderVariableName);
            context.CodeWriter.Write(".");
            context.CodeWriter.Write(ComponentsApi.RenderTreeBuilder.CloseComponent);
            context.CodeWriter.Write("();");
            context.CodeWriter.WriteLine();
        }
        else
        {
            var parameters = GetTypeInferenceMethodParameters(node.TypeInferenceNode);

            // If this component is going to cascade any of its generic types, we have to split its type inference
            // into two parts. First we call an inference method that captures all the parameters in local variables,
            // then we use those to call the real type inference method that emits the component. The reason for this
            // is so the captured variables can be used by descendants without re-evaluating the expressions.
            CodeWriterExtensions.CSharpCodeWritingScope? typeInferenceCaptureScope = null;
            if (node.Component.SuppliesCascadingGenericParameters())
            {
                typeInferenceCaptureScope = context.CodeWriter.BuildScope();
                TypeNameHelper.WriteGloballyQualifiedName(context.CodeWriter, node.TypeInferenceNode.FullTypeName);
                context.CodeWriter.Write(".");
                context.CodeWriter.Write(node.TypeInferenceNode.MethodName);
                context.CodeWriter.Write("_CaptureParameters(");
                var isFirst = true;
                foreach (var parameter in parameters.Where(p => p.UsedForTypeInference))
                {
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        context.CodeWriter.Write(", ");
                    }

                    WriteTypeInferenceMethodParameterInnards(context, parameter);
                    context.CodeWriter.Write(", out var ");

                    var variableName = new TypeInferenceArgName(ScopeStack.Depth, parameter.ParameterName);
                    context.CodeWriter.Write(variableName);

                    UseCapturedCascadingGenericParameterVariable(node, parameter, variableName);
                }
                context.CodeWriter.WriteLine(");");
            }

            // When we're doing type inference, we can't write all of the code inline to initialize
            // the component on the builder. We generate a method elsewhere, and then pass all of the information
            // to that method. We pass in all of the attribute values + the sequence numbers.
            //
            // __Blazor.MyComponent.TypeInference.CreateMyComponent_0(builder, 0, 1, ..., 2, ..., 3, ...);

            TypeNameHelper.WriteGloballyQualifiedName(context.CodeWriter, node.TypeInferenceNode.FullTypeName);
            context.CodeWriter.Write(".");
            context.CodeWriter.Write(node.TypeInferenceNode.MethodName);
            context.CodeWriter.Write("(");

            context.CodeWriter.Write(BuilderVariableName);
            context.CodeWriter.Write(", ");

            context.CodeWriter.WriteIntegerLiteral(_sourceSequence++);

            foreach (var parameter in parameters)
            {
                context.CodeWriter.Write(", ");

                if (parameter.SeqName != null)
                {
                    context.CodeWriter.WriteIntegerLiteral(_sourceSequence++);
                    context.CodeWriter.Write(", ");
                }

                WriteTypeInferenceMethodParameterInnards(context, parameter);
            }

            context.CodeWriter.Write(");");
            context.CodeWriter.WriteLine();

            if (typeInferenceCaptureScope.HasValue)
            {
                foreach (var localToClear in parameters.Select(p => p.Source).OfType<TypeInferenceCapturedVariable>())
                {
                    // Ensure we're not interfering with the GC lifetime of these captured values
                    // We don't need the values any longer (code in closures only uses its types for compile-time inference)
                    context.CodeWriter.Write(localToClear.VariableName);
                    context.CodeWriter.WriteLine(" = default;");
                }

                typeInferenceCaptureScope.Value.Dispose();
            }
        }
    }

    public override void WriteComponentTypeInferenceMethod(CodeRenderingContext context, ComponentTypeInferenceMethodIntermediateNode node)
    {
        WriteComponentTypeInferenceMethod(context, node, returnComponentType: false, allowNameof: true, mapComponentStartTag: true);
    }

    private void WriteTypeInferenceMethodParameterInnards(CodeRenderingContext context, TypeInferenceMethodParameter parameter)
    {
        switch (parameter.Source)
        {
            case ComponentAttributeIntermediateNode attribute:
                // Don't type check generics, since we can't actually write the type name.
                // The type checking will happen anyway since we defined a method and we're generating
                // a call to it.
                WriteComponentAttributeInnards(context, attribute, canTypeCheck: false);
                break;
            case SplatIntermediateNode splat:
                WriteSplatInnards(context, splat, canTypeCheck: false);
                break;
            case ComponentChildContentIntermediateNode childNode:
                WriteComponentChildContentInnards(context, childNode);
                break;
            case SetKeyIntermediateNode setKey:
                WriteSetKeyInnards(context, setKey);
                break;
            case ReferenceCaptureIntermediateNode capture:
                WriteReferenceCaptureInnards(context, capture, shouldTypeCheck: false);
                break;
            case CascadingGenericTypeParameter syntheticArg:
                // The value should be populated before we use it, because we emit code for creating ancestors
                // first, and that's where it's populated. However if this goes wrong somehow, we don't want to
                // throw, so use a fallback
                if (syntheticArg.ValueExpression is IWriteableValue writeableValue)
                {
                    writeableValue.WriteTo(context.CodeWriter);
                }
                else
                {
                    var valueExpression = syntheticArg.ValueExpression as string ?? "default";
                    context.CodeWriter.Write(valueExpression);

                    if (!context.Options.SuppressNullabilityEnforcement && IsDefaultExpression(valueExpression))
                    {
                        context.CodeWriter.Write("!");
                    }
                }

                break;

            case TypeInferenceCapturedVariable capturedVariable:
                context.CodeWriter.Write(capturedVariable.VariableName);
                break;
            case RenderModeIntermediateNode renderMode:
                WriteCSharpCode(context, new CSharpCodeIntermediateNode() { Source = renderMode.Source, Children = { renderMode.Children[0] } });
                break;
            default:
                throw new InvalidOperationException($"Not implemented: type inference method parameter from source {parameter.Source}");
        }
    }

    public override void WriteComponentAttribute(CodeRenderingContext context, ComponentAttributeIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (node.IsDesignTimePropertyAccessHelper)
        {
            WriteDesignTimePropertyAccessor(context, node);
            return;
        }

        // 强类型发射（ISSUE-136）：元素上的事件属性写成 __e0.OnClick = ...，
        // 组件参数写成 __c0.Size = ...，两者都绕开运行时的名字查找与反射。
        if (TryWriteTypedElementEvent(context, node) || TryWriteTypedComponentParameter(context, node))
        {
            return;
        }

        var addAttributeMethod = node.AddAttributeMethodName ?? GetAddComponentParameterMethodName(context);

        // _builder.AddComponentParameter(1, nameof(Component.Property), 42);
        context.CodeWriter.Write(BuilderVariableName);
        context.CodeWriter.Write(".");
        context.CodeWriter.Write(addAttributeMethod);
        context.CodeWriter.Write("(");
        context.CodeWriter.WriteIntegerLiteral(_sourceSequence++);
        context.CodeWriter.Write(", ");

        WriteComponentAttributeName(context, node);
        context.CodeWriter.Write(", ");

        if (addAttributeMethod == ComponentsApi.RenderTreeBuilder.AddAttribute)
        {
            context.CodeWriter.Write("(object)(");
        }

        WriteComponentAttributeInnards(context, node, canTypeCheck: true);

        if (addAttributeMethod == ComponentsApi.RenderTreeBuilder.AddAttribute)
        {
            context.CodeWriter.Write(")");
        }

        context.CodeWriter.Write(");");
        context.CodeWriter.WriteLine();
    }

    /// <summary>
    /// Emits an element event handler as a direct assignment to its strongly-typed slot:
    /// <c>__e0.OnClick = RenderTreeBuilder.ToHandler(EventCallback.Factory.Create&lt;T&gt;(this, ...));</c>
    ///
    /// <para>The helper returns <c>null</c> for a callback with no delegate, preserving the old
    /// behaviour where such a callback left the slot untouched.</para>
    /// </summary>
    private bool TryWriteTypedElementEvent(CodeRenderingContext context, ComponentAttributeIntermediateNode node)
    {
        if (!_currentElement.IsTyped)
        {
            return false;
        }

        if (MikoElements.GetSlot(_currentElement.TagName, node.AttributeName) is not { } slot ||
            slot.Kind != MikoElements.SlotKind.EventHandler)
        {
            return false;
        }

        var writer = context.CodeWriter;
        writer.Write(_currentElement.VariableName);
        writer.Write(".");
        writer.Write(slot.PropertyName);
        writer.Write(" = global::");
        writer.Write(ComponentsApi.RenderTreeBuilder.FullTypeName);
        writer.Write(".");
        writer.Write(ComponentsApi.RenderTreeBuilder.ToHandler);
        writer.Write("(");

        WriteComponentAttributeInnards(context, node, canTypeCheck: true);

        writer.Write(");");
        writer.WriteLine();
        return true;
    }

    /// <summary>
    /// Emits a component parameter as a direct property assignment: <c>__c0.Size = "small";</c>
    ///
    /// <para>Replaces <c>AddComponentParameter</c>'s per-parameter
    /// <c>GetType().GetProperty(name)</c> + <c>SetValue</c> (uncached, and boxing for value-typed
    /// parameters). Requires a bound, strongly-typed attribute so the property name is known;
    /// weakly-typed and unbound attributes keep the reflection path.</para>
    /// </summary>
    private bool TryWriteTypedComponentParameter(CodeRenderingContext context, ComponentAttributeIntermediateNode node)
    {
        if (_currentComponent is null)
        {
            return false;
        }

        // 属性名必须来自绑定描述符；弱类型属性（如 splat 合成、任意名字的 HTML 属性）
        // 在编译期没有对应的 CLR 属性可写，仍交给反射路径。
        if (node.BoundAttribute is null || node.BoundAttribute.IsWeaklyTyped)
        {
            return false;
        }

        var propertyName = node.BoundAttribute.PropertyName;
        if (string.IsNullOrEmpty(propertyName))
        {
            return false;
        }

        var writer = context.CodeWriter;
        writer.Write(_currentComponent);
        writer.Write(".");
        writer.Write(propertyName);
        writer.Write(" = ");

        WriteComponentAttributeInnards(context, node, canTypeCheck: true);

        writer.Write(";");
        writer.WriteLine();
        return true;
    }

    private static void WriteDesignTimePropertyAccessor(CodeRenderingContext context, ComponentAttributeIntermediateNode attribute)
    {
        // These attributes don't really exist in the emitted code, but have a representation in the razor document.
        // We emit a small piece of empty code that is elided by the compiler, so that the IDE has something to reference
        // for Find All References etc.
        Debug.Assert(attribute.BoundAttribute?.ContainingType is not null);
        context.CodeWriter.Write(" _ = ");
        WriteComponentAttributeName(context, attribute);
        context.CodeWriter.WriteLine(";");
    }

    private void WriteComponentAttributeInnards(CodeRenderingContext context, ComponentAttributeIntermediateNode node, bool canTypeCheck)
    {
        if (node.Children.Count > 1)
        {
            Debug.Assert(node.HasDiagnostics, "We should have reported an error for mixed content.");
            // We render the children anyway, so tooling works.
        }

        if (node.AttributeStructure == AttributeStructure.Minimized)
        {
            // Minimized attributes always map to 'true'
            context.CodeWriter.Write("true");
        }
        else if (node.Children.Count == 1 && node.Children[0] is HtmlContentIntermediateNode htmlNode)
        {
            // This is how string attributes are lowered by default, a single HTML node with a single HTML token.
            var content = string.Join(string.Empty, GetHtmlTokens(htmlNode).Select(t => t.Content));
            context.CodeWriter.WriteStringLiteral(content);
        }
        else
        {
            // See comments in ComponentDesignTimeNodeWriter for a description of the cases that are possible.
            var tokens = GetCSharpTokens(node);
            if ((node.BoundAttribute?.IsDelegateProperty() ?? false) ||
                (node.BoundAttribute?.IsChildContentProperty() ?? false))
            {
                if (canTypeCheck)
                {
                    context.CodeWriter.Write("(");
                    WriteGloballyQualifiedTypeName(context, node);
                    context.CodeWriter.Write(")");
                    context.CodeWriter.Write("(");
                }

                WriteCSharpTokens(context, tokens);

                if (canTypeCheck)
                {
                    context.CodeWriter.Write(")");
                }
            }
            else if (node.BoundAttribute?.IsEventCallbackProperty() ?? false)
            {
                var explicitType = node.HasExplicitTypeName;
                var isInferred = node.IsOpenGeneric;
                if (canTypeCheck && NeedsTypeCheck(node))
                {
                    context.CodeWriter.Write(ComponentsApi.RuntimeHelpers.TypeCheck);
                    context.CodeWriter.Write("<");
                    QualifyEventCallback(context.CodeWriter, node.TypeName, explicitType);
                    context.CodeWriter.Write(">");
                    context.CodeWriter.Write("(");
                }

                // Microsoft.AspNetCore.Components.EventCallback.Factory.Create(this, ...) OR
                // Microsoft.AspNetCore.Components.EventCallback.Factory.Create<T>(this, ...)

                context.CodeWriter.Write("global::");
                context.CodeWriter.Write(ComponentsApi.EventCallback.FactoryAccessor);
                context.CodeWriter.Write(".");
                context.CodeWriter.Write(ComponentsApi.EventCallbackFactory.CreateMethod);

                if (isInferred != true && node.TryParseEventCallbackTypeArgument(out ReadOnlyMemory<char> argument))
                {
                    context.CodeWriter.Write("<");
                    if (explicitType)
                    {
                        context.CodeWriter.Write(argument);
                    }
                    else
                    {
                        TypeNameHelper.WriteGloballyQualifiedName(context.CodeWriter, argument);
                    }
                    context.CodeWriter.Write(">");
                }

                context.CodeWriter.Write("(");
                context.CodeWriter.Write("this");
                context.CodeWriter.Write(", ");

                WriteCSharpTokens(context, tokens);

                context.CodeWriter.Write(")");

                if (canTypeCheck && NeedsTypeCheck(node))
                {
                    context.CodeWriter.Write(")");
                }
            }
            else
            {
                if (canTypeCheck && NeedsTypeCheck(node))
                {
                    context.CodeWriter.Write(ComponentsApi.RuntimeHelpers.TypeCheck);
                    context.CodeWriter.Write("<");
                    WriteGloballyQualifiedTypeName(context, node);
                    context.CodeWriter.Write(">");
                    context.CodeWriter.Write("(");
                }

                WriteCSharpTokens(context, tokens);

                if (canTypeCheck && NeedsTypeCheck(node))
                {
                    context.CodeWriter.Write(")");
                }

            }

            static void QualifyEventCallback(CodeWriter codeWriter, string typeName, bool? explicitType)
            {
                if (ComponentAttributeIntermediateNode.TryGetEventCallbackArgument(typeName.AsMemory(), out var argument))
                {
                    codeWriter.Write("global::");
                    codeWriter.Write(ComponentsApi.EventCallback.FullTypeName);
                    codeWriter.Write("<");
                    if (explicitType == true)
                    {
                        codeWriter.Write(argument);
                    }
                    else
                    {
                        TypeNameHelper.WriteGloballyQualifiedName(codeWriter, argument);
                    }
                    codeWriter.Write(">");
                }
                else
                {
                    TypeNameHelper.WriteGloballyQualifiedName(codeWriter, typeName);
                }
            }
        }

        static bool NeedsTypeCheck(ComponentAttributeIntermediateNode n)
        {
            return n.BoundAttribute != null && !n.BoundAttribute.IsWeaklyTyped;
        }
    }

    private static ImmutableArray<HtmlIntermediateToken> GetHtmlTokens(IntermediateNode node)
    {
        // We generally expect all children to be HTML, this is here just in case.
        return node.FindDescendantNodes<HtmlIntermediateToken>();
    }

    private static ImmutableArray<CSharpIntermediateToken> GetCSharpTokens(IntermediateNode node)
    {
        // We generally expect all children to be CSharp, this is here just in case.
        return node.FindDescendantNodes<CSharpIntermediateToken>();
    }

    public override void WriteComponentChildContent(CodeRenderingContext context, ComponentChildContentIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        // 强类型发射（ISSUE-136）：ChildContent 也是普通参数，直接赋值即可，
        // 顺带省掉此前对 AddAttribute 路径的 (object) 装箱。
        // 注意 ChildContent 的 lambda 里会渲染子元素/子组件，进而改写 _currentElement /
        // _currentComponent；故先把赋值目标写出来，渲染完成后再恢复。
        if (_currentComponent is not null)
        {
            var componentVariable = _currentComponent;
            var writer = context.CodeWriter;
            writer.Write(componentVariable);
            writer.Write(".");
            writer.Write(node.AttributeName);
            writer.Write(" = (");
            WriteGloballyQualifiedTypeName(context, node);
            writer.Write(")(");

            var previousElement = _currentElement;
            var previousComponent = _currentComponent;
            _currentElement = default;
            _currentComponent = null;
            try
            {
                WriteComponentChildContentInnards(context, node);
            }
            finally
            {
                _currentElement = previousElement;
                _currentComponent = previousComponent;
            }

            writer.Write(");");
            writer.WriteLine();
            return;
        }

        // Writes something like:
        //
        // _builder.AddComponentParameter(1, "ChildContent", (RenderFragment)((__builder73) => { ... }));
        // OR
        // _builder.AddComponentParameter(1, "ChildContent", (RenderFragment<Person>)((person) => (__builder73) => { ... }));
        BeginWriteAttribute(context, node.AttributeName);
        context.CodeWriter.WriteParameterSeparator();
        context.CodeWriter.Write("(");
        WriteGloballyQualifiedTypeName(context, node);
        context.CodeWriter.Write(")(");

        WriteComponentChildContentInnards(context, node);

        context.CodeWriter.Write(")");
        context.CodeWriter.WriteEndMethodInvocation();
    }

    private void WriteComponentChildContentInnards(CodeRenderingContext context, ComponentChildContentIntermediateNode node)
    {
        // Writes something like:
        //
        // ((__builder73) => { ... })
        // OR
        // ((person) => (__builder73) => { })
        var parameterName = node.IsParameterized ? node.ParameterName : null;

        using (ScopeStack.OpenComponentScope(context, parameterName))
        {
            foreach (var child in node.Children)
            {
                context.RenderNode(child);
            }
        }
    }

    public override void WriteComponentTypeArgument(CodeRenderingContext context, ComponentTypeArgumentIntermediateNode node)
    {
        WriteCSharpToken(context, node.Value);
    }

    public override void WriteTemplate(CodeRenderingContext context, TemplateIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        // Looks like:
        //
        // (__builder73) => { ... }
        using (ScopeStack.OpenTemplateScope(context))
        {
            context.RenderChildren(node);
        }
    }

    public override void WriteSetKey(CodeRenderingContext context, SetKeyIntermediateNode node)
    {
        // Looks like:
        //
        // _builder.SetKey(_keyValue);

        var codeWriter = context.CodeWriter;

        codeWriter.WriteStartMethodInvocation($"{BuilderVariableName}.{ComponentsApi.RenderTreeBuilder.SetKey}");
        WriteSetKeyInnards(context, node);
        codeWriter.WriteEndMethodInvocation();
    }

    private void WriteSetKeyInnards(CodeRenderingContext context, SetKeyIntermediateNode node)
    {
        WriteCSharpCode(context, new CSharpCodeIntermediateNode
        {
            Source = node.Source,
            Children = { node.KeyValueToken }
        });
    }

    public override void WriteSplat(CodeRenderingContext context, SplatIntermediateNode node)
    {
        // Looks like:
        //
        // _builder.AddMultipleAttributes(2, ...);
        context.CodeWriter.WriteStartMethodInvocation($"{BuilderVariableName}.{ComponentsApi.RenderTreeBuilder.AddMultipleAttributes}");
        context.CodeWriter.WriteIntegerLiteral(_sourceSequence++);
        context.CodeWriter.WriteParameterSeparator();

        WriteSplatInnards(context, node, canTypeCheck: true);

        context.CodeWriter.WriteEndMethodInvocation();
    }

    private static void WriteSplatInnards(CodeRenderingContext context, SplatIntermediateNode node, bool canTypeCheck)
    {
        var writer = context.CodeWriter;

        if (canTypeCheck)
        {
            writer.Write($"{ComponentsApi.RuntimeHelpers.TypeCheck}<{ComponentsApi.AddMultipleAttributesTypeFullName}>(");
        }

        using var tokens = new PooledArrayBuilder<CSharpIntermediateToken>();
        node.CollectDescendantNodes(ref tokens.AsRef());

        WriteCSharpTokens(context, in tokens);

        if (canTypeCheck)
        {
            writer.Write(")");
        }
    }

    public sealed override void WriteFormName(CodeRenderingContext context, FormNameIntermediateNode node)
    {
        if (node.Children.Count > 1)
        {
            Debug.Assert(node.HasDiagnostics, "We should have reported an error for mixed content.");
        }

        // string __formName = expression;
        context.CodeWriter.Write($"string {FormNameVariableName} = {ComponentsApi.RuntimeHelpers.TypeCheck}<string>(");
        WriteAttributeValue(context, node.FindDescendantNodes<IntermediateToken>());
        context.CodeWriter.WriteLine(");");
    }

    public override void WriteReferenceCapture(CodeRenderingContext context, ReferenceCaptureIntermediateNode node)
    {
        // Looks like:
        //
        // _builder.AddComponentReferenceCapture(2, (__value) = { _field = (MyComponent)__value; });
        // OR
        // _builder.AddElementReferenceCapture(2, (__value) = { _field = (ElementReference)__value; });
        var codeWriter = context.CodeWriter;

        var methodName = node.IsComponentCapture
            ? ComponentsApi.RenderTreeBuilder.AddComponentReferenceCapture
            : ComponentsApi.RenderTreeBuilder.AddElementReferenceCapture;
        codeWriter
            .WriteStartMethodInvocation($"{BuilderVariableName}.{methodName}")
            .WriteIntegerLiteral(_sourceSequence++)
            .WriteParameterSeparator();

        WriteReferenceCaptureInnards(context, node, shouldTypeCheck: true);

        codeWriter.WriteEndMethodInvocation();
    }

    protected override void WriteReferenceCaptureInnards(CodeRenderingContext context, ReferenceCaptureIntermediateNode node, bool shouldTypeCheck)
    {
        // Looks like:
        //
        // (__value) = { _field = (MyComponent)__value; }
        // OR
        // (__value) = { _field = (ElementRef)__value; }
        const string RefCaptureParamName = "__value";
        const string DefaultAssignment = $" = {RefCaptureParamName};";

        using (context.CodeWriter.BuildLambda(RefCaptureParamName))
        {
            shouldTypeCheck = shouldTypeCheck && node.IsComponentCapture;

            var assignmentToken = shouldTypeCheck
                ? IntermediateNodeFactory.CSharpToken($" = ({node.FieldTypeName}){RefCaptureParamName};")
                : IntermediateNodeFactory.CSharpToken(DefaultAssignment);

            WriteCSharpCode(context, new CSharpCodeIntermediateNode
            {
                Source = node.Source,
                Children = { node.IdentifierToken, assignmentToken }
            });
        }
    }

    public override void WriteRenderMode(CodeRenderingContext context, RenderModeIntermediateNode node)
    {
        // Looks like:
        // global::Microsoft.AspNetCore.Components.IComponentRenderMode __renderMode0 = expression;
        context.CodeWriter.Write($"global::{ComponentsApi.IComponentRenderMode.FullTypeName} {RenderModeVariableName} = ");

        WriteCSharpCode(context, new CSharpCodeIntermediateNode
        {
            Source = node.Source,
            Children = { node.Children[0] }
        });

        context.CodeWriter.WriteLine(";");
    }

    private void WriteAttribute(CodeRenderingContext context, string key, ImmutableArray<IntermediateToken> value)
    {
        // 强类型路径（ISSUE-136）：已知标签上的已知属性直接写到元素实例上，
        // 不再经运行时的名字 switch。
        if (TryWriteTypedElementAttribute(context, key, value))
        {
            return;
        }

        BeginWriteAttribute(context, key);

        if (value.Length > 0)
        {
            context.CodeWriter.WriteParameterSeparator();
            WriteAttributeValue(context, value);
        }
        else if (!context.Options.OmitMinimizedComponentAttributeValues)
        {
            // In version 5+, there's no need to supply a value for a minimized attribute.
            // But for older language versions, minimized attributes were represented as "true".
            context.CodeWriter.WriteParameterSeparator();
            context.CodeWriter.WriteBooleanLiteral(true);
        }

        context.CodeWriter.WriteEndMethodInvocation();
    }

    /// <summary>
    /// Emits an element attribute as a direct assignment on the open element's local, e.g.
    /// <c>__e0.Class = "x";</c> or <c>__e0.OnClick = RenderTreeBuilder.ToHandler(...);</c>.
    /// Returns false when there is no open typed element or the attribute has no known slot,
    /// leaving the caller to use the legacy string-based path — which, as before, silently
    /// ignores attributes the runtime never supported (<c>role</c>, <c>aria-*</c>, …).
    /// </summary>
    private bool TryWriteTypedElementAttribute(
        CodeRenderingContext context, string key, ImmutableArray<IntermediateToken> value)
    {
        if (!_currentElement.IsTyped)
        {
            return false;
        }

        if (MikoElements.GetSlot(_currentElement.TagName, key) is not { } slot)
        {
            return false;
        }

        var writer = context.CodeWriter;

        switch (slot.Kind)
        {
            case MikoElements.SlotKind.EventHandler:
                // 元素上的 @onclick 被 ComponentEventHandlerLoweringPass 降级成
                // HtmlAttributeIntermediateNode（父节点是 MarkupElementIntermediateNode），
                // 其值已经是 EventCallback.Factory.Create<T>(this, handler)。
                // 这里把它包进 ToHandler 并直接写到强类型槽位上。
                //
                // 无值（罕见的错误场景）时不发射：保持旧路径「没有委托就不装处理器」的语义。
                if (value.Length == 0)
                {
                    return false;
                }

                writer.Write(_currentElement.VariableName);
                writer.Write(".");
                writer.Write(slot.PropertyName);
                writer.Write(" = global::");
                writer.Write(ComponentsApi.RenderTreeBuilder.FullTypeName);
                writer.Write(".");
                writer.Write(ComponentsApi.RenderTreeBuilder.ToHandler);
                writer.Write("(");
                WriteAttributeValue(context, value);
                writer.WriteLine(");");
                return true;

            case MikoElements.SlotKind.Helper:
                // RenderTreeBuilder.SetInputValue(__e0, value);
                writer.Write(slot.Helper);
                writer.Write("(");
                writer.Write(_currentElement.VariableName);
                writer.Write(", ");
                WriteTypedAttributeValue(context, slot, value);
                writer.WriteLine(");");
                return true;

            case MikoElements.SlotKind.Converted:
                // __e0.Type = RenderTreeBuilder.ParseInputType(value);
                writer.Write(_currentElement.VariableName);
                writer.Write(".");
                writer.Write(slot.PropertyName);
                writer.Write(" = ");
                // 字面量（type="checkbox"、autoplay="true"）在编译期即可折叠成枚举/布尔常量，
                // 省掉运行时的 ToLowerInvariant + switch 与字符串比较。
                if (TryGetLiteralAttributeValue(value) is { } literal &&
                    MikoElements.TryFoldConvertedLiteral(slot, literal, out var folded))
                {
                    writer.Write(folded);
                    writer.WriteLine(";");
                    return true;
                }

                writer.Write(slot.Converter);
                writer.Write("(");
                WriteTypedAttributeValue(context, slot, value);
                writer.WriteLine(");");
                return true;

            default:
                // __e0.Class = value;
                writer.Write(_currentElement.VariableName);
                writer.Write(".");
                writer.Write(slot.PropertyName);
                writer.Write(" = ");
                WriteTypedAttributeValue(context, slot, value);
                writer.WriteLine(";");
                return true;
        }
    }

    /// <summary>
    /// The attribute's value when it is a single HTML literal (<c>type="checkbox"</c>), else null.
    /// A minimized attribute yields <c>"true"</c>, matching the runtime's HTML boolean rules.
    /// </summary>
    private static string? TryGetLiteralAttributeValue(ImmutableArray<IntermediateToken> value)
    {
        if (value.Length == 0)
        {
            return "true";
        }

        foreach (var token in value)
        {
            if (token is not HtmlIntermediateToken)
            {
                return null;
            }
        }

        return value.Length == 1
            ? value[0].Content
            : string.Concat(value.Select(static t => t.Content));
    }

    /// <summary>
    /// Writes the value expression for a typed slot. A minimized attribute
    /// (<c>&lt;video autoplay&gt;</c>) has no value tokens and means "present", which the
    /// runtime's HTML boolean rules represent as the string <c>"true"</c>.
    /// </summary>
    private void WriteTypedAttributeValue(
        CodeRenderingContext context, in MikoElements.Slot slot, ImmutableArray<IntermediateToken> value)
    {
        if (value.Length > 0)
        {
            WriteAttributeValue(context, value);
            return;
        }

        // 无值属性：布尔转换器接受字符串，其余（含 class="" 这类空串）写空字面量。
        if (MikoElements.IsBooleanConverted(slot))
        {
            context.CodeWriter.WriteStringLiteral("true");
        }
        else
        {
            context.CodeWriter.WriteStringLiteral(string.Empty);
        }
    }

    private void WriteAttribute(CodeRenderingContext context, IntermediateNode nameExpression, ImmutableArray<IntermediateToken> value)
    {
        BeginWriteAttribute(context, nameExpression);

        if (value.Length > 0)
        {
            context.CodeWriter.WriteParameterSeparator();
            WriteAttributeValue(context, value);
        }

        context.CodeWriter.WriteEndMethodInvocation();
    }

    protected override void BeginWriteAttribute(CodeRenderingContext context, string key)
    {
        context.CodeWriter
            .WriteStartMethodInvocation($"{BuilderVariableName}.{ComponentsApi.RenderTreeBuilder.AddAttribute}")
            .WriteIntegerLiteral(_sourceSequence++)
            .WriteParameterSeparator()
            .WriteStringLiteral(key);
    }

    protected override void BeginWriteAttribute(CodeRenderingContext context, IntermediateNode nameExpression)
    {
        context.CodeWriter.WriteStartMethodInvocation($"{BuilderVariableName}.{ComponentsApi.RenderTreeBuilder.AddAttribute}");
        context.CodeWriter.WriteIntegerLiteral(_sourceSequence++);
        context.CodeWriter.WriteParameterSeparator();

        var tokens = GetCSharpTokens(nameExpression);
        for (var i = 0; i < tokens.Length; i++)
        {
            WriteCSharpToken(context, tokens[i]);
        }
    }

    private static string GetHtmlContent(HtmlContentIntermediateNode node)
    {
        using var _ = StringBuilderPool.GetPooledObject(out var builder);

        var htmlTokens = node.Children.OfType<HtmlIntermediateToken>();

        foreach (var child in node.Children)
        {
            if (child is HtmlIntermediateToken htmlToken)
            {
                builder.Append(htmlToken.Content);
            }
        }

        return builder.ToString();
    }

    // There are a few cases here, we need to handle:
    // - Pure HTML
    // - Pure CSharp
    // - Mixed HTML and CSharp
    //
    // Only the mixed case is complicated, we want to turn it into code that will concatenate
    // the values into a string at runtime.

    private static void WriteAttributeValue(CodeRenderingContext context, ImmutableArray<IntermediateToken> tokens)
    {
        if (tokens.Length == 0)
        {
            return;
        }

        var writer = context.CodeWriter;
        var hasHtml = false;
        var hasCSharp = false;

        foreach (var token in tokens)
        {
            if (token is CSharpIntermediateToken)
            {
                hasCSharp |= true;
            }
            else
            {
                Debug.Assert(token is HtmlIntermediateToken);
                hasHtml |= true;
            }
        }

        if (!hasCSharp && !hasHtml)
        {
            Assumed.Unreachable("Found attribute whose value is neither HTML nor CSharp");
        }

        // If we only have C# tokens, we write them out directly.
        if (hasCSharp && !hasHtml)
        {
            foreach (var token in tokens)
            {
                WriteCSharpToken(context, (CSharpIntermediateToken)token);
            }

            return;
        }

        // If we only have HTML tokens, we write out a single string literal.
        if (hasHtml && !hasCSharp)
        {
            using var _ = StringBuilderPool.GetPooledObject(out var builder);

            foreach (var token in tokens)
            {
                Debug.Assert(token is HtmlIntermediateToken);
                builder.Append(token.Content);
            }

            writer.WriteStringLiteral(builder.ToString());
            return;
        }

        // If it's a C# expression, we have to wrap it in parentheses, otherwise things like ternary
        // expressions don't compose with concatenation. However, this is a little complicated
        // because C# tokens themselves aren't guaranteed to be distinct expressions. We want
        // to treat all contiguous C# tokens as a single expression.
        var insideCSharp = false;
        var first = true;
        foreach (var token in tokens)
        {
            if (token is CSharpIntermediateToken csharpToken)
            {
                if (!insideCSharp)
                {
                    // Transition to a new C# expression
                    if (!first)
                    {
                        writer.Write(" + ");
                    }

                    writer.Write("(");
                    insideCSharp = true;
                }

                WriteCSharpToken(context, csharpToken);
            }
            else
            {
                if (insideCSharp)
                {
                    // Transition to HTML, close out the C# expression
                    writer.Write(")");
                    insideCSharp = false;
                }

                if (!first)
                {
                    writer.Write(" + ");
                }

                writer.WriteStringLiteral(token.Content);
            }

            if (first)
            {
                first = false;
            }
        }

        if (insideCSharp)
        {
            writer.Write(")");
        }
    }

    private static void WriteCSharpTokens(CodeRenderingContext context, ImmutableArray<CSharpIntermediateToken> tokens)
    {
        foreach (var token in tokens)
        {
            WriteCSharpToken(context, token);
        }
    }

    private static void WriteCSharpTokens(CodeRenderingContext context, ref readonly PooledArrayBuilder<CSharpIntermediateToken> tokens)
    {
        foreach (var token in tokens)
        {
            WriteCSharpToken(context, token);
        }
    }

    private static void WriteCSharpToken(CodeRenderingContext context, CSharpIntermediateToken token)
    {
        if (token.Source?.FilePath == null)
        {
            context.CodeWriter.Write(token.Content);
            return;
        }

        using (context.BuildEnhancedLinePragma(token.Source))
        {
            context.CodeWriter.Write(token.Content);
        }
    }
}
