using System.Reflection.Metadata;

namespace Nuscope.Cli;

internal static class MetadataNames
{
    /// <summary>
    /// Builds a full metadata type definition name from namespace and simple name components.
    /// </summary>
    public static string GetTypeName(MetadataReader reader, TypeDefinition type)
    {
        var name = reader.GetString(type.Name);
        var ns = reader.GetString(type.Namespace);
        return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
    }

    /// <summary>
    /// Builds a full metadata type reference name from namespace and simple name components.
    /// </summary>
    public static string GetTypeReferenceName(MetadataReader reader, TypeReference type)
    {
        var name = reader.GetString(type.Name);
        var ns = reader.GetString(type.Namespace);
        return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
    }

    /// <summary>
    /// Removes the namespace prefix from a full type name for constructor display.
    /// </summary>
    public static string GetShortName(string typeName)
    {
        var dot = typeName.LastIndexOf('.');
        return dot < 0 ? typeName : typeName[(dot + 1)..];
    }
}
