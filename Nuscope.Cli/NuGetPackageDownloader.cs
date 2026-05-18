using System.Text.Json;

namespace Nuscope.Cli;

internal static class NuGetPackageDownloader
{
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(60)
    };

    /// <summary>
    /// Resolves a package ID and version against a NuGet v3 source, then downloads the package into memory.
    /// </summary>
    public static async Task<DownloadedPackage> DownloadAsync(InspectOptions options)
    {
        var packageId = options.Input.Trim();
        if (string.IsNullOrWhiteSpace(packageId) || packageId.Contains(' '))
        {
            throw new CliException($"Invalid NuGet package ID: {options.Input}", 2);
        }

        var source = NormalizeSource(options.Source);
        var baseAddress = await GetPackageBaseAddressAsync(source);
        var lowerId = packageId.ToLowerInvariant();
        var version = options.Version?.ToLowerInvariant() ?? await GetLatestVersionAsync(baseAddress, lowerId, options.IncludePrerelease);
        var packageUri = new Uri(baseAddress, $"{Uri.EscapeDataString(lowerId)}/{Uri.EscapeDataString(version)}/{Uri.EscapeDataString(lowerId)}.{Uri.EscapeDataString(version)}.nupkg");

        using var response = await Client.GetAsync(packageUri, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
        {
            throw new CliException($"NuGet package download failed for {packageId} {version}: {(int)response.StatusCode} {response.ReasonPhrase}", 5);
        }

        var memory = new MemoryStream();
        await response.Content.CopyToAsync(memory);
        memory.Position = 0;
        return new DownloadedPackage($"nuget:{packageId}/{version}", memory);
    }

    /// <summary>
    /// Validates the configured package source and converts it to an absolute URI.
    /// </summary>
    private static Uri NormalizeSource(string source)
    {
        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri))
        {
            throw new CliException($"Invalid NuGet source URL: {source}", 2);
        }

        return uri;
    }

    /// <summary>
    /// Reads the NuGet service index and returns the flat-container PackageBaseAddress endpoint.
    /// </summary>
    private static async Task<Uri> GetPackageBaseAddressAsync(Uri serviceIndexUri)
    {
        using var document = await GetJsonAsync(serviceIndexUri);
        if (!document.RootElement.TryGetProperty("resources", out var resources) || resources.ValueKind != JsonValueKind.Array)
        {
            throw new CliException($"NuGet source does not expose a resources array: {serviceIndexUri}", 5);
        }

        foreach (var resource in resources.EnumerateArray())
        {
            if (!resource.TryGetProperty("@type", out var typeElement)
                || !resource.TryGetProperty("@id", out var idElement))
            {
                continue;
            }

            var type = typeElement.GetString();
            var id = idElement.GetString();
            if (type is not null
                && id is not null
                && type.StartsWith("PackageBaseAddress/", StringComparison.OrdinalIgnoreCase)
                && Uri.TryCreate(id, UriKind.Absolute, out var resourceUri))
            {
                return EnsureTrailingSlash(resourceUri);
            }
        }

        throw new CliException($"NuGet source does not expose PackageBaseAddress: {serviceIndexUri}", 5);
    }

    /// <summary>
    /// Reads the package version index and selects the latest available version allowed by prerelease settings.
    /// </summary>
    private static async Task<string> GetLatestVersionAsync(Uri baseAddress, string lowerId, bool includePrerelease)
    {
        var versionsUri = new Uri(baseAddress, $"{Uri.EscapeDataString(lowerId)}/index.json");
        using var document = await GetJsonAsync(versionsUri);
        if (!document.RootElement.TryGetProperty("versions", out var versionsElement) || versionsElement.ValueKind != JsonValueKind.Array)
        {
            throw new CliException($"NuGet package has no versions index: {versionsUri}", 5);
        }

        string? latest = null;
        foreach (var versionElement in versionsElement.EnumerateArray())
        {
            var version = versionElement.GetString();
            if (string.IsNullOrWhiteSpace(version))
            {
                continue;
            }

            if (!includePrerelease && version.Contains('-', StringComparison.Ordinal))
            {
                continue;
            }

            latest = version;
        }

        if (latest is null)
        {
            throw new CliException($"NuGet package '{lowerId}' has no {(includePrerelease ? string.Empty : "stable ")}versions.", 5);
        }

        return latest;
    }

    /// <summary>
    /// Performs a NuGet HTTP request and parses a successful JSON response.
    /// </summary>
    private static async Task<JsonDocument> GetJsonAsync(Uri uri)
    {
        using var response = await Client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
        {
            throw new CliException($"NuGet request failed for {uri}: {(int)response.StatusCode} {response.ReasonPhrase}", 5);
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    /// <summary>
    /// Normalizes endpoint URIs so later relative URI construction appends path segments correctly.
    /// </summary>
    private static Uri EnsureTrailingSlash(Uri uri)
    {
        var value = uri.ToString();
        return value.EndsWith("/", StringComparison.Ordinal) ? uri : new Uri(value + "/");
    }
}
