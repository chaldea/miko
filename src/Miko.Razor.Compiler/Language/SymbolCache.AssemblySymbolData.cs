// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

internal partial class SymbolCache
{
    public sealed partial class AssemblySymbolData(IAssemblySymbol symbol)
    {
        public bool MightContainTagHelpers { get; } = CalculateMightContainTagHelpers(symbol);

        private static bool CalculateMightContainTagHelpers(IAssemblySymbol assembly)
        {
            // In order to contain tag helpers, components, or anything else we might want to find,
            // the assembly must be a framework assembly or reference one.
            //
            // Miko's framework assembly is "Miko" (not "Microsoft.AspNetCore.*"), so it has to be
            // recognized here too — otherwise the element @bind mappings it declares (via
            // BindAttributes) are skipped and `<input @bind="_x" />` silently never lowers.
            return IsFrameworkAssembly(assembly.Name) ||
                    assembly.Modules.First().ReferencedAssemblies.Any(
                        a => IsFrameworkAssembly(a.Name));

            static bool IsFrameworkAssembly(string name)
                => name is ComponentsApi.AssemblyName ||
                   name.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal);
        }
    }
}
