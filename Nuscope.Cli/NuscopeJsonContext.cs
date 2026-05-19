using System.Text.Json.Serialization;

namespace Nuscope.Cli;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = [typeof(JsonStringEnumConverter<SymbolKind>), typeof(JsonStringEnumConverter<TypeKind>)])]
[JsonSerializable(typeof(InspectionReport))]
internal sealed partial class NuscopeJsonContext : JsonSerializerContext;
