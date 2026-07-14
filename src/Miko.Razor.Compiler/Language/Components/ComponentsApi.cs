// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace Microsoft.AspNetCore.Razor.Language.Components;

// Constants for method names used in code-generation
// Keep these in sync with the actual definitions
internal static class ComponentsApi
{
    public const string AssemblyName = "Miko";

    public const string AddMultipleAttributesTypeFullName = "global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<string, object>>";

    public static class ComponentBase
    {
        public const string Namespace = "Miko.Components";
        public const string FullTypeName = "Miko.Components.ComponentBase";
        public const string MetadataName = FullTypeName;

        public const string BuildRenderTree = nameof(BuildRenderTree);
    }

    public static class ParameterAttribute
    {
        public const string FullTypeName = "Miko.Components.ParameterAttribute";
        public const string MetadataName = FullTypeName;
    }

    public static class LayoutAttribute
    {
        public const string FullTypeName = "Miko.Components.LayoutAttribute";
    }

    public static class InjectAttribute
    {
        public const string FullTypeName = "Miko.Components.InjectAttribute";
    }

    public static class IComponent
    {
        public const string FullTypeName = "Miko.Components.IComponent";

        public const string MetadataName = FullTypeName;
    }

    public static class IDictionary
    {
        public const string MetadataName = "System.Collection.IDictionary`2";
    }

    public static class IComponentRenderMode
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Components.IComponentRenderMode";
    }

    public static class RenderModeAttribute
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Components.RenderModeAttribute";
    }

    public static class RenderFragment
    {
        public const string Namespace = "Miko.Components";
        public const string FullTypeName = "Miko.Components.RenderFragment";
        public const string MetadataName = FullTypeName;
    }

    public static class RenderFragmentOfT
    {
        public const string Namespace = "Miko.Components";
        public const string FullTypeName = "Miko.Components.RenderFragment<>";
        public const string MetadataName = "Miko.Components.RenderFragment`1";
        public const string DisplayName = "Miko.Components.RenderFragment<TValue>";
    }

    public static class RenderTreeBuilder
    {
        public const string FullTypeName = "Miko.Components.RenderTreeBuilder";

        public const string BuilderParameter = "__builder";

        public const string FormNameVariableName = "__formName";

        public const string OpenElement = nameof(OpenElement);

        public const string CloseElement = nameof(CloseElement);

        public const string OpenComponent = nameof(OpenComponent);

        public const string CloseComponent = nameof(CloseComponent);

        public const string AddMarkupContent = nameof(AddMarkupContent);

        public const string AddContent = nameof(AddContent);

        public const string AddAttribute = nameof(AddAttribute);

        public const string AddMultipleAttributes = nameof(AddMultipleAttributes);

        public const string AddComponentParameter = nameof(AddComponentParameter);

        public const string AddNamedEvent = nameof(AddNamedEvent);

        public const string AddElementReferenceCapture = nameof(AddElementReferenceCapture);

        public const string AddComponentReferenceCapture = nameof(AddComponentReferenceCapture);

        public const string Clear = nameof(Clear);

        public const string GetFrames = nameof(GetFrames);

        public const string ChildContent = nameof(ChildContent);

        public const string SetKey = nameof(SetKey);

        public const string SetUpdatesAttributeName = nameof(SetUpdatesAttributeName);

        public const string AddEventPreventDefaultAttribute = nameof(AddEventPreventDefaultAttribute);

        public const string AddEventStopPropagationAttribute = nameof(AddEventStopPropagationAttribute);

        public const string AddComponentRenderMode = nameof(AddComponentRenderMode);

        public const string RenderModeVariableName = "__renderMode";
    }

    public static class RuntimeHelpers
    {
        public const string TypeCheck = "global::Miko.Components.CompilerServices.RuntimeHelpers.TypeCheck";
        public const string CreateInferredEventCallback = "global::Miko.Components.CompilerServices.RuntimeHelpers.CreateInferredEventCallback";
        public const string CreateInferredBindSetter = "global::Miko.Components.CompilerServices.RuntimeHelpers.CreateInferredBindSetter";
        public const string InvokeSynchronousDelegate = "global::Miko.Components.CompilerServices.RuntimeHelpers.InvokeSynchronousDelegate";
        public const string InvokeAsynchronousDelegate = "global::Miko.Components.CompilerServices.RuntimeHelpers.InvokeAsynchronousDelegate";
    }

    public static class RouteAttribute
    {
        public const string FullTypeName = "Miko.Components.RouteAttribute";
    }

    public static class BindElementAttribute
    {
        public const string FullTypeName = "Miko.Components.BindElementAttribute";
    }

    public static class BindInputElementAttribute
    {
        public const string FullTypeName = "Miko.Components.BindInputElementAttribute";
    }

    public static class EventHandlerAttribute
    {
        public const string FullTypeName = "Miko.Components.EventHandlerAttribute";
    }

    public static class ElementReference
    {
        public const string FullTypeName = "Miko.Components.ElementReference";
    }

    public static class EventCallback
    {
        public const string FullTypeName = "Miko.Components.EventCallback";
        public const string MetadataName = FullTypeName;

        public const string FactoryAccessor = FullTypeName + ".Factory";
    }

    public static class EventCallbackOfT
    {
        public const string MetadataName = "Miko.Components.EventCallback`1";
        public const string DisplayName = "Miko.Components.EventCallback<TValue>";
    }

    public static class EventCallbackFactory
    {
        public const string CreateMethod = "Create";
        public const string CreateBinderMethod = "CreateBinder";
    }

    public static class BindConverter
    {
        public const string FullTypeName = "Miko.Components.BindConverter";
        public const string FormatValue = "Miko.Components.BindConverter.FormatValue";
    }

    public static class CascadingTypeParameterAttribute
    {
        public const string MetadataName = "Miko.Components.CascadingTypeParameterAttribute";
    }
}
