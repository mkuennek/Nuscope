# AGENTS.md

## Project basics

- Nuscope is a small .NET 10 CLI tool for inspecting NuGet packages and .NET assemblies.
- Build pipeline is defined in the Cake SDK file `pipeline.cs`.
- Run the default pipeline with `dotnet pipeline.cs` (restores, builds, and tests).
- Common targets: `dotnet pipeline.cs -- --target Build`, `dotnet pipeline.cs -- --target Test`, `dotnet pipeline.cs -- --target Pack`, `dotnet pipeline.cs -- --target CI --rebuild`.
- Direct commands still work when needed: build with `dotnet build Nuscope.slnx`; test with `dotnet test --project Nuscope.Cli.Tests/Nuscope.Cli.Tests.csproj`.

## CLI and test setup

- The product assembly is named `nuscope` (`Nuscope.Cli/Nuscope.Cli.csproj`) even though the namespace/project is `Nuscope.Cli`.
- Tests execute the built DLL directly with `dotnet <built nuscope.dll> inspect ...`; they do not use a globally installed tool.
- `global.json` opts into `Microsoft.Testing.Platform`; tests use TUnit, not xUnit/NUnit/MSTest.
- The test project builds the CLI under test before running.

## Test fixtures

- Integration tests generate a temporary sample package/assembly at runtime under the system temp directory.
- Do not add checked-in binary fixtures unless a test truly needs stable binary input.
- Remote package tests use an in-process fake NuGet feed via `NuGetFeed`; prefer extending that over relying on nuget.org.

## Inspection behavior

- Package inspection intentionally only scans `.dll` entries under `lib/` and `ref/` in `.nupkg` files; tools/build assets are ignored as non-API surface.
- Metadata is read with `System.Reflection.Metadata`/`PEReader` so inspected assemblies are never loaded or executed. Preserve that property when adding inspection features.
- Remote package inspection talks to NuGet V3 flat-container endpoints.

## Output and errors

- Public output is part of the contract.
- Text output is grouped by namespace/type and ordered constructor, property, method, event, field.
- JSON uses source-generated serialization in `NuscopeJsonContext` and string enum names.
- Exit codes are meaningful: usage/validation errors usually return 2, missing local files 3, invalid/non-.NET assemblies 4, and NuGet/network/source failures 5.
