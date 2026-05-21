using TUnit.Core;

namespace Nuscope.Cli.Tests;

public sealed class SkillCommandTests
{
    [Test]
    public async Task Skill_local_installs_skill_in_nearest_project()
    {
        // Arrange
        var root = CreateTemporaryDirectory();
        var child = Path.Combine(root, "src", "app");
        Directory.CreateDirectory(Path.Combine(root, ".git"));
        Directory.CreateDirectory(child);

        try
        {
            // Act
            var result = await CliTestRunner.RunCommandAsync(["skill", "local"], child);

            // Assert
            var skillPath = Path.Combine(root, ".agents", "skills", "nuscope", "SKILL.md");
            using (Assert.Multiple())
            {
                await Assert.That(result.ExitCode).IsEqualTo(0);
                await Assert.That(result.Output).Contains(skillPath);
                await Assert.That(File.Exists(skillPath)).IsTrue();
                await Assert.That(await File.ReadAllTextAsync(skillPath)).Contains("name: nuscope");
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Skill_global_installs_skill_under_user_home()
    {
        // Arrange
        var home = CreateTemporaryDirectory();

        try
        {
            // Act
            var result = await CliTestRunner.RunCommandAsync(
                ["skill", "global"],
                environment: new Dictionary<string, string?> { ["HOME"] = home });

            // Assert
            var skillPath = Path.Combine(home, ".agents", "skills", "nuscope", "SKILL.md");
            using (Assert.Multiple())
            {
                await Assert.That(result.ExitCode).IsEqualTo(0);
                await Assert.That(result.Output).Contains(skillPath);
                await Assert.That(File.Exists(skillPath)).IsTrue();
                await Assert.That(await File.ReadAllTextAsync(skillPath)).Contains("nuscope inspect");
            }
        }
        finally
        {
            Directory.Delete(home, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nuscope-skill-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
