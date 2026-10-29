// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// A component's <c>.razor.css</c> file (ISSUE-161): its path, for diagnostics, and its text.
/// Miko compiles it into the component instead of bundling it for a browser.
/// </summary>
public sealed record ScopedCssSource(string FilePath, string Text);
