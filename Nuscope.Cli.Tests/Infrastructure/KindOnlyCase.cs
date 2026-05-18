namespace Nuscope.Cli.Tests;

public readonly record struct KindOnlyCase(string Kind, string ExpectedKind, string ExpectedName, string UnexpectedKind)
{
    /// <summary>
    /// Shows the filtered kind in generated test case names.
    /// </summary>
    public override string ToString() => Kind;
}
