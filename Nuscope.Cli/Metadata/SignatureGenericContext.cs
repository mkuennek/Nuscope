using System.Collections.Immutable;

namespace Nuscope.Cli;

internal sealed record SignatureGenericContext(
    ImmutableArray<string> TypeParameters,
    ImmutableArray<string> MethodParameters)
{
    public static SignatureGenericContext Empty { get; } = new([], []);
}
