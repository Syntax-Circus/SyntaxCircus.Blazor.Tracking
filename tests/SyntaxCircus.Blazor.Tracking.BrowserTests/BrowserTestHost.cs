using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using SyntaxCircus.Blazor.Tracking.TestHost;

namespace SyntaxCircus.Blazor.Tracking.BrowserTests;

internal sealed class BrowserTestHost : IAsyncDisposable
{
    private readonly Process process;

    private BrowserTestHost(Process process, Uri address, Task<string> standardOutput, Task<string> standardError)
    {
        this.process = process;
        Address = address;
        StandardOutput = standardOutput;
        StandardError = standardError;
    }

    public Uri Address { get; }

    private Task<string> StandardOutput { get; }

    private Task<string> StandardError { get; }

    public static async Task<BrowserTestHost> StartAsync(string mode, string policyVersion = "1", bool umamiRequireConsent = true, bool umamiRespectDoNotTrack = true)
    {
        var port = GetAvailablePort();
        var address = new Uri($"http://127.0.0.1:{port}");
        var hostAssembly = typeof(TestHostAssemblyMarker).Assembly.Location;
        var startInfo = new ProcessStartInfo("dotnet", $"\"{hostAssembly}\" --urls {address}")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.Environment["Tracking__GoogleAnalytics__Enabled"] = mode == "ga4" ? "true" : "false";
        startInfo.Environment["Tracking__GoogleAnalytics__MeasurementId"] = "G-ABCDE123";
        startInfo.Environment["Tracking__GoogleTagManager__Enabled"] = mode == "gtm" ? "true" : "false";
        startInfo.Environment["Tracking__GoogleTagManager__ContainerId"] = "GTM-ABCDE123";
        startInfo.Environment["Tracking__Umami__Enabled"] = mode == "umami" ? "true" : "false";
        startInfo.Environment["Tracking__Umami__ScriptUrl"] = "https://analytics.example.test/tracker.js";
        startInfo.Environment["Tracking__Umami__WebsiteId"] = "website-id";
        startInfo.Environment["Tracking__Umami__RequireConsent"] = umamiRequireConsent ? "true" : "false";
        startInfo.Environment["Tracking__Umami__RespectDoNotTrack"] = umamiRespectDoNotTrack ? "true" : "false";
        startInfo.Environment["Tracking__Consent__PolicyVersion"] = policyVersion;

        var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start the browser test host.");
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        var deadline = DateTimeOffset.UtcNow.AddSeconds(15);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException($"The browser test host exited with code {process.ExitCode}.{await GetHostOutputAsync(standardOutput, standardError)}");
            }

            try
            {
                using var response = await client.GetAsync(address);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return new BrowserTestHost(process, address, standardOutput, standardError);
                }
            }
            catch (HttpRequestException)
            {
            }

            await Task.Delay(100);
        }

        if (process.HasExited)
        {
            throw new InvalidOperationException($"The browser test host exited with code {process.ExitCode}.{await GetHostOutputAsync(standardOutput, standardError)}");
        }

        process.Kill(entireProcessTree: true);
        await process.WaitForExitAsync();
        throw new TimeoutException($"The browser test host did not start within 15 seconds.{await GetHostOutputAsync(standardOutput, standardError)}");
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

    private static async Task<string> GetHostOutputAsync(Task<string> standardOutput, Task<string> standardError)
    {
        var output = await standardOutput;
        var error = await standardError;
        return string.IsNullOrWhiteSpace(output) && string.IsNullOrWhiteSpace(error)
            ? string.Empty
            : $"{Environment.NewLine}Host output:{Environment.NewLine}{output}{error}";
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
