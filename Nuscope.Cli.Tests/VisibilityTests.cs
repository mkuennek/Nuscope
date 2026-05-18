using TUnit.Core;

namespace Nuscope.Cli.Tests;

[MethodDataSource(typeof(InspectTargetCases), nameof(InspectTargetCases.All))]
public sealed class VisibilityTests(string targetName)
{
    /// <summary>
    /// Verifies that the default report only includes the public API surface.
    /// </summary>
    [Test]
    public async Task Non_public_symbols_are_hidden_by_default()
    {
        // Arrange
        await using var target = await InspectTargetCases.ResolveAsync(targetName);

        // Act
        var result = await CliTestRunner.RunAsync([.. target.BaseArgs, "--format", "json"]);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result.ExitCode).IsEqualTo(0);
            await Assert.That(result.Output).DoesNotContain("Hidden");
        }
    }

    /// <summary>
    /// Verifies that the non-public flag expands the report to include internal members.
    /// </summary>
    [Test]
    public async Task Include_non_public_exposes_internal_symbols()
    {
        // Arrange
        await using var target = await InspectTargetCases.ResolveAsync(targetName);

        // Act
        var result = await CliTestRunner.RunAsync([.. target.BaseArgs, "--include-non-public", "--search", "Hidden"]);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result.ExitCode).IsEqualTo(0);
            await Assert.That(result.Output).Contains("void Hidden()");
        }
    }
}
