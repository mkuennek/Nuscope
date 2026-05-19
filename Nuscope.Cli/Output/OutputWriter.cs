using System.Text.Json;

namespace Nuscope.Cli;

internal static class OutputWriter
{
    /// <summary>
    /// Writes the inspection report in either machine-readable JSON or grouped text form.
    /// </summary>
    public static void Write(TextWriter writer, InspectionReport report, OutputFormat format)
    {
        if (format == OutputFormat.Json)
        {
            writer.WriteLine(JsonSerializer.Serialize(report, NuscopeJsonContext.Default.InspectionReport));
            return;
        }

        writer.WriteLine(report.InputPath);
        foreach (var assembly in report.Assemblies)
        {
            writer.WriteLine();
            writer.WriteLine($"{assembly.AssemblyName} ({assembly.Path})");
            WriteGroupedSymbols(writer, assembly.Symbols);
        }
    }

    /// <summary>
    /// Groups text output by namespace and declaring type, using a C#-like declaration layout for readability.
    /// </summary>
    private static void WriteGroupedSymbols(TextWriter writer, IReadOnlyList<SymbolInfo> symbols)
    {
        foreach (var namespaceGroup in symbols
            .GroupBy(GetNamespace)
            .OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            if (namespaceGroup.Key != "<global>")
            {
                writer.WriteLine($"  namespace {namespaceGroup.Key};");
                writer.WriteLine();
            }

            var firstType = true;
            foreach (var typeGroup in namespaceGroup
                .GroupBy(GetTypeName)
                .OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                if (!firstType)
                {
                    writer.WriteLine("----------------------------------------");
                    writer.WriteLine();
                }

                firstType = false;
                var typeName = typeGroup.Key;
                var typeSymbol = typeGroup.FirstOrDefault(s => s.Kind == SymbolKind.Type);
                if (typeSymbol is not null)
                {
                    WriteDocumentation(writer, typeSymbol, "  ");
                    writer.WriteLine($"  {FormatTypeDeclaration(typeSymbol, typeName)}");
                }
                else
                {
                    writer.WriteLine($"  {GetShortTypeName(typeName)}");
                }

                writer.WriteLine("  {");

                foreach (var symbol in typeGroup
                    .Where(s => s.Kind != SymbolKind.Type)
                    .OrderBy(s => GetKindOrder(s.Kind))
                    .ThenBy(s => s.Name, StringComparer.Ordinal))
                {
                    WriteDocumentation(writer, symbol, "    ");
                    writer.WriteLine($"    {FormatMemberDeclaration(symbol, typeName)}");
                    writer.WriteLine();
                }

                writer.WriteLine("  }");
                writer.WriteLine();
            }
        }
    }

    /// <summary>
    /// Writes normalized XML documentation comments above a declaration when available.
    /// </summary>
    private static void WriteDocumentation(TextWriter writer, SymbolInfo symbol, string indent)
    {
        if (string.IsNullOrWhiteSpace(symbol.Documentation))
        {
            return;
        }

        writer.WriteLine($"{indent}/// <summary>");
        writer.WriteLine($"{indent}/// {symbol.Documentation}");
        writer.WriteLine($"{indent}/// </summary>");
    }

    /// <summary>
    /// Formats a type declaration as C#-like text while preserving metadata-derived modifiers.
    /// </summary>
    private static string FormatTypeDeclaration(SymbolInfo symbol, string typeName)
    {
        var declaration = symbol.Signature ?? $"{symbol.Visibility} {symbol.Classification} {typeName}";
        return declaration.Replace(typeName, GetShortTypeName(typeName), StringComparison.Ordinal);
    }

    /// <summary>
    /// Formats a member declaration as C#-like text using currently available metadata.
    /// </summary>
    private static string FormatMemberDeclaration(SymbolInfo symbol, string typeName)
    {
        var signature = symbol.Signature ?? symbol.Name;
        return symbol.Kind switch
        {
            SymbolKind.Constructor => $"{symbol.Visibility} {GetShortTypeName(typeName)}{GetParameterList(signature)};",
            SymbolKind.Property => $"{symbol.Visibility} {signature} {{ {FormatAccessors(symbol)} }}",
            SymbolKind.Event => $"{symbol.Visibility} event {signature};",
            _ => $"{symbol.Visibility} {signature};"
        };
    }

    /// <summary>
    /// Formats property accessors, falling back to get-only for older symbol data.
    /// </summary>
    private static string FormatAccessors(SymbolInfo symbol) =>
        symbol.Accessors is { Count: > 0 } accessors ? string.Join(' ', accessors) : "get;";

    /// <summary>
    /// Extracts the parameter list from a formatted method signature.
    /// </summary>
    private static string GetParameterList(string signature)
    {
        var start = signature.IndexOf('(');
        return start < 0 ? "()" : signature[start..];
    }

    /// <summary>
    /// Derives the namespace bucket from the symbol's declaring type or own type name.
    /// </summary>
    private static string GetNamespace(SymbolInfo symbol)
    {
        var typeName = GetTypeName(symbol);
        var index = typeName.LastIndexOf('.');
        return index < 0 ? "<global>" : typeName[..index];
    }

    /// <summary>
    /// Finds the full type name that should own the symbol in grouped text output.
    /// </summary>
    private static string GetTypeName(SymbolInfo symbol)
    {
        if (symbol.Kind == SymbolKind.Type)
        {
            return symbol.Name;
        }

        if (!string.IsNullOrWhiteSpace(symbol.DeclaringType))
        {
            return symbol.DeclaringType;
        }

        var index = symbol.Name.LastIndexOf('.');
        return index < 0 ? symbol.Name : symbol.Name[..index];
    }

    /// <summary>
    /// Removes the namespace prefix for display inside a namespace group.
    /// </summary>
    private static string GetShortTypeName(string typeName)
    {
        var index = typeName.LastIndexOf('.');
        return index < 0 ? typeName : typeName[(index + 1)..];
    }

    /// <summary>
    /// Defines the stable member ordering used by the human-readable text output.
    /// </summary>
    private static int GetKindOrder(SymbolKind kind) =>
        kind switch
        {
            SymbolKind.Constructor => 0,
            SymbolKind.Property => 1,
            SymbolKind.Method => 2,
            SymbolKind.Event => 3,
            SymbolKind.Field => 4,
            SymbolKind.Type => 5,
            _ => 6
        };
}
