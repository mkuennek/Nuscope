---
name: nuscope
description: Inspect NuGet packages and .NET assemblies with the Nuscope CLI. Use when an agent needs to discover public API shape, symbols, signatures, or package assembly contents without loading binaries.
---

# Nuscope

Use `nuscope inspect` to query the API surface of a `.nupkg`, `.dll`, or remote NuGet package. Agents should prefer `--format json` so results can be filtered with tools such as `jq` or consumed by scripts.

Nuscope reads metadata only; it does not load or execute inspected assemblies.

## JSON format

JSON uses PascalCase property names and string enum values:

```json
{
  "InputPath": "Newtonsoft.Json",
  "Assemblies": [
    {
      "Path": "lib/netstandard2.0/Newtonsoft.Json.dll",
      "AssemblyName": "Newtonsoft.Json",
      "TargetFramework": "netstandard2.0",
      "AssetKind": "lib",
      "Symbols": [
        {
          "Kind": "Type",
          "Name": "Newtonsoft.Json.JsonConvert",
          "Classification": "static class",
          "Visibility": "public",
          "Signature": "public static class Newtonsoft.Json.JsonConvert",
          "Documentation": "Provides methods for converting between .NET types and JSON types.",
          "DeclaringType": "System.Object",
          "AssemblyPath": "lib/netstandard2.0/Newtonsoft.Json.dll",
          "TypeKind": "Class",
          "Modifiers": ["static"]
        }
      ]
    }
  ]
}
```

`Kind` is one of `Type`, `Method`, `Property`, `Field`, `Event`, or `Constructor`. JSON omits nullable fields when no value is available. `Documentation` is included when XML documentation comments are available for a symbol.

Important JSON details for agents:

- Values are case-sensitive. Current `Visibility` values are lower-case, e.g. `public`.
- Type symbols include a declaration `Signature`, a human-readable `Classification`, a machine-readable `TypeKind` (`Class`, `Interface`, `Struct`, `Enum`, or `Delegate`), and `Modifiers` such as `static`, `sealed`, or `abstract`.
- To answer "classes", prefer `TypeKind == "Class"` instead of parsing `Classification`.
- NuGet package assembly entries include `TargetFramework` and `AssetKind`; use `--tfm <target-framework>` to inspect a single TFM and avoid duplicate symbols across target frameworks.
- Symbols may include `Documentation` extracted from adjacent/package XML documentation files; absence means no XML summary was found.

## When to use

- Discover types, methods, properties, fields, events, and constructors in .NET assemblies.
- Inspect a local NuGet package or DLL before writing integration code.
- Inspect a remote NuGet package by ID and optional version.
- Search package API surface for a symbol or signature fragment.

## Common invocations

```sh
# Local package, JSON output
nuscope inspect ./artifacts/packages/My.Package.1.0.0.nupkg --format json

# Local assembly, JSON output
nuscope inspect ./bin/Debug/net10.0/My.Library.dll --format json

# Remote NuGet package, fixed version
nuscope inspect Newtonsoft.Json --version 13.0.3 --format json

# Search for a type
nuscope inspect Microsoft.Extensions.Logging --search ILogger --kind type --format json

# Search methods in a local package
nuscope inspect ./My.Package.nupkg --search HttpClient --kind method --format json

# List public type declarations from one target framework
nuscope inspect Newtonsoft.Json --version 13.0.3 --kind type --tfm netstandard2.0 --format json \
  | jq -r '.Assemblies[].Symbols[] | select(.Visibility == "public") | .Signature'

# List public classes, including static/sealed/abstract classes
nuscope inspect Newtonsoft.Json --version 13.0.3 --kind type --tfm netstandard2.0 --format json \
  | jq -r '.Assemblies[].Symbols[] | select(.Visibility == "public" and .TypeKind == "Class") | .Signature'

# Include assembly context while filtering symbols. After `.Assemblies[] as $a`, read symbols from `$a.Symbols[]`, not `.Symbols[]`.
nuscope inspect Newtonsoft.Json --version 13.0.3 --format json \
  | jq -r '.Assemblies[] as $a | $a.Symbols[] | select(.Kind == "Method") | "\($a.AssemblyName)\t\(.Signature)"'

# Include non-public API when needed
nuscope inspect ./My.Library.dll --include-non-public --format json
```

## Options

- `--format text|json`: output format; default is `text`.
- `--search <term>`: case-insensitive match over symbol names, signatures, and documentation.
- `--kind <type|method|property|field|event|constructor>`: restrict symbols by kind.
- `--tfm <target-framework>`: for NuGet packages, inspect only assemblies for one target framework, such as `net8.0` or `netstandard2.0`.
- `--include-non-public`: include non-public types and members.
- `--version <version>`: inspect a specific NuGet package version.
- `--prerelease`: allow latest prerelease when no version is specified.
- `--source <url>`: use a specific NuGet V3 service index.

## Agent guidance

Start broad, then narrow with `--search`, `--kind`, and `--tfm` if output is large. Use explicit `--version` for reproducible answers. For remote package failures, retry with `--source` if the package is hosted on a private or custom NuGet feed.
