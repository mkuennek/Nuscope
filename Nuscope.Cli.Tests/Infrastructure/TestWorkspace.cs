using System.Diagnostics;

namespace Nuscope.Cli.Tests;

internal sealed class TestWorkspace
{
    /// <summary>
    /// Shares the generated sample project across tests so expensive build setup only runs once.
    /// </summary>
    public static TestWorkspace Current { get; } = new();

    private readonly Lazy<Task<SampleArtifacts>> _sample;
    private readonly Lazy<Task<NuGetFeed>> _feed;

    private TestWorkspace()
    {
        _sample = new Lazy<Task<SampleArtifacts>>(CreateSampleAsync);
        _feed = new Lazy<Task<NuGetFeed>>(CreateFeedAsync);
    }

    /// <summary>
    /// Returns the compiled sample assembly and package used as inspection targets.
    /// </summary>
    public Task<SampleArtifacts> SampleAsync() => _sample.Value;

    /// <summary>
    /// Returns the shared in-process NuGet feed used by all remote package tests.
    /// </summary>
    public Task<NuGetFeed> FeedAsync() => _feed.Value;

    /// <summary>
    /// Starts the dummy NuGet feed once per test process to avoid parallel tests racing for ports.
    /// </summary>
    private async Task<NuGetFeed> CreateFeedAsync()
    {
        var sample = await SampleAsync();
        return await NuGetFeed.StartAsync(sample);
    }

    /// <summary>
    /// Creates a temporary sample project that exposes the symbols needed by the integration tests.
    /// </summary>
    private static async Task<SampleArtifacts> CreateSampleAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "nuscope-tests", Guid.NewGuid().ToString("N"));
        var sampleDir = Path.Combine(root, "Sample");
        Directory.CreateDirectory(sampleDir);

        await File.WriteAllTextAsync(Path.Combine(sampleDir, "Sample.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFrameworks>netstandard2.0;net10.0</TargetFrameworks>
                <LangVersion>latest</LangVersion>
                <Nullable>enable</Nullable>
                <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
                <PackageId>Nuscope.Sample</PackageId>
                <Version>1.0.0</Version>
              </PropertyGroup>
            </Project>
            """);

        await File.WriteAllTextAsync(Path.Combine(sampleDir, "Widget.cs"), """
            using System;

            namespace Nuscope.Sample;

            public interface IWidget
            {
                string Name { get; }
                event EventHandler? Changed;
                int Resize(int width, int height);
            }

            public sealed class Widget : IWidget
            {
                public const string DefaultName = "widget";
                public string Name { get; }
                public event EventHandler? Changed;
                public Widget(string name) => Name = name;
                public int Resize(int width, int height) => width * height;
                internal void Hidden() { }
            }

            public readonly struct WidgetSize
            {
                public int Width { get; }
            }

            public enum WidgetMode
            {
                Compact,
                Full
            }
            """);

        await RunDotnetAsync(sampleDir, "build", "Sample.csproj", "-c", "Release");

        return new SampleArtifacts(
            Root: root,
            DllPath: Path.Combine(sampleDir, "bin", "Release", "net10.0", "Sample.dll"),
            PackagePath: Path.Combine(sampleDir, "bin", "Release", "Nuscope.Sample.1.0.0.nupkg"));
    }

    /// <summary>
    /// Runs dotnet commands needed to build test artifacts and fails fast with captured output.
    /// </summary>
    private static async Task RunDotnetAsync(string workingDirectory, params string[] args)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start dotnet.");
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"dotnet {string.Join(' ', args)} failed with exit code {process.ExitCode}.\n{output}\n{error}");
        }
    }
}
