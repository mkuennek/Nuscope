namespace Nuscope.Cli;

internal sealed record AssemblyReport(
    string Path,
    string AssemblyName,
    string? TargetFramework,
    string? AssetKind,
    IReadOnlyList<SymbolInfo> Symbols);
