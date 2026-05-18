using TUnit.Core;

namespace Nuscope.Cli.Tests;

[MethodDataSource(typeof(InspectTargetCases), nameof(InspectTargetCases.All))]
public sealed class OutputFormatTests(string targetName)
{
    /// <summary>
    /// Verifies that default text output is grouped by namespace/type and keeps members in stable display order.
    /// </summary>
    [Test]
    public async Task Text_format_groups_symbols_by_namespace_and_type()
    {
        // Arrange
        await using var target = await InspectTargetCases.ResolveAsync(targetName);

        // Act
        var result = await CliTestRunner.RunAsync(target.BaseArgs);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result.ExitCode).IsEqualTo(0);
            await Assert.That(result.Output).Contains("  namespace Nuscope.Sample");
            await Assert.That(result.Output).Contains("    Widget (sealed class, public)");
            await Assert.That(result.Output).Contains("      constructor public             void ctor(string)");
            await Assert.That(result.Output).Contains("      property    public             string Name");
            await Assert.That(result.Output).Contains("      method      public             int32 Resize(int32, int32)");
            await Assert.That(result.Output).Contains("      event       public             System.EventHandler Changed");

            var widgetIndex = result.Output.IndexOf("    Widget (sealed class, public)", StringComparison.Ordinal);
            var constructorIndex = result.Output.IndexOf("      constructor public", widgetIndex, StringComparison.Ordinal);
            var propertyIndex = result.Output.IndexOf("      property    public             string Name", widgetIndex, StringComparison.Ordinal);
            var methodIndex = result.Output.IndexOf("      method      public             int32 Resize", widgetIndex, StringComparison.Ordinal);
            var eventIndex = result.Output.IndexOf("      event       public             System.EventHandler Changed", widgetIndex, StringComparison.Ordinal);

            await Assert.That(constructorIndex < propertyIndex && propertyIndex < methodIndex && methodIndex < eventIndex).IsTrue();
        }
    }

    /// <summary>
    /// Verifies that JSON output contains the discovered public types and members with their key metadata.
    /// </summary>
    [Test]
    public async Task Json_format_contains_discovered_public_symbols()
    {
        // Arrange
        await using var target = await InspectTargetCases.ResolveAsync(targetName);

        // Act
        var result = await CliTestRunner.RunAsync([.. target.BaseArgs, "--format", "json"]);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result.ExitCode).IsEqualTo(0);
            await Assert.That(result.Output).Contains("\"Name\": \"Nuscope.Sample.Widget\"");
            await Assert.That(result.Output).Contains("\"Classification\": \"sealed class\"");
            await Assert.That(result.Output).Contains("\"Signature\": \"public sealed class Nuscope.Sample.Widget\"");
            await Assert.That(result.Output).Contains("\"TypeKind\": \"Class\"");
            await Assert.That(result.Output).Contains("\"Modifiers\": [");
            await Assert.That(result.Output).Contains("\"sealed\"");
            await Assert.That(result.Output).Contains("\"Name\": \"Nuscope.Sample.Widget.Widget\"");
            await Assert.That(result.Output).Contains("\"Kind\": \"Constructor\"");
            await Assert.That(result.Output).Contains("\"Name\": \"Nuscope.Sample.Widget.Name\"");
            await Assert.That(result.Output).Contains("\"Kind\": \"Property\"");
            await Assert.That(result.Output).Contains("\"Name\": \"Nuscope.Sample.Widget.Resize\"");
            await Assert.That(result.Output).Contains("\"Kind\": \"Method\"");
            await Assert.That(result.Output).Contains("\"Name\": \"Nuscope.Sample.Widget.Changed\"");
            await Assert.That(result.Output).Contains("\"Kind\": \"Event\"");
            await Assert.That(result.Output).Contains("\"Name\": \"Nuscope.Sample.Widget.DefaultName\"");
            await Assert.That(result.Output).Contains("\"Kind\": \"Field\"");
            await Assert.That(result.Output).Contains("\"Classification\": \"struct\"");
            await Assert.That(result.Output).Contains("\"Classification\": \"enum\"");
        }
    }
}
