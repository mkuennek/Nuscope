namespace Nuscope.Cli.Tests;

public readonly record struct SearchCase(string Term, string Expected, string Unexpected)
{
    /// <summary>
    /// Shows the search term in generated test case names.
    /// </summary>
    public override string ToString() => Term;
}
