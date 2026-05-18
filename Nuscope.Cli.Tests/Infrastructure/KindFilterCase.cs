namespace Nuscope.Cli.Tests;

public readonly record struct KindFilterCase(string Kind, string Search, string ExpectedKind, string ExpectedName, string UnexpectedName)
{
    /// <summary>
    /// Shows the filtered kind in generated test case names.
    /// </summary>
    public override string ToString() => Kind;
}
