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
    /// Builds a C#-like full type definition name, replacing metadata generic arity with type parameters.
    /// </summary>
    public static string GetDisplayTypeName(MetadataReader reader, TypeDefinition type)
    {
        var name = StripGenericArity(reader.GetString(type.Name));
        var genericParameters = type.GetGenericParameters()
            .Select(p => reader.GetString(reader.GetGenericParameter(p).Name))
            .ToArray();
        if (genericParameters.Length > 0)
        {
            name += $"<{string.Join(", ", genericParameters)}>";
        }

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
    /// Builds a C#-like full type reference name without metadata generic arity suffixes.
    /// </summary>
    public static string GetDisplayTypeReferenceName(MetadataReader reader, TypeReference type)
    {
        var name = StripGenericArity(reader.GetString(type.Name));
        var ns = reader.GetString(type.Namespace);
        return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
    }

    /// <summary>
    /// Removes a metadata generic arity suffix such as `1 or `2 from a type name.
    /// </summary>
    public static string StripGenericArity(string name)
    {
        var tick = name.IndexOf('`', StringComparison.Ordinal);
        return tick < 0 ? name : name[..tick];
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
