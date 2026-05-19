namespace Nuscope.Cli;

internal sealed record SymbolInfo(
    SymbolKind Kind,
    string Name,
    string Classification,
    string Visibility,
    string? Signature,
    string? Documentation,
    string? DeclaringType,
    string AssemblyPath,
    TypeKind? TypeKind = null,
    IReadOnlyList<string>? Modifiers = null,
    IReadOnlyList<string>? Accessors = null)
{
    /// <summary>
    /// Checks whether a user search term appears in the symbol's searchable report fields.
    /// </summary>
    public bool Matches(string search) =>
        Name.Contains(search, StringComparison.OrdinalIgnoreCase)
        || Classification.Contains(search, StringComparison.OrdinalIgnoreCase)
        || Visibility.Contains(search, StringComparison.OrdinalIgnoreCase)
        || (Signature?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
        || (Documentation?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
        || (DeclaringType?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
        || (TypeKind?.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
        || (Modifiers?.Any(modifier => modifier.Contains(search, StringComparison.OrdinalIgnoreCase)) ?? false)
        || (Accessors?.Any(accessor => accessor.Contains(search, StringComparison.OrdinalIgnoreCase)) ?? false);
}
