using TUnit.Core;

namespace Nuscope.Cli.Tests;

[MethodDataSource(typeof(InspectTargetCases), nameof(InspectTargetCases.All))]
public sealed class OutputFormatTests(string targetName)
{
    /// <summary>
    /// Verifies that default text output uses a C#-like layout and keeps members in stable display order.
    /// </summary>
    [Test]
    public async Task Text_format_uses_code_like_layout_grouped_by_namespace_and_type()
    {
        // Arrange
        await using var target = await InspectTargetCases.ResolveAsync(targetName);

        // Act
        var result = await CliTestRunner.RunAsync(target.BaseArgs);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result.ExitCode).IsEqualTo(0);
            await Assert.That(result.Output).Contains("  namespace Nuscope.Sample;");
            await Assert.That(result.Output).Contains("  /// <summary>");
            await Assert.That(result.Output).Contains("  /// Represents a documented Nuscope.Sample.Widget.");
            await Assert.That(result.Output).Contains("----------------------------------------");
            await Assert.That(result.Output).Contains("  public sealed class Widget");
            await Assert.That(result.Output).Contains("  {");
            await Assert.That(result.Output).Contains("    /// Creates a widget with the supplied name.");
            await Assert.That(result.Output).Contains("    public Widget(string);");
            await Assert.That(result.Output).Contains("    public string Name { get; }");
            await Assert.That(result.Output).Contains("    public int32 Priority { get; set; }");
            await Assert.That(result.Output).Contains("    public System.Guid Id { get; private set; }");
            await Assert.That(result.Output).Contains("    public int32 Resize(int32, int32);");
            await Assert.That(result.Output).Contains("    public event System.EventHandler Changed;");
            await Assert.That(result.Output).Contains("  }");

            var widgetIndex = result.Output.IndexOf("  public sealed class Widget", StringComparison.Ordinal);
            var constructorIndex = result.Output.IndexOf("    public Widget(string);", widgetIndex, StringComparison.Ordinal);
            var propertyIndex = result.Output.IndexOf("    public string Name { get; }", widgetIndex, StringComparison.Ordinal);
            var methodIndex = result.Output.IndexOf("    public int32 Resize", widgetIndex, StringComparison.Ordinal);
            var eventIndex = result.Output.IndexOf("    public event System.EventHandler Changed;", widgetIndex, StringComparison.Ordinal);

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
            await Assert.That(result.Output).Contains("\"Documentation\": \"Represents a documented Nuscope.Sample.Widget.\"");
            await Assert.That(result.Output).Contains("\"TypeKind\": \"Class\"");
            await Assert.That(result.Output).Contains("\"Modifiers\": [");
            await Assert.That(result.Output).Contains("\"sealed\"");
            await Assert.That(result.Output).Contains("\"Name\": \"Nuscope.Sample.Widget.Widget\"");
            await Assert.That(result.Output).Contains("\"Kind\": \"Constructor\"");
            await Assert.That(result.Output).Contains("\"Name\": \"Nuscope.Sample.Widget.Name\"");
            await Assert.That(result.Output).Contains("\"Kind\": \"Property\"");
            await Assert.That(result.Output).Contains("\"Name\": \"Nuscope.Sample.Widget.Priority\"");
            await Assert.That(result.Output).Contains("\"Accessors\": [");
            await Assert.That(result.Output).Contains("\"get;\"");
            await Assert.That(result.Output).Contains("\"set;\"");
            await Assert.That(result.Output).Contains("\"Name\": \"Nuscope.Sample.Widget.Id\"");
            await Assert.That(result.Output).Contains("\"private set;\"");
            await Assert.That(result.Output).Contains("\"Name\": \"Nuscope.Sample.Widget.Resize\"");
            await Assert.That(result.Output).Contains("\"Kind\": \"Method\"");
            await Assert.That(result.Output).Contains("\"Documentation\": \"Resizes the widget and returns the new area.\"");
            await Assert.That(result.Output).Contains("\"Name\": \"Nuscope.Sample.Widget.Changed\"");
            await Assert.That(result.Output).Contains("\"Kind\": \"Event\"");
            await Assert.That(result.Output).Contains("\"Name\": \"Nuscope.Sample.Widget.DefaultName\"");
            await Assert.That(result.Output).Contains("\"Kind\": \"Field\"");
            await Assert.That(result.Output).Contains("\"Classification\": \"struct\"");
            await Assert.That(result.Output).Contains("\"Classification\": \"enum\"");
        }
    }

    /// <summary>
    /// Verifies that JSON output omits nullable properties instead of emitting noisy null values.
    /// </summary>
    [Test]
    public async Task Json_format_omits_null_values()
    {
        // Arrange
        await using var target = await InspectTargetCases.ResolveAsync(targetName);

        // Act
        var result = await CliTestRunner.RunAsync([.. target.BaseArgs, "--format", "json"]);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result.ExitCode).IsEqualTo(0);
            await Assert.That(result.Output).DoesNotContain(": null");
            await Assert.That(result.Output).DoesNotContain("\"Documentation\": null");
        }
    }
}
