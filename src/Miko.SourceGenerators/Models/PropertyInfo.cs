using System.Collections.Generic;

namespace Miko.SourceGenerators.Models;

internal sealed class PropertyInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsValueType { get; set; }
}

internal sealed class StyleInfo
{
    public List<PropertyInfo> Properties { get; set; } = new();
}
