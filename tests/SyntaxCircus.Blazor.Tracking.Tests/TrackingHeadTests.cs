using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using SyntaxCircus.Blazor.Tracking.Components;
using Xunit;

namespace SyntaxCircus.Blazor.Tracking.Tests;

public sealed class TrackingHeadTests
{
    [Fact]
    public void RendersConfigurationWithoutHardCodingProviderValues()
    {
        using var context = new BunitContext();
        context.Services.AddSingleton<IOptions<TrackingOptions>>(Options.Create(new TrackingOptions
        {
            Umami = new UmamiOptions
            {
                Enabled = true,
                ScriptUrl = "https://analytics.example/script.js",
                WebsiteId = "website-id"
            }
        }));

        var markup = context.Render<TrackingHead>().Markup;

        markup.ShouldContain("https://analytics.example/script.js");
        markup.ShouldContain("website-id");
        markup.ShouldContain("_content/SyntaxCircus.Blazor.Tracking/tracking.js");
        markup.ShouldNotContain("&quot;");
    }
}
