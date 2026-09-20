using Microsoft.Playwright;
using Shouldly;
using Xunit;

namespace SyntaxCircus.Blazor.Tracking.BrowserTests;

public sealed class UmamiTrackingBrowserTests
{
    private const string TrackerUrl = "https://analytics.example.test/**";

    [Fact]
    public async Task UmamiLoadsWithoutConsentAndShowsNoBannerWhenConsentIsNotRequired()
    {
        await using var host = await BrowserTestHost.StartAsync("umami", umamiRequireConsent: false);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();
        var trackerRequests = await RouteTrackerAsync(page);

        await page.GotoAsync(host.Address.ToString());
        await page.WaitForTimeoutAsync(200);

        trackerRequests.ShouldHaveSingleItem();
        (await page.Locator("[data-privacy-banner]").IsVisibleAsync()).ShouldBeFalse();
        (await page.Locator("script[data-website-id]").GetAttributeAsync("data-do-not-track")).ShouldBe("true");
        (await page.Context.CookiesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task DoNotTrackAttributeIsOmittedWhenDisabled()
    {
        await using var host = await BrowserTestHost.StartAsync("umami", umamiRequireConsent: false, umamiRespectDoNotTrack: false);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();
        await RouteTrackerAsync(page);

        await page.GotoAsync(host.Address.ToString());
        await page.WaitForTimeoutAsync(200);

        (await page.Locator("script[data-website-id]").GetAttributeAsync("data-do-not-track")).ShouldBeNull();
    }

    [Fact]
    public async Task UmamiWaitsForAnalyticsConsentWhenConsentIsRequired()
    {
        await using var host = await BrowserTestHost.StartAsync("umami");
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();
        var trackerRequests = await RouteTrackerAsync(page);

        await page.GotoAsync(host.Address.ToString());
        await page.WaitForTimeoutAsync(200);
        (await page.Locator("[data-privacy-banner]").IsVisibleAsync()).ShouldBeTrue();
        trackerRequests.ShouldBeEmpty();

        await page.Locator("[data-privacy-action='accept-all']").ClickAsync();
        await page.WaitForTimeoutAsync(200);
        trackerRequests.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task RejectingConsentKeepsUmamiOffAfterReload()
    {
        await using var host = await BrowserTestHost.StartAsync("umami", umamiRequireConsent: true);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();
        var trackerRequests = await RouteTrackerAsync(page);

        await page.GotoAsync(host.Address.ToString());
        await page.Locator("[data-privacy-action='reject-all']").ClickAsync();
        await page.ReloadAsync();
        await page.WaitForTimeoutAsync(200);

        trackerRequests.ShouldBeEmpty();
    }

    private static async Task<List<string>> RouteTrackerAsync(IPage page)
    {
        var requests = new List<string>();
        await page.RouteAsync(TrackerUrl, async route =>
        {
            requests.Add(route.Request.Url);
            await route.FulfillAsync(new RouteFulfillOptions { ContentType = "application/javascript", Body = "" });
        });
        return requests;
    }
}
