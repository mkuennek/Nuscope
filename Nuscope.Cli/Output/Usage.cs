namespace Nuscope.Cli;

internal static class Usage
{
    /// <summary>
    /// Prints the command syntax, supported filters, and default NuGet source.
    /// </summary>
    public static void Write(TextWriter writer)
    {
        writer.WriteLine("Usage:");
        writer.WriteLine("  nuscope inspect <path-to.dll|path-to.nupkg|package-id> [--format text|json] [--include-non-public] [--search term] [--kind kind]");
        writer.WriteLine("                                                    [--tfm target-framework] [--version version] [--prerelease] [--source url]");
        writer.WriteLine("  nuscope skill local|global");
        writer.WriteLine();
        writer.WriteLine("Kinds: type, method, property, field, event, constructor");
        writer.WriteLine("Skill targets: local installs to the nearest project under .agents/skills/nuscope; global installs under ~/.agents/skills/nuscope.");
        writer.WriteLine($"Default source: {InspectOptions.DefaultSource}");
    }
}
