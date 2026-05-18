namespace Nuscope.Cli;

internal sealed record InspectOptions(
    string Input,
    OutputFormat Format,
    bool IncludeNonPublic,
    string? Search,
    SymbolKind? Kind,
    string? Version,
    string Source,
    bool IncludePrerelease,
    string? TargetFramework)
{
    public const string DefaultSource = "https://api.nuget.org/v3/index.json";

    /// <summary>
    /// Converts the inspect command arguments into validated options consumed by the inspection pipeline.
    /// </summary>
    public static InspectOptions Parse(ReadOnlySpan<string> args)
    {
        if (args.Length == 0)
        {
            throw new CliException("Missing path, package ID, or .nupkg.", 2);
        }

        string? input = null;
        var format = OutputFormat.Text;
        var includeNonPublic = false;
        string? search = null;
        SymbolKind? kind = null;
        string? version = null;
        var source = DefaultSource;
        var includePrerelease = false;
        string? targetFramework = null;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--format":
                    format = ParseFormat(RequireValue(args, ref i, "--format"));
                    break;
                case "--include-non-public":
                    includeNonPublic = true;
                    break;
                case "--search":
                    search = RequireValue(args, ref i, "--search");
                    break;
                case "--kind":
                    kind = ParseKind(RequireValue(args, ref i, "--kind"));
                    break;
                case "--version":
                    version = RequireValue(args, ref i, "--version");
                    break;
                case "--source":
                    source = RequireValue(args, ref i, "--source");
                    break;
                case "--prerelease":
                    includePrerelease = true;
                    break;
                case "--tfm":
                    targetFramework = RequireValue(args, ref i, "--tfm");
                    break;
                default:
                    if (arg.StartsWith('-'))
                    {
                        throw new CliException($"Unknown option '{arg}'.", 2);
                    }

                    if (input is not null)
                    {
                        throw new CliException($"Unexpected argument '{arg}'.", 2);
                    }

                    input = arg;
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            throw new CliException("Missing path, package ID, or .nupkg.", 2);
        }

        return new InspectOptions(input, format, includeNonPublic, search, kind, version, source, includePrerelease, targetFramework);
    }

    /// <summary>
    /// Reads the value that must follow an option and advances the parser past that value.
    /// </summary>
    private static string RequireValue(ReadOnlySpan<string> args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
        {
            throw new CliException($"Missing value for {option}.", 2);
        }

        index++;
        return args[index];
    }

    /// <summary>
    /// Maps the text output format option to the internal enum used by the writer.
    /// </summary>
    private static OutputFormat ParseFormat(string value) =>
        value.ToLowerInvariant() switch
        {
            "text" => OutputFormat.Text,
            "json" => OutputFormat.Json,
            _ => throw new CliException("--format must be 'text' or 'json'.", 2)
        };

    /// <summary>
    /// Maps the text symbol kind filter to the internal enum used during symbol collection.
    /// </summary>
    private static SymbolKind ParseKind(string value) =>
        value.ToLowerInvariant() switch
        {
            "type" => SymbolKind.Type,
            "method" => SymbolKind.Method,
            "property" => SymbolKind.Property,
            "field" => SymbolKind.Field,
            "event" => SymbolKind.Event,
            "constructor" => SymbolKind.Constructor,
            _ => throw new CliException("--kind must be type, method, property, field, event, or constructor.", 2)
        };
}
