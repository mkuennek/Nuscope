# Nuscope

Nuscope is a .NET CLI tool for inspecting NuGet packages and .NET assemblies without loading them. It exposes types, methods, properties, fields, events, and signatures in text or JSON for developers, automation, and AI agents.

> Tired of watching an AI agent try `ilspycmd`, fail, then spiral into increasingly cursed PowerShell just to answer “what's in this DLL?” Nuscope gives it one job and one clean command.

## Install

Requires the .NET 8 SDK/runtime or newer.

```sh
dotnet tool install --global Nuscope
```

Update an existing installation:

```sh
dotnet tool update --global Nuscope
```

## Agent skill

Nuscope also ships with an agent skill for Pi-compatible agents. Download the skill from [here](https://raw.githubusercontent.com/mkuennek/Nuscope/refs/heads/main/skills/nuscope/SKILL.md) and put it into a location compatible with your coding agent.

Once installed, agents can use the skill to inspect `.dll` and `.nupkg` files or remote NuGet packages without resorting to brittle `ilspycmd` wrappers or improvised shell glue.

## Why Nuscope?

- Inspect package and assembly API surface quickly
- Avoid loading or executing inspected binaries
- Query local `.dll` and `.nupkg` files or remote NuGet packages
- Filter by symbol name, kind, and target framework
- Export structured JSON for scripts, tools, and agents

## Quick examples

Inspect a local NuGet package:

```sh
nuscope inspect path/to/Package.nupkg
```

Inspect a local assembly as JSON:

```sh
nuscope inspect path/to/Library.dll --format json
```

Inspect a package from NuGet.org:

```sh
nuscope inspect Newtonsoft.Json --version 13.0.3
```

Search for a symbol:

```sh
nuscope inspect Microsoft.Extensions.Logging --search ILogger --kind type
```

Inspect a single target framework:

```sh
nuscope inspect Microsoft.Extensions.AI --kind type --tfm netstandard2.0 --format json
```

Include non-public members:

```sh
nuscope inspect path/to/Library.dll --include-non-public
```

## Options

- `--format text|json`: output format. Defaults to `text`.
- `--search <term>`: case-insensitive filter over symbol names and signatures.
- `--kind <type|method|property|field|event|constructor>`: include only a symbol kind.
- `--tfm <target-framework>`: for NuGet packages, inspect only assemblies for one target framework, such as `net8.0` or `netstandard2.0`.
- `--include-non-public`: include non-public members and types.
- `--version <version>`: inspect a specific NuGet package version. If omitted, the latest stable version is used.
- `--prerelease`: allow latest prerelease selection when `--version` is omitted.
- `--source <url>`: NuGet V3 service index URL. Defaults to `https://api.nuget.org/v3/index.json`.

JSON assembly entries include `TargetFramework` and `AssetKind` when inspecting NuGet packages. Type symbols include a type declaration `Signature`, a machine-readable `TypeKind`, and `Modifiers` in addition to the human-readable `Classification`.

## Build from source

The repository uses a Cake SDK build pipeline (`pipeline.cs`). Run the default target to restore, build, and test:

```sh
dotnet pipeline.cs
```

Useful targets:

```sh
dotnet pipeline.cs -- --target Build
dotnet pipeline.cs -- --target Test
dotnet pipeline.cs -- --target Pack
dotnet pipeline.cs -- --target Publish
dotnet pipeline.cs -- --target InstallLocally
dotnet pipeline.cs -- --target CI --rebuild
```

The `Pack`/`CI` targets write NuGet tool packages to `artifacts/packages`. The `Publish` target packs and publishes packages to NuGet using the `NUGET_API_KEY` environment variable. Override the feed with `--nugetSource <url>` when needed. The `InstallLocally` target packs the tool, installs it globally from that local package source, and copies the Nuscope agent skill to `~/.agents/skills/nuscope`.

## Test

```sh
dotnet pipeline.cs -- --target Test
```

Direct test execution is also supported:

```sh
dotnet test --project Nuscope.Cli.Tests/Nuscope.Cli.Tests.csproj
```

## License

MIT
