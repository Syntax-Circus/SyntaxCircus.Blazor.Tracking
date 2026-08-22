using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using SyntaxCircus.Blazor.Tracking.Components;
using Xunit;

namespace SyntaxCircus.Blazor.Tracking.Tests;

public sealed class ConsentBannerTests
{
    [Fact]
    public void RendersAccessibleDefaultMarkup()
    {
        using var context = new BunitContext();
        context.Services.AddSingleton<IOptions<TrackingOptions>>(Options.Create(new TrackingOptions
        {
            Consent = new ConsentOptions { PrivacyPolicyUrl = "/privacy" }
        }));
        var cut = context.Render<ConsentBanner>();

        cut.Find("[data-privacy-banner]").GetAttribute("role").ShouldBe("dialog");
        cut.Markup.ShouldContain("Accept optional cookies");
        cut.Markup.ShouldContain("Reject optional cookies");
        cut.Markup.ShouldContain("href=\"/privacy\"");
    }

    [Fact]
    public void ChildContentReplacesDefaultMarkup()
    {
        using var context = new BunitContext();
        context.Services.AddSingleton<IOptions<TrackingOptions>>(Options.Create(new TrackingOptions()));
        RenderFragment content = builder => builder.AddMarkupContent(0, "<aside data-privacy-banner><button data-privacy-action=\"accept-all\">Allow</button></aside>");
        var cut = context.Render<ConsentBanner>(parameters => parameters.Add(component => component.ChildContent, content));

        cut.Markup.ShouldContain("Allow");
        cut.Markup.ShouldNotContain("Accept optional cookies");
    }
}
