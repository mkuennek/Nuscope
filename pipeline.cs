#!/usr/bin/env dotnet run
#:sdk Cake.Sdk@6.1.0

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var nugetSource = Argument("nugetSource", "https://api.nuget.org/v3/index.json");

const string solution = "./Nuscope.slnx";
const string testProject = "./Nuscope.Cli.Tests/Nuscope.Cli.Tests.csproj";
const string packProject = "./Nuscope.Cli/Nuscope.Cli.csproj";
const string toolPackageId = "Nuscope";
const string skillDirectory = "./skills/nuscope";
const string artifactsDirectory = "./artifacts";
const string packagesDirectory = "./artifacts/packages";

string ReadPackageVersion(string projectXml)
{
    var document = System.Xml.Linq.XDocument.Parse(projectXml);
    var version = document.Descendants("Version").FirstOrDefault()?.Value;

    if (string.IsNullOrWhiteSpace(version))
    {
        throw new Exception("Could not find a <Version> element in the package project file.");
    }

    return version.Trim();
}

string ReadCurrentPackageVersion()
{
    return ReadPackageVersion(System.IO.File.ReadAllText(packProject));
}

string RunGit(params string[] arguments)
{
    var output = new System.Text.StringBuilder();
    var error = new System.Text.StringBuilder();
    using var process = new System.Diagnostics.Process();

    process.StartInfo = new System.Diagnostics.ProcessStartInfo
    {
        FileName = "git",
        RedirectStandardError = true,
        RedirectStandardOutput = true,
        UseShellExecute = false,
    };

    foreach (var argument in arguments)
    {
        process.StartInfo.ArgumentList.Add(argument);
    }

    process.OutputDataReceived += (_, args) =>
    {
        if (args.Data is not null)
        {
            output.AppendLine(args.Data);
        }
    };
    process.ErrorDataReceived += (_, args) =>
    {
        if (args.Data is not null)
        {
            error.AppendLine(args.Data);
        }
    };

    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();
    process.WaitForExit();

    if (process.ExitCode != 0)
    {
        throw new Exception($"git {string.Join(' ', arguments)} failed: {error.ToString().Trim()}");
    }

    return output.ToString().Trim();
}

string? GetCurrentBranchName()
{
    var githubRefName = EnvironmentVariable("GITHUB_REF_NAME");

    if (!string.IsNullOrWhiteSpace(githubRefName))
    {
        return githubRefName;
    }

    var githubRef = EnvironmentVariable("GITHUB_REF");

    if (!string.IsNullOrWhiteSpace(githubRef) && githubRef.StartsWith("refs/heads/", StringComparison.Ordinal))
    {
        return githubRef["refs/heads/".Length..];
    }

    try
    {
        return RunGit("branch", "--show-current");
    }
    catch (Exception exception)
    {
        Information("Could not determine current git branch: {0}", exception.Message);
        return null;
    }
}

bool IsMainBranch()
{
    var branch = GetCurrentBranchName();
    var isMain = string.Equals(branch, "main", StringComparison.Ordinal);

    Information("Current branch: {0}", string.IsNullOrWhiteSpace(branch) ? "<unknown>" : branch);
    Information("On main branch: {0}", isMain);

    return isMain;
}

string GetPackageBaseAddress()
{
    using var httpClient = new System.Net.Http.HttpClient();
    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Nuscope-Build/1.0");

    using var response = httpClient.GetAsync(nugetSource).GetAwaiter().GetResult();

    if (!response.IsSuccessStatusCode)
    {
        throw new Exception($"Could not read NuGet service index from {nugetSource}: {(int)response.StatusCode} {response.ReasonPhrase}");
    }

    using var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
    using var document = System.Text.Json.JsonDocument.Parse(stream);

    foreach (var resource in document.RootElement.GetProperty("resources").EnumerateArray())
    {
        var type = resource.GetProperty("@type").GetString();

        if (type?.Split('/').Any(value => value.Equals("PackageBaseAddress", StringComparison.OrdinalIgnoreCase)) == true)
        {
            return resource.GetProperty("@id").GetString() ?? throw new Exception("PackageBaseAddress resource did not include an @id.");
        }
    }

    throw new Exception($"Could not find a PackageBaseAddress resource in {nugetSource}.");
}

bool PackageVersionExistsOnSource()
{
    var currentVersion = NuGet.Versioning.NuGetVersion.Parse(ReadCurrentPackageVersion()).ToNormalizedString();
    var packageId = toolPackageId.ToLowerInvariant();
    var packageBaseAddress = GetPackageBaseAddress().TrimEnd('/');
    var versionIndexUrl = $"{packageBaseAddress}/{packageId}/index.json";

    Information("Checking whether {0} {1} exists on {2}.", toolPackageId, currentVersion, nugetSource);

    using var httpClient = new System.Net.Http.HttpClient();
    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Nuscope-Build/1.0");

    using var response = httpClient.GetAsync(versionIndexUrl).GetAwaiter().GetResult();

    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        Information("Package {0} does not exist on {1} yet.", toolPackageId, nugetSource);
        return false;
    }

    if (!response.IsSuccessStatusCode)
    {
        throw new Exception($"Could not read package version index from {versionIndexUrl}: {(int)response.StatusCode} {response.ReasonPhrase}");
    }

    using var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
    using var document = System.Text.Json.JsonDocument.Parse(stream);
    var exists = document.RootElement.GetProperty("versions")
        .EnumerateArray()
        .Select(version => NuGet.Versioning.NuGetVersion.Parse(version.GetString()!).ToNormalizedString())
        .Any(version => string.Equals(version, currentVersion, StringComparison.OrdinalIgnoreCase));

    Information("Package version exists on source: {0}", exists);

    return exists;
}

