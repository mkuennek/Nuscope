using System.Reflection.Metadata;

namespace Nuscope.Cli;

internal sealed class SignatureFormatter(MetadataReader reader)
{
    private readonly SignatureTypeProvider _provider = new();

    /// <summary>
    /// Returns the formatted base type for a type definition, or null when no base type is recorded.
    /// </summary>
    public string? FormatBaseType(TypeDefinition type)
    {
        if (type.BaseType.IsNil)
        {
            return null;
        }

        return FormatType(type.BaseType);
    }

    /// <summary>
    /// Formats a type declaration with visibility, modifiers, kind, and full metadata name.
    /// </summary>
    public string FormatTypeDefinition(TypeDefinition type, string visibility, TypeShape shape)
    {
        var modifiers = shape.Modifiers.Count == 0 ? string.Empty : $"{string.Join(' ', shape.Modifiers)} ";
        return $"{visibility} {modifiers}{GetTypeKeyword(shape.Kind)} {MetadataNames.GetTypeName(reader, type)}";
    }

    /// <summary>
    /// Formats a field signature as type followed by field name.
    /// </summary>
    public string FormatField(FieldDefinition field)
    {
        var fieldType = field.DecodeSignature(_provider, null);
        return $"{fieldType} {reader.GetString(field.Name)}";
    }

    /// <summary>
    /// Formats a property signature, including indexer parameters when present.
    /// </summary>
    public string FormatProperty(PropertyDefinition property)
    {
        var signature = property.DecodeSignature(_provider, null);
        var parameters = signature.ParameterTypes.Length == 0
            ? string.Empty
            : $"[{string.Join(", ", signature.ParameterTypes)}]";
        return $"{signature.ReturnType} {reader.GetString(property.Name)}{parameters}";
    }

    /// <summary>
    /// Formats an event signature as event type followed by event name.
    /// </summary>
    public string FormatEvent(EventDefinition eventDefinition) =>
        $"{FormatType(eventDefinition.Type)} {reader.GetString(eventDefinition.Name)}";

    /// <summary>
    /// Formats a method or constructor signature with generic parameters and parameter types.
    /// </summary>
    public string FormatMethod(MethodDefinition method)
    {
        var signature = method.DecodeSignature(_provider, null);
        var name = reader.GetString(method.Name);
        if (name == ".ctor" || name == ".cctor")
        {
            name = "ctor";
        }

        var genericParameters = method.GetGenericParameters().Select(p => reader.GetString(reader.GetGenericParameter(p).Name)).ToArray();
        if (genericParameters.Length > 0)
        {
            name += $"<{string.Join(", ", genericParameters)}>";
        }

        return $"{signature.ReturnType} {name}({string.Join(", ", signature.ParameterTypes)})";
    }

    /// <summary>
    /// Returns the C# keyword matching a structured type kind.
    /// </summary>
    private static string GetTypeKeyword(TypeKind kind) =>
        kind switch
        {
            TypeKind.Class => "class",
            TypeKind.Interface => "interface",
            TypeKind.Struct => "struct",
            TypeKind.Enum => "enum",
            TypeKind.Delegate => "delegate",
            _ => kind.ToString().ToLowerInvariant()
        };

    /// <summary>
    /// Resolves the different metadata handles that can identify a type in a signature.
    /// </summary>
    private string FormatType(EntityHandle handle) =>
        handle.Kind switch
        {
            HandleKind.TypeDefinition => MetadataNames.GetTypeName(reader, reader.GetTypeDefinition((TypeDefinitionHandle)handle)),
            HandleKind.TypeReference => MetadataNames.GetTypeReferenceName(reader, reader.GetTypeReference((TypeReferenceHandle)handle)),
            HandleKind.TypeSpecification => reader.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(_provider, null),
            _ => handle.Kind.ToString()
        };
}
