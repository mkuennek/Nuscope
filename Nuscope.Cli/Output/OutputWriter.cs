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
    /// Groups text output by namespace and declaring type so related symbols are shown together.
    /// </summary>
    private static void WriteGroupedSymbols(TextWriter writer, IReadOnlyList<SymbolInfo> symbols)
    {
        foreach (var namespaceGroup in symbols
            .GroupBy(GetNamespace)
            .OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            writer.WriteLine($"  namespace {namespaceGroup.Key}");

            foreach (var typeGroup in namespaceGroup
                .GroupBy(GetTypeName)
                .OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                var typeSymbol = typeGroup.FirstOrDefault(s => s.Kind == SymbolKind.Type);
                if (typeSymbol is not null)
                {
                    writer.WriteLine($"    {GetShortTypeName(typeGroup.Key)} ({typeSymbol.Classification}, {typeSymbol.Visibility})");
                    WriteDocumentation(writer, typeSymbol, "      ");
                }
                else
                {
                    writer.WriteLine($"    {GetShortTypeName(typeGroup.Key)}");
                }

                foreach (var symbol in typeGroup
                    .Where(s => s.Kind != SymbolKind.Type)
                    .OrderBy(s => GetKindOrder(s.Kind))
                    .ThenBy(s => s.Name, StringComparer.Ordinal))
                {
                    var signature = symbol.Signature is null ? symbol.Name : symbol.Signature;
                    writer.WriteLine($"      {symbol.Kind.ToString().ToLowerInvariant(),-11} {symbol.Visibility,-18} {signature}");
                    WriteDocumentation(writer, symbol, "        ");
                }
            }
        }
    }

    /// <summary>
    /// Writes normalized XML documentation comments below the symbol line when available.
    /// </summary>
    private static void WriteDocumentation(TextWriter writer, SymbolInfo symbol, string indent)
    {
        if (!string.IsNullOrWhiteSpace(symbol.Documentation))
        {
            writer.WriteLine($"{indent}/// {symbol.Documentation}");
        }
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
