using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Nuscope.Cli;

internal static class PackageInspector
{
    /// <summary>
    /// Resolves the input as either a local artifact or a NuGet package, then returns the discovered assemblies and symbols.
    /// </summary>
    public static async Task<InspectionReport> InspectAsync(InspectOptions options)
    {
        if (File.Exists(options.Input))
        {
            return InspectLocalFile(Path.GetFullPath(options.Input), options);
        }

        if (LooksLikeLocalFile(options.Input))
        {
            throw new CliException($"File not found: {options.Input}", 3);
        }

        var remotePackage = await NuGetPackageDownloader.DownloadAsync(options);
        var assemblies = InspectPackage(remotePackage.DisplayName, remotePackage.Content, options);
        return new InspectionReport(remotePackage.DisplayName, assemblies);
    }

    /// <summary>
    /// Chooses the correct local inspection path for package archives, assemblies, and executables.
    /// </summary>
    private static InspectionReport InspectLocalFile(string fullPath, InspectOptions options)
    {
        var extension = Path.GetExtension(fullPath);
        var assemblies = extension.Equals(".nupkg", StringComparison.OrdinalIgnoreCase)
            ? InspectPackage(fullPath, options)
            : extension.Equals(".dll", StringComparison.OrdinalIgnoreCase) || extension.Equals(".exe", StringComparison.OrdinalIgnoreCase)
                ? [InspectAssembly(fullPath, fullPath, options, targetFramework: null, assetKind: null)]
                : throw new CliException("Input must be a .dll, .exe, or .nupkg file.", 2);

        return new InspectionReport(fullPath, assemblies);
    }

