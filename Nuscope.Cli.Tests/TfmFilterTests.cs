using System.Text.Json;

namespace Nuscope.Cli.Tests;

public sealed class TfmFilterTests
{
    /// <summary>
    /// Verifies that NuGet package inspection can be restricted to one target framework.
    /// </summary>
    [Test]
    public async Task Tfm_filter_limits_package_assemblies_to_requested_target_framework()
    {
        // Arrange
        var sample = await TestWorkspace.Current.SampleAsync();

        // Act
        var result = await CliTestRunner.RunAsync([sample.PackagePath, "--format", "json", "--tfm", "netstandard2.0"]);

        // Assert
        using var document = JsonDocument.Parse(result.Output);
        var assemblies = document.RootElement.GetProperty("Assemblies").EnumerateArray().ToArray();

        using (Assert.Multiple())
        {
            await Assert.That(result.ExitCode).IsEqualTo(0);
            await Assert.That(assemblies.Length).IsEqualTo(1);
            await Assert.That(assemblies[0].GetProperty("TargetFramework").GetString()).IsEqualTo("netstandard2.0");
            await Assert.That(assemblies[0].GetProperty("AssetKind").GetString()).IsEqualTo("lib");
            await Assert.That(assemblies[0].GetProperty("Path").GetString()).Contains("lib/netstandard2.0/");
        }
    }
}
