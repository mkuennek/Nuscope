using System.Reflection;

namespace Nuscope.Cli;

internal static class SkillInstaller
{
    private const string SkillName = "nuscope";
    private const string SkillResourceName = "Nuscope.Skill.SKILL.md";

    public static async Task InstallAsync(string[] args)
    {
        if (args.Length != 1)
        {
            throw new CliException("Usage: nuscope skill local|global", 2);
        }

        var targetRoot = args[0] switch
        {
            "local" => FindLocalProjectRoot(),
            "global" => GetHomeDirectory(),
            _ => throw new CliException($"Unknown skill target '{args[0]}'. Use 'local' or 'global'.", 2)
        };

        var skillDirectory = Path.Combine(targetRoot.FullName, ".agents", "skills", SkillName);
        var skillPath = Path.Combine(skillDirectory, "SKILL.md");

        Directory.CreateDirectory(skillDirectory);
        await File.WriteAllTextAsync(skillPath, ReadSkillContent());

        await Console.Out.WriteLineAsync($"Installed nuscope skill at {skillPath}");
    }

    private static DirectoryInfo FindLocalProjectRoot()
    {
        DirectoryInfo? fallback = null;

        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")) ||
                Directory.Exists(Path.Combine(directory.FullName, ".agents")) ||
                Directory.Exists(Path.Combine(directory.FullName, ".git")) ||
                File.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory;
            }

            if (fallback is null && LooksLikeProjectDirectory(directory))
            {
                fallback = directory;
            }
        }

        return fallback ?? throw new CliException("Could not find a local project. Run this command inside a project or use 'nuscope skill global'.", 2);
    }

    private static bool LooksLikeProjectDirectory(DirectoryInfo directory) =>
        directory.EnumerateFiles("*.sln").Any() ||
        directory.EnumerateFiles("*.slnx").Any() ||
        directory.EnumerateFiles("*.csproj").Any();

    private static DirectoryInfo GetHomeDirectory()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(home))
        {
            throw new CliException("Could not determine the current user's home directory.", 2);
        }

        return new DirectoryInfo(home);
    }

    private static string ReadSkillContent()
    {
        var assembly = typeof(SkillInstaller).Assembly;
        using var stream = assembly.GetManifestResourceStream(SkillResourceName)
            ?? throw new InvalidOperationException($"Embedded skill resource '{SkillResourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
