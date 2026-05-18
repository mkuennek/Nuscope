using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Nuscope.Cli.Tests;

internal sealed class NuGetFeed : IAsyncDisposable
{
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Task _server;
    private readonly byte[] _packageBytes;

    private NuGetFeed(HttpListener listener, string serviceIndexUrl, byte[] packageBytes)
    {
        _listener = listener;
        ServiceIndexUrl = serviceIndexUrl;
        _packageBytes = packageBytes;
        _server = Task.Run(ServeAsync);
    }

    /// <summary>
    /// Gets the local NuGet v3 service index URL passed to the CLI under test.
    /// </summary>
    public string ServiceIndexUrl { get; }

    /// <summary>
    /// Starts an in-process NuGet feed that serves the generated sample package.
    /// </summary>
    public static async Task<NuGetFeed> StartAsync(SampleArtifacts sample)
    {
        var port = GetFreePort();
        var prefix = $"http://127.0.0.1:{port}/";
        var listener = new HttpListener();
        listener.Prefixes.Add(prefix);
        listener.Start();

        var packageBytes = await File.ReadAllBytesAsync(sample.PackagePath);
        return new NuGetFeed(listener, $"{prefix}v3/index.json", packageBytes);
    }

    /// <summary>
    /// Stops the listener and waits for the background request loop to exit.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _cancellation.CancelAsync();
        _listener.Close();

        try
        {
            await _server;
        }
        catch (ObjectDisposedException)
        {
        }
        catch (HttpListenerException)
        {
        }
    }

    /// <summary>
    /// Accepts incoming HTTP requests until the test feed is disposed.
    /// </summary>
    private async Task ServeAsync()
    {
        while (!_cancellation.IsCancellationRequested)
        {
            var context = await _listener.GetContextAsync();

            // Each request is independent and small, so fire-and-forget keeps the listener responsive.
            _ = Task.Run(() => RespondAsync(context));
        }
    }

    /// <summary>
    /// Responds with the minimal NuGet v3 endpoints required for package resolution and download.
    /// </summary>
    private async Task RespondAsync(HttpListenerContext context)
    {
        var path = context.Request.Url?.AbsolutePath ?? string.Empty;
        if (path.Equals("/v3/index.json", StringComparison.OrdinalIgnoreCase))
        {
            var baseAddress = $"{context.Request.Url!.GetLeftPart(UriPartial.Authority)}/v3-flatcontainer/";
            await WriteJsonAsync(context, $$"""
                {
                  "version": "3.0.0",
                  "resources": [
                    {
                      "@id": "{{baseAddress}}",
                      "@type": "PackageBaseAddress/3.0.0"
                    }
                  ]
                }
                """);
            return;
        }

        if (path.Equals("/v3-flatcontainer/nuscope.sample/index.json", StringComparison.OrdinalIgnoreCase))
        {
            await WriteJsonAsync(context, """
                {
                  "versions": [
                    "1.0.0"
                  ]
                }
                """);
            return;
        }

        if (path.Equals("/v3-flatcontainer/nuscope.sample/1.0.0/nuscope.sample.1.0.0.nupkg", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.ContentType = "application/octet-stream";
            context.Response.ContentLength64 = _packageBytes.Length;
            await context.Response.OutputStream.WriteAsync(_packageBytes);
            context.Response.Close();
            return;
        }

        context.Response.StatusCode = 404;
        context.Response.Close();
    }

    /// <summary>
    /// Writes a JSON response and closes the request.
    /// </summary>
    private static async Task WriteJsonAsync(HttpListenerContext context, string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes);
        context.Response.Close();
    }

    /// <summary>
    /// Reserves an available loopback port for the local feed before the HTTP listener starts.
    /// </summary>
    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
