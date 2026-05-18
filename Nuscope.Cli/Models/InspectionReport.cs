namespace Nuscope.Cli;

internal sealed record InspectionReport(string InputPath, IReadOnlyList<AssemblyReport> Assemblies);