    /// <summary>
    /// Detects path-like input so missing local files are reported instead of treated as package IDs.
    /// </summary>
    private static bool LooksLikeLocalFile(string input) =>
        Path.IsPathRooted(input)
        || input.Contains(Path.DirectorySeparatorChar)
        || input.Contains(Path.AltDirectorySeparatorChar)
        || Path.GetExtension(input).Equals(".dll", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(input).Equals(".exe", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(input).Equals(".nupkg", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Opens a local NuGet package file and scans its candidate assemblies.
    /// </summary>
    private static IReadOnlyList<AssemblyReport> InspectPackage(string packagePath, InspectOptions options)
    {
        using var archive = ZipFile.OpenRead(packagePath);
        return InspectPackage(packagePath, archive, options);
    }

    /// <summary>
    /// Opens a downloaded NuGet package stream and scans its candidate assemblies.
    /// </summary>
    private static IReadOnlyList<AssemblyReport> InspectPackage(string packageName, Stream packageStream, InspectOptions options)
    {
        using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read, leaveOpen: false);
        return InspectPackage(packageName, archive, options);
    }

    /// <summary>
    /// Walks a package archive, selecting lib/ref assemblies and producing one report per assembly.
    /// </summary>
    private static IReadOnlyList<AssemblyReport> InspectPackage(string packageName, ZipArchive archive, InspectOptions options)
    {
        var reports = new List<AssemblyReport>();
        foreach (var entry in archive.Entries.OrderBy(e => e.FullName, StringComparer.OrdinalIgnoreCase))
        {
            // NuGet packages can contain build assets and tools; lib/ref assemblies are the public API surface.
            if (!entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || !IsCandidateAssemblyEntry(entry.FullName))
            {
                continue;
            }

            var asset = GetPackageAsset(entry.FullName);
            if (options.TargetFramework is not null
                && !string.Equals(asset.TargetFramework, options.TargetFramework, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using var stream = entry.Open();
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            memory.Position = 0;
            reports.Add(InspectAssembly($"{packageName}:{entry.FullName}", memory, options, asset.TargetFramework, asset.AssetKind));
        }

        return reports;
    }

    /// <summary>
    /// Keeps only package entries that normally represent the consumable API surface.
    /// </summary>
    private static bool IsCandidateAssemblyEntry(string name) =>
        name.StartsWith("lib/", StringComparison.OrdinalIgnoreCase)
        || name.StartsWith("ref/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Extracts the NuGet asset kind and target framework from entries such as lib/net8.0/Foo.dll.
    /// </summary>
    private static (string? AssetKind, string? TargetFramework) GetPackageAsset(string entryName)
    {
        var parts = entryName.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 3 ? (parts[0], parts[1]) : (null, null);
    }

    /// <summary>
    /// Opens an assembly file from disk and delegates metadata inspection to the stream-based reader.
    /// </summary>
    private static AssemblyReport InspectAssembly(string displayPath, string filePath, InspectOptions options, string? targetFramework, string? assetKind)
    {
        using var stream = File.OpenRead(filePath);
        return InspectAssembly(displayPath, stream, options, targetFramework, assetKind);
    }

    /// <summary>
    /// Reads .NET metadata from an assembly stream and collects matching type and member symbols.
    /// </summary>
    private static AssemblyReport InspectAssembly(string displayPath, Stream stream, InspectOptions options, string? targetFramework, string? assetKind)
    {
        // PEReader lets us inspect assembly metadata without loading user code into the current process.
        using var peReader = new PEReader(stream, PEStreamOptions.PrefetchMetadata);
        if (!peReader.HasMetadata)
        {
            throw new CliException($"{displayPath} does not contain .NET metadata.", 4);
        }

        var reader = peReader.GetMetadataReader();
        var assemblyName = reader.IsAssembly
            ? reader.GetString(reader.GetAssemblyDefinition().Name)
            : Path.GetFileNameWithoutExtension(displayPath);
        var formatter = new SignatureFormatter(reader);
        var symbols = new List<SymbolInfo>();

        foreach (var typeHandle in reader.TypeDefinitions)
        {
            var type = reader.GetTypeDefinition(typeHandle);
            var typeName = MetadataNames.GetTypeName(reader, type);
            if (typeName == "<Module>")
            {
                continue;
            }

            var typeVisibility = Visibility.FromTypeAttributes(type.Attributes);
            if (!options.IncludeNonPublic && typeVisibility != "public")
            {
                continue;
            }

            var baseType = formatter.FormatBaseType(type);
            var typeShape = TypeShape.FromAttributes(type.Attributes, baseType);
            AddIfMatched(symbols, options, new SymbolInfo(
                SymbolKind.Type,
                typeName,
                typeShape.Classification,
                typeVisibility,
                formatter.FormatTypeDefinition(type, typeVisibility, typeShape),
                null,
                baseType,
                displayPath,
                typeShape.Kind,
                typeShape.Modifiers));

            AddFields(reader, formatter, options, symbols, type, typeName, displayPath);
            AddProperties(reader, formatter, options, symbols, type, typeName, displayPath);
            AddEvents(reader, formatter, options, symbols, type, typeName, displayPath);
            AddMethods(reader, formatter, options, symbols, type, typeName, displayPath);
        }

        return new AssemblyReport(displayPath, assemblyName, targetFramework, assetKind, symbols);
    }

    /// <summary>
    /// Adds visible fields declared by a type, subject to the active kind and search filters.
    /// </summary>
    private static void AddFields(
        MetadataReader reader,
        SignatureFormatter formatter,
        InspectOptions options,
        List<SymbolInfo> symbols,
        TypeDefinition type,
        string typeName,
        string displayPath)
    {
        foreach (var fieldHandle in type.GetFields())
        {
            var field = reader.GetFieldDefinition(fieldHandle);
            var visibility = Visibility.FromFieldAttributes(field.Attributes);
            if (!options.IncludeNonPublic && visibility != "public")
            {
                continue;
            }

            var fieldName = reader.GetString(field.Name);
            AddIfMatched(symbols, options, new SymbolInfo(
                SymbolKind.Field,
                $"{typeName}.{fieldName}",
                "field",
                visibility,
                formatter.FormatField(field),
                null,
                typeName,
                displayPath));
        }
    }

    /// <summary>
    /// Adds visible properties declared by a type, deriving visibility from their accessors.
    /// </summary>
    private static void AddProperties(
        MetadataReader reader,
        SignatureFormatter formatter,
        InspectOptions options,
        List<SymbolInfo> symbols,
        TypeDefinition type,
        string typeName,
        string displayPath)
    {
        foreach (var propertyHandle in type.GetProperties())
        {
            var property = reader.GetPropertyDefinition(propertyHandle);
            var propertyName = reader.GetString(property.Name);
            var visibility = Visibility.FromAccessor(reader, property.GetAccessors());
            if (!options.IncludeNonPublic && visibility != "public")
            {
                continue;
            }

            AddIfMatched(symbols, options, new SymbolInfo(
                SymbolKind.Property,
                $"{typeName}.{propertyName}",
                "property",
                visibility,
                formatter.FormatProperty(property),
                null,
                typeName,
                displayPath));
        }
    }

    /// <summary>
    /// Adds visible events declared by a type, deriving visibility from their accessor methods.
    /// </summary>
    private static void AddEvents(
        MetadataReader reader,
        SignatureFormatter formatter,
        InspectOptions options,
        List<SymbolInfo> symbols,
        TypeDefinition type,
        string typeName,
        string displayPath)
    {
        foreach (var eventHandle in type.GetEvents())
        {
            var eventDef = reader.GetEventDefinition(eventHandle);
            var eventName = reader.GetString(eventDef.Name);
            var visibility = Visibility.FromAccessor(reader, eventDef.GetAccessors());
            if (!options.IncludeNonPublic && visibility != "public")
            {
                continue;
            }

            AddIfMatched(symbols, options, new SymbolInfo(
                SymbolKind.Event,
                $"{typeName}.{eventName}",
                "event",
                visibility,
                formatter.FormatEvent(eventDef),
                null,
                typeName,
                displayPath));
        }
    }

    /// <summary>
    /// Adds visible constructors and methods, skipping property/event accessors emitted as special-name methods.
    /// </summary>
    private static void AddMethods(
        MetadataReader reader,
        SignatureFormatter formatter,
        InspectOptions options,
        List<SymbolInfo> symbols,
        TypeDefinition type,
        string typeName,
        string displayPath)
    {
        foreach (var methodHandle in type.GetMethods())
        {
            var method = reader.GetMethodDefinition(methodHandle);
            var methodName = reader.GetString(method.Name);
            var kind = methodName == ".ctor" || methodName == ".cctor" ? SymbolKind.Constructor : SymbolKind.Method;
            if (kind == SymbolKind.Method && method.Attributes.HasFlag(MethodAttributes.SpecialName))
            {
                continue;
            }

            var visibility = Visibility.FromMethodAttributes(method.Attributes);
            if (!options.IncludeNonPublic && visibility != "public")
            {
                continue;
            }

            AddIfMatched(symbols, options, new SymbolInfo(
                kind,
                $"{typeName}.{(kind == SymbolKind.Constructor ? MetadataNames.GetShortName(typeName) : methodName)}",
                kind == SymbolKind.Constructor ? "constructor" : "method",
                visibility,
                formatter.FormatMethod(method),
                null,
                typeName,
                displayPath));
        }
    }

    /// <summary>
    /// Applies the shared kind and search filters before appending a discovered symbol.
    /// </summary>
    private static void AddIfMatched(List<SymbolInfo> symbols, InspectOptions options, SymbolInfo symbol)
    {
        if (options.Kind is { } kind && symbol.Kind != kind)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(options.Search)
            && !symbol.Matches(options.Search))
        {
            return;
        }

        symbols.Add(symbol);
    }
}
