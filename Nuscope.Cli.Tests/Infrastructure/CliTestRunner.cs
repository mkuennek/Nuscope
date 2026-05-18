using System.Diagnostics;

namespace Nuscope.Cli.Tests;

internal static class CliTestRunner
{
    /// <summary>
    /// Runs the built nuscope CLI as a child process and captures its exit code and output streams.
    /// </summary>
    public static async Task<CliResult> RunAsync(string[] args)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        startInfo.ArgumentList.Add(CliAssembly.Path);
        startInfo.ArgumentList.Add("inspect");

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
