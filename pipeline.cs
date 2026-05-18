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

RunTarget(target);
