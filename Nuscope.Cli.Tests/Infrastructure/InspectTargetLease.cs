namespace Nuscope.Cli.Tests;

public sealed class InspectTargetLease : IAsyncDisposable
{
    private readonly IAsyncDisposable? _resource;

    internal InspectTargetLease(InspectTarget target, IAsyncDisposable? resource = null)
    {
        BaseArgs = target.BaseArgs;
        Name = target.Name;
        _resource = resource;
    }

    public string[] BaseArgs { get; }

    public string Name { get; }

    /// <summary>
    /// Releases any target-specific resource, such as the local NuGet feed used for remote package tests.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_resource is not null)
        {
            await _resource.DisposeAsync();
        }
    }
}
