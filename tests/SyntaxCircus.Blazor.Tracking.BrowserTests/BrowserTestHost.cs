using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace SyntaxCircus.Blazor.Tracking.BrowserTests;

internal sealed class BrowserTestHost : IAsyncDisposable
{
    private readonly Process process;

    private BrowserTestHost(Process process, Uri address)
    {
        this.process = process;
        Address = address;
    }

    public Uri Address { get; }

    public static async Task<BrowserTestHost> StartAsync(string mode, string policyVersion = "1")
    {
        var port = GetAvailablePort();
        var hostAssembly = Path.Combine(FindRepositoryRoot(), "tests", "SyntaxCircus.Blazor.Tracking.TestHost", "bin", "Release", "net10.0", "SyntaxCircus.Blazor.Tracking.TestHost.dll");
        if (!File.Exists(hostAssembly))
        {
            throw new FileNotFoundException("The test host assembly was not copied to the browser-test output directory.", hostAssembly);
        }

        var startInfo = new ProcessStartInfo("dotnet", $"\"{hostAssembly}\" --urls http://127.0.0.1:{port}")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.Environment["Tracking__GoogleAnalytics__Enabled"] = mode == "ga4" ? "true" : "false";
        startInfo.Environment["Tracking__GoogleAnalytics__MeasurementId"] = "G-ABCDE123";
        startInfo.Environment["Tracking__GoogleTagManager__Enabled"] = mode == "gtm" ? "true" : "false";
        startInfo.Environment["Tracking__GoogleTagManager__ContainerId"] = "GTM-ABCDE123";
        startInfo.Environment["Tracking__Umami__Enabled"] = mode == "umami" ? "true" : "false";
        startInfo.Environment["Tracking__Umami__ScriptUrl"] = "https://analytics.example.test/tracker.js";
        startInfo.Environment["Tracking__Umami__WebsiteId"] = "website-id";
        startInfo.Environment["Tracking__Consent__PolicyVersion"] = policyVersion;

        var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start the browser test host.");
        var address = new Uri($"http://127.0.0.1:{port}");
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        var deadline = DateTimeOffset.UtcNow.AddSeconds(15);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException($"The browser test host exited with code {process.ExitCode}.");
            }

            try
            {
                using var response = await client.GetAsync(address);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return new BrowserTestHost(process, address);
                }
            }
            catch (HttpRequestException)
            {
            }

            await Task.Delay(100);
        }

        process.Kill(entireProcessTree: true);
        await process.WaitForExitAsync();
        throw new TimeoutException("The browser test host did not start within 15 seconds.");
    }

    public ValueTask DisposeAsync()
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
        }

        process.Dispose();
        return ValueTask.CompletedTask;
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SyntaxCircus.Blazor.Tracking.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Unable to locate the repository root from the browser-test output directory.");
    }
}