bool ShouldDeployPackage()
{
    if (!IsMainBranch())
    {
        Information("Skipping deployment because packages may only be published from the main branch.");
        return false;
    }

    if (PackageVersionExistsOnSource())
    {
        Information("Skipping deployment because this package version already exists on the NuGet source.");
        return false;
    }

    return true;
}

Task("Clean")
    .WithCriteria(c => HasArgument("rebuild"))
    .Does(() =>
{
    DotNetClean(solution, new DotNetCleanSettings
    {
        Configuration = configuration,
    });

    CleanDirectory(artifactsDirectory);
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetRestore(solution);
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    DotNetBuild(solution, new DotNetBuildSettings
    {
        Configuration = configuration,
        NoRestore = true,
    });
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    DotNetTest(testProject, new DotNetTestSettings
    {
        Configuration = configuration,
        NoBuild = true,
        NoRestore = true,
    });
});

Task("Pack")
    .IsDependentOn("Test")
    .Does(() =>
{
    EnsureDirectoryExists(packagesDirectory);

    DotNetPack(packProject, new DotNetPackSettings
    {
        Configuration = configuration,
        NoBuild = true,
        NoRestore = true,
        OutputDirectory = packagesDirectory,
    });
});

Task("Publish")
    .IsDependentOn("Pack")
    .Does(() =>
{
    if (!IsMainBranch())
    {
        throw new Exception("Packages may only be published from the main branch.");
    }

    var nugetApiKey = EnvironmentVariable("NUGET_API_KEY");

    if (string.IsNullOrWhiteSpace(nugetApiKey))
    {
        throw new Exception("NUGET_API_KEY environment variable is required to publish packages.");
    }

    var packages = GetFiles($"{packagesDirectory}/*.nupkg");

    if (packages.Count == 0)
    {
        throw new Exception($"No NuGet packages found in {packagesDirectory}.");
    }

    foreach (var package in packages)
    {
        Information("Publishing {0} to {1}.", package.GetFilename(), nugetSource);

        DotNetNuGetPush(package.FullPath, new DotNetNuGetPushSettings
        {
            Source = nugetSource,
            ApiKey = nugetApiKey,
            SkipDuplicate = true,
        });
    }
});

Task("InstallLocally")
    .IsDependentOn("Pack")
    .Does(() =>
{
    var packageSource = MakeAbsolute(Directory(packagesDirectory)).FullPath;
    var skillSource = MakeAbsolute(Directory(skillDirectory)).FullPath;
    var homeDirectory = EnvironmentVariable("HOME");

    if (string.IsNullOrWhiteSpace(homeDirectory))
    {
        homeDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
    }

    if (string.IsNullOrWhiteSpace(homeDirectory))
    {
        throw new Exception("Could not determine the home directory for skill installation.");
    }

    var skillDestination = System.IO.Path.Combine(homeDirectory, ".agents", "skills", "nuscope");

    Information("Installing {0} from {1}.", toolPackageId, packageSource);

    StartProcess("dotnet", new ProcessSettings
    {
        Arguments = new ProcessArgumentBuilder()
            .Append("tool")
            .Append("uninstall")
            .Append(toolPackageId)
            .Append("--global"),
    });

    var exitCode = StartProcess("dotnet", new ProcessSettings
    {
        Arguments = new ProcessArgumentBuilder()
            .Append("tool")
            .Append("install")
            .Append(toolPackageId)
            .Append("--global")
            .Append("--add-source")
            .AppendQuoted(packageSource),
    });

    if (exitCode != 0)
    {
        throw new Exception($"Failed to install {toolPackageId} from {packageSource}.");
    }

    Information("Installing nuscope agent skill to {0}.", skillDestination);

    if (!System.IO.Directory.Exists(skillSource))
    {
        throw new Exception($"Skill source directory does not exist: {skillSource}");
    }

    System.IO.Directory.CreateDirectory(skillDestination);

    foreach (var sourceFile in System.IO.Directory.EnumerateFiles(skillSource, "*", System.IO.SearchOption.AllDirectories))
    {
        var relativePath = System.IO.Path.GetRelativePath(skillSource, sourceFile);
        var destinationFile = System.IO.Path.Combine(skillDestination, relativePath);
        var destinationDirectory = System.IO.Path.GetDirectoryName(destinationFile);

        if (!string.IsNullOrWhiteSpace(destinationDirectory))
        {
            System.IO.Directory.CreateDirectory(destinationDirectory);
        }

        System.IO.File.Copy(sourceFile, destinationFile, overwrite: true);
    }
});

Task("Default")
    .IsDependentOn("Test");

Task("CI")
    .IsDependentOn("Pack");

Task("Deploy")
    .IsDependentOn("Publish");

if (target.Equals("Deploy", StringComparison.OrdinalIgnoreCase) && !ShouldDeployPackage())
{
    return;
}

if (target.Equals("Publish", StringComparison.OrdinalIgnoreCase) && !IsMainBranch())
{
    throw new Exception("Packages may only be published from the main branch.");
}

RunTarget(target);
