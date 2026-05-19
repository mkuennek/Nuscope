using System.Collections.Immutable;
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
        return $"{visibility} {modifiers}{GetTypeKeyword(shape.Kind)} {MetadataNames.GetDisplayTypeName(reader, type)}";
    }

    /// <summary>
    /// Formats a field signature as type followed by field name.
    /// </summary>
    public string FormatField(TypeDefinition declaringType, FieldDefinition field)
    {
        var fieldType = field.DecodeSignature(_provider, CreateContext(declaringType));
        return $"{fieldType} {reader.GetString(field.Name)}";
    }

    /// <summary>
    /// Formats a property signature, including indexer parameters when present.
    /// </summary>
    public string FormatProperty(TypeDefinition declaringType, PropertyDefinition property)
    {
        var signature = property.DecodeSignature(_provider, CreateContext(declaringType));
        var parameters = signature.ParameterTypes.Length == 0
            ? string.Empty
            : $"[{string.Join(", ", signature.ParameterTypes)}]";
        return $"{signature.ReturnType} {reader.GetString(property.Name)}{parameters}";
    }

    /// <summary>
    /// Formats an event signature as event type followed by event name.
    /// </summary>
    public string FormatEvent(TypeDefinition declaringType, EventDefinition eventDefinition) =>
        $"{FormatType(eventDefinition.Type, CreateContext(declaringType))} {reader.GetString(eventDefinition.Name)}";

    /// <summary>
    /// Formats a method or constructor signature with generic parameters and parameter types.
    /// </summary>
    public string FormatMethod(TypeDefinition declaringType, MethodDefinition method)
    {
        var genericParameters = method.GetGenericParameters().Select(p => reader.GetString(reader.GetGenericParameter(p).Name)).ToArray();
        var signature = method.DecodeSignature(_provider, CreateContext(declaringType, genericParameters));
        var name = reader.GetString(method.Name);
        if (name == ".ctor" || name == ".cctor")
        {
            name = MetadataNames.StripGenericArity(reader.GetString(declaringType.Name));
        }
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
    private string FormatType(EntityHandle handle) => FormatType(handle, SignatureGenericContext.Empty);

    /// <summary>
    /// Resolves the different metadata handles that can identify a type in a signature with generic context.
    /// </summary>
    private string FormatType(EntityHandle handle, SignatureGenericContext context) =>
        handle.Kind switch
        {
            HandleKind.TypeDefinition => MetadataNames.GetDisplayTypeName(reader, reader.GetTypeDefinition((TypeDefinitionHandle)handle)),
            HandleKind.TypeReference => MetadataNames.GetDisplayTypeReferenceName(reader, reader.GetTypeReference((TypeReferenceHandle)handle)),
            HandleKind.TypeSpecification => reader.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(_provider, context),
            _ => handle.Kind.ToString()
        };

    /// <summary>
    /// Creates a generic parameter lookup for signatures declared on a type and optionally a generic method.
    /// </summary>
    private SignatureGenericContext CreateContext(TypeDefinition declaringType, string[]? methodParameters = null) =>
        new(
            declaringType.GetGenericParameters()
                .Select(p => reader.GetString(reader.GetGenericParameter(p).Name))
                .ToImmutableArray(),
            methodParameters?.ToImmutableArray() ?? []);
}
