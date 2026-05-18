using TUnit.Core;

namespace Nuscope.Cli.Tests;

[MethodDataSource(typeof(InspectTargetCases), nameof(InspectTargetCases.All))]
public sealed class SearchTests(string targetName)
{
    /// <summary>
    /// Verifies that search terms keep matching symbols and remove unrelated symbols from text output.
    /// </summary>
    [Test]
    [MethodDataSource(nameof(SearchCases))]
    public async Task Search_limits_output_to_matching_symbols(SearchCase search)
    {
        // Arrange
        await using var target = await InspectTargetCases.ResolveAsync(targetName);

        // Act
        var result = await CliTestRunner.RunAsync([.. target.BaseArgs, "--search", search.Term]);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result.ExitCode).IsEqualTo(0);
            await Assert.That(result.Output).Contains(search.Expected);
            await Assert.That(result.Output).DoesNotContain(search.Unexpected);
        }
    }

    /// <summary>
    /// Exercises matching by member name, type name, and field name.
    /// </summary>
    public static IEnumerable<SearchCase> SearchCases()
    {
        yield return new SearchCase("Resize", "Resize", "string Name");
        yield return new SearchCase("WidgetSize", "WidgetSize", "WidgetMode");
        yield return new SearchCase("DefaultName", "DefaultName", "Resize");
    }
}
