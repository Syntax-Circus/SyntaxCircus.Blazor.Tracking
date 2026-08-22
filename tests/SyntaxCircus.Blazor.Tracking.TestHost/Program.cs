using SyntaxCircus.Blazor.Tracking;
using SyntaxCircus.Blazor.Tracking.TestHost.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddRazorComponents();
builder.Services.AddSyntaxCircusTracking(builder.Configuration);

var app = builder.Build();
app.UseAntiforgery();
app.MapGet("/_content/SyntaxCircus.Blazor.Tracking/tracking.js", () => Results.File(Path.Combine(FindRepositoryRoot(), "src", "SyntaxCircus.Blazor.Tracking", "wwwroot", "tracking.js"), "text/javascript"));
app.MapRazorComponents<App>();
app.Run();

static string FindRepositoryRoot()
{
    for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
    {
        if (File.Exists(Path.Combine(directory.FullName, "SyntaxCircus.Blazor.Tracking.slnx")))
        {
            return directory.FullName;
        }
    }

    throw new DirectoryNotFoundException("Unable to locate the repository root from the test-host output directory.");
}
