namespace Nuscope.Cli.Tests;

internal static class CliAssembly
{
    private static readonly Lazy<string> Current = new(Find);

    /// <summary>
    /// Gets the built CLI assembly path that tests execute through dotnet.
    /// </summary>
    public static string Path => Current.Value;

    /// <summary>
    /// Walks up from the test output directory to find the repository root and expected CLI build output.
    /// </summary>
    private static string Find()
    {
        var configuration =
#if DEBUG
            "Debug";
#else
            "Release";
#endif

        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var projectPath = System.IO.Path.Combine(directory.FullName, "Nuscope.Cli", "Nuscope.Cli.csproj");
            if (!File.Exists(projectPath))
            {
                continue;
            }

            var assemblyPath = System.IO.Path.Combine(directory.FullName, "Nuscope.Cli", "bin", configuration, "net10.0", "nuscope.dll");
            if (File.Exists(assemblyPath))
            {
                return assemblyPath;
            }

            throw new FileNotFoundException($"The nuscope CLI has not been built at the expected path: {assemblyPath}", assemblyPath);
        }

        throw new DirectoryNotFoundException("Could not find the repository root containing Nuscope.Cli/Nuscope.Cli.csproj.");
    }
}
