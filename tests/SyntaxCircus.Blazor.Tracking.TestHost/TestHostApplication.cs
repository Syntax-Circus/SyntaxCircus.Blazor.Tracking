using Microsoft.AspNetCore.Builder;
using SyntaxCircus.Blazor.Tracking;
using SyntaxCircus.Blazor.Tracking.TestHost.Components;

namespace SyntaxCircus.Blazor.Tracking.TestHost;

public static class TestHostApplication
{
    public static async Task RunAsync(string[] args)
    {
        var app = Build(args);
        await app.RunAsync();
    }

    private static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.ClearProviders();
        builder.Services.AddRazorComponents();
        builder.Services.AddSyntaxCircusTracking(builder.Configuration);

        var app = builder.Build();
        app.UseAntiforgery();
        app.MapGet("/_content/SyntaxCircus.Blazor.Tracking/tracking.js", () => Results.Stream(GetTrackingScript(), "text/javascript"));
        app.MapRazorComponents<App>();
        return app;
    }

    private static Dictionary<string, string?> CreateSettings(string mode, string policyVersion) => new(StringComparer.Ordinal)
    {
        ["Tracking:GoogleAnalytics:Enabled"] = mode == "ga4" ? "true" : "false",
        ["Tracking:GoogleAnalytics:MeasurementId"] = "G-ABCDE123",
        ["Tracking:GoogleTagManager:Enabled"] = mode == "gtm" ? "true" : "false",
        ["Tracking:GoogleTagManager:ContainerId"] = "GTM-ABCDE123",
        ["Tracking:Umami:Enabled"] = mode == "umami" ? "true" : "false",
        ["Tracking:Umami:ScriptUrl"] = "https://analytics.example.test/tracker.js",
        ["Tracking:Umami:WebsiteId"] = "website-id",
        ["Tracking:Consent:PolicyVersion"] = policyVersion
    };

    private static Stream GetTrackingScript() => typeof(TestHostApplication).Assembly.GetManifestResourceStream("SyntaxCircus.Blazor.Tracking.TestHost.tracking.js")
        ?? throw new FileNotFoundException("The embedded tracking script is missing from the browser test host.");
}
