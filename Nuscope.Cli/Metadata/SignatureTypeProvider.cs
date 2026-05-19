using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Nuscope.Cli;

internal sealed class SignatureTypeProvider : ISignatureTypeProvider<string, object?>
{
    public string GetArrayType(string elementType, ArrayShape shape) => $"{elementType}[{new string(',', shape.Rank - 1)}]";
    public string GetByReferenceType(string elementType) => $"{elementType}&";
    public string GetFunctionPointerType(MethodSignature<string> signature) => "fnptr";
    public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments)
    {
        var typeName = MetadataNames.StripGenericArity(genericType);
        return typeName == "System.ValueTuple"
            ? $"({string.Join(", ", typeArguments)})"
            : $"{typeName}<{string.Join(", ", typeArguments)}>";
    }

    public string GetGenericMethodParameter(object? genericContext, int index) =>
        genericContext is SignatureGenericContext context && index < context.MethodParameters.Length
            ? context.MethodParameters[index]
            : $"!!{index}";

    public string GetGenericTypeParameter(object? genericContext, int index) =>
        genericContext is SignatureGenericContext context && index < context.TypeParameters.Length
            ? context.TypeParameters[index]
            : $"!{index}";
    public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;
    public string GetPinnedType(string elementType) => elementType;
    public string GetPointerType(string elementType) => $"{elementType}*";
    public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString().ToLowerInvariant();

    public string GetSZArrayType(string elementType) => $"{elementType}[]";

    public string GetTypeFromDefinition(MetadataReader metadataReader, TypeDefinitionHandle handle, byte rawTypeKind) =>
        MetadataNames.GetDisplayTypeName(metadataReader, metadataReader.GetTypeDefinition(handle));

    public string GetTypeFromReference(MetadataReader metadataReader, TypeReferenceHandle handle, byte rawTypeKind) =>
        MetadataNames.GetDisplayTypeReferenceName(metadataReader, metadataReader.GetTypeReference(handle));

    public string GetTypeFromSpecification(MetadataReader metadataReader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
        metadataReader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
}
