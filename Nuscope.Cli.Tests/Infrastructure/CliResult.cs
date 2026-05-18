namespace Nuscope.Cli.Tests;

internal sealed record CliResult(int ExitCode, string Output, string Error);
