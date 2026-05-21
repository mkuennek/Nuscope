namespace Nuscope.Cli;

public static class Program
{
    /// <summary>
    /// Dispatches the CLI command, runs the requested inspection, and maps known failures to process exit codes.
    /// </summary>
    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
            {
                Usage.Write(Console.Out);
                return 0;
            }

            if (args[0] == "skill")
            {
                await SkillInstaller.InstallAsync(args[1..]);
                return 0;
            }

            if (args[0] == "inspect")
            {
                var options = InspectOptions.Parse(args.AsSpan(1));
                var report = await PackageInspector.InspectAsync(options);
                OutputWriter.Write(Console.Out, report, options.Format);
                return 0;
            }

            await Console.Error.WriteLineAsync($"Unknown command '{args[0]}'.");
            Usage.Write(Console.Error);
            return 2;
        }
        catch (CliException ex)
        {
            await Console.Error.WriteLineAsync(ex.Message);
            return ex.ExitCode;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.Message);
            return 1;
        }
    }
}
