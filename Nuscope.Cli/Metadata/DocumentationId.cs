using System.Reflection.Metadata;

namespace Nuscope.Cli;

internal static class DocumentationId
{
    private static readonly DocumentationSignatureTypeProvider Provider = new();

    public static string ForType(string typeName) => $"T:{typeName}";

    public static string ForField(string typeName, FieldDefinition field, MetadataReader reader) =>
        $"F:{typeName}.{reader.GetString(field.Name)}";

    public static string ForProperty(string typeName, PropertyDefinition property, MetadataReader reader)
    {
        var signature = property.DecodeSignature(Provider, null);
        var parameters = signature.ParameterTypes.Length == 0
            ? string.Empty
            : $"({string.Join(',', signature.ParameterTypes)})";
        return $"P:{typeName}.{reader.GetString(property.Name)}{parameters}";
    }

    public static string ForEvent(string typeName, EventDefinition eventDefinition, MetadataReader reader) =>
        $"E:{typeName}.{reader.GetString(eventDefinition.Name)}";

    public static string ForMethod(string typeName, MethodDefinition method, MetadataReader reader)
    {
        var methodName = reader.GetString(method.Name) switch
        {
            ".ctor" => "#ctor",
            ".cctor" => "#cctor",
            var name => name
        };

        var genericArity = method.GetGenericParameters().Count;
        if (genericArity > 0)
        {
            methodName += $"``{genericArity}";
        }

        var signature = method.DecodeSignature(Provider, null);
        var parameters = signature.ParameterTypes.Length == 0
            ? string.Empty
            : $"({string.Join(',', signature.ParameterTypes)})";

        return $"M:{typeName}.{methodName}{parameters}";
    }
}
