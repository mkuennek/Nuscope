using System.Diagnostics;

namespace Nuscope.Cli.Tests;

internal static class CliTestRunner
{
    /// <summary>
    /// Runs the built nuscope CLI as a child process and captures its exit code and output streams.
    /// </summary>
    public static Task<CliResult> RunAsync(string[] args) => RunCommandAsync(["inspect", .. args]);

    public static async Task<CliResult> RunCommandAsync(
        string[] args,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        if (workingDirectory is not null)
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        foreach (var (key, value) in environment ?? new Dictionary<string, string?>())
        {
            startInfo.Environment[key] = value;
        }

        startInfo.ArgumentList.Add(CliAssembly.Path);

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start nuscope.");
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new CliResult(process.ExitCode, output, error);
    }
}
