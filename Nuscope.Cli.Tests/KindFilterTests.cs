namespace Nuscope.Cli.Tests;

[MethodDataSource(typeof(InspectTargetCases), nameof(InspectTargetCases.All))]
public sealed class KindFilterTests(string targetName)
{
    /// <summary>
    /// Verifies that each kind filter suppresses unrelated symbol kinds across all supported input forms.
    /// </summary>
    [Test]
    [MethodDataSource(nameof(KindOnlyCases))]
    public async Task Kind_filter_without_search_limits_output_to_requested_symbol_kind(KindOnlyCase filter)
    {
        // Arrange
        await using var target = await InspectTargetCases.ResolveAsync(targetName);

        // Act
        var result = await CliTestRunner.RunAsync([.. target.BaseArgs, "--format", "json", "--kind", filter.Kind]);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result.ExitCode).IsEqualTo(0);
            await Assert.That(result.Output).Contains($"\"Kind\": \"{filter.ExpectedKind}\"");
            await Assert.That(result.Output).Contains($"\"Name\": \"{filter.ExpectedName}\"");
            await Assert.That(result.Output).DoesNotContain($"\"Kind\": \"{filter.UnexpectedKind}\"");
        }
    }

    /// <summary>
    /// Verifies that kind and search filters are applied together rather than independently widening results.
    /// </summary>
    [Test]
    [MethodDataSource(nameof(KindCases))]
    public async Task Kind_filter_with_search_limits_output_to_requested_symbol_kind(KindFilterCase filter)
    {
        // Arrange
        await using var target = await InspectTargetCases.ResolveAsync(targetName);

        // Act
        var result = await CliTestRunner.RunAsync([.. target.BaseArgs, "--format", "json", "--search", filter.Search, "--kind", filter.Kind]);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result.ExitCode).IsEqualTo(0);
            await Assert.That(result.Output).Contains($"\"Kind\": \"{filter.ExpectedKind}\"");
            await Assert.That(result.Output).Contains($"\"Name\": \"{filter.ExpectedName}\"");
            await Assert.That(result.Output).DoesNotContain($"\"Name\": \"{filter.UnexpectedName}\"");
        }
    }

    /// <summary>
    /// Provides one combined kind/search example for every reportable symbol kind.
    /// </summary>
    public static IEnumerable<KindFilterCase> KindCases()
    {
        yield return new KindFilterCase("type", "widget", "Type", "Nuscope.Sample.Widget", "Nuscope.Sample.Widget.Resize");
        yield return new KindFilterCase("constructor", "widget", "Constructor", "Nuscope.Sample.Widget.Widget", "Nuscope.Sample.Widget.Name");
        yield return new KindFilterCase("property", "name", "Property", "Nuscope.Sample.Widget.Name", "Nuscope.Sample.Widget.DefaultName");
        yield return new KindFilterCase("method", "resize", "Method", "Nuscope.Sample.Widget.Resize", "Nuscope.Sample.Widget.Name");
        yield return new KindFilterCase("event", "changed", "Event", "Nuscope.Sample.Widget.Changed", "Nuscope.Sample.Widget.Resize");
        yield return new KindFilterCase("field", "default", "Field", "Nuscope.Sample.Widget.DefaultName", "Nuscope.Sample.Widget.Resize");
    }

    /// <summary>
    /// Provides one kind-only example for every reportable symbol kind.
    /// </summary>
    public static IEnumerable<KindOnlyCase> KindOnlyCases()
    {
        yield return new KindOnlyCase("type", "Type", "Nuscope.Sample.Widget", "Method");
        yield return new KindOnlyCase("constructor", "Constructor", "Nuscope.Sample.Widget.Widget", "Property");
        yield return new KindOnlyCase("property", "Property", "Nuscope.Sample.Widget.Name", "Method");
        yield return new KindOnlyCase("method", "Method", "Nuscope.Sample.Widget.Resize", "Property");
        yield return new KindOnlyCase("event", "Event", "Nuscope.Sample.Widget.Changed", "Method");
        yield return new KindOnlyCase("field", "Field", "Nuscope.Sample.Widget.DefaultName", "Method");
    }
}
