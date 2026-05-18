namespace Nuscope.Cli.Tests;

public static class InspectTargetCases
{
    private const string RemotePackageId = "Nuscope.Sample";

    /// <summary>
    /// Enumerates the same sample API as a DLL, a local package, and a package ID served by the test feed.
    /// </summary>
    public static IEnumerable<string> All()
    {
        var sample = TestWorkspace.Current.SampleAsync().GetAwaiter().GetResult();

        yield return sample.DllPath;
        yield return sample.PackagePath;
        yield return RemotePackageId;
    }

    /// <summary>
    /// Converts a target case into CLI arguments and any temporary resource needed to serve it.
    /// </summary>
    public static async Task<InspectTargetLease> ResolveAsync(string target)
    {
        if (target.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            return new InspectTargetLease(new InspectTarget([target], Path.GetFileName(target)));
        }

        if (target.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
        {
            return new InspectTargetLease(new InspectTarget([target], Path.GetFileName(target)));
        }

        var feed = await TestWorkspace.Current.FeedAsync();
        var inspectTarget = new InspectTarget(
            [target, "--source", feed.ServiceIndexUrl, "--version", "1.0.0"],
            target);

        return new InspectTargetLease(inspectTarget);
    }
}
