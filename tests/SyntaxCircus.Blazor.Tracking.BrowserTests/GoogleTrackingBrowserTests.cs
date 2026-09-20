using Microsoft.Playwright;
using Shouldly;
using Xunit;

namespace SyntaxCircus.Blazor.Tracking.BrowserTests;

public sealed class GoogleTrackingBrowserTests
{
    [Fact]
    public async Task DirectGa4WaitsForAnalyticsConsentAndRemovesCookiesOnRevocation()
    {
        await using var host = await BrowserTestHost.StartAsync("ga4");
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();
        var googleRequests = new List<string>();
        await page.RouteAsync("https://www.googletagmanager.com/**", async route =>
        {
            googleRequests.Add(route.Request.Url);
            await route.FulfillAsync(new RouteFulfillOptions
            {
                ContentType = "application/javascript",
                Body = "document.cookie='_ga=analytics; Path=/; SameSite=Lax';document.cookie='_gcl_au=marketing; Path=/; SameSite=Lax';"
            });
        });

        await page.GotoAsync(host.Address.ToString());
        (await page.Locator("[data-privacy-banner]").IsVisibleAsync()).ShouldBeTrue();
        googleRequests.ShouldBeEmpty();

        await page.Locator("[data-privacy-action='accept-all']").ClickAsync();
        await page.WaitForTimeoutAsync(100);
        googleRequests.ShouldHaveSingleItem().ShouldContain("/gtag/js?id=G-ABCDE123");

        await page.Locator("[data-privacy-settings-link]").ClickAsync();
        await page.Locator("[data-privacy-category='analytics']").UncheckAsync();
        await page.Locator("[data-privacy-category='marketing']").UncheckAsync();
        await page.Locator("[data-privacy-action='save-settings']").ClickAsync();
        await page.WaitForTimeoutAsync(100);

        var cookies = await page.Context.CookiesAsync();
        cookies.ShouldNotContain(cookie => cookie.Name == "_ga" || cookie.Name == "_gcl_au");
        var dataLayer = await page.EvaluateAsync<string>("JSON.stringify(window.dataLayer)");
        dataLayer.IndexOf("\"default\"", StringComparison.Ordinal).ShouldBeLessThan(dataLayer.IndexOf("\"update\"", StringComparison.Ordinal));
        dataLayer.IndexOf("\"update\"", StringComparison.Ordinal).ShouldBeLessThan(dataLayer.IndexOf("\"config\"", StringComparison.Ordinal));
        dataLayer.ShouldContain("\"update\"");
        dataLayer.ShouldContain("\"analytics_storage\":\"denied\"");
    }

    [Fact]
    public async Task GoogleTagManagerLoadsForMarketingOnlyConsentAndPublishesTheConsentEvent()
    {
        await using var host = await BrowserTestHost.StartAsync("gtm");
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();
        var googleRequests = new List<string>();
        await page.RouteAsync("https://www.googletagmanager.com/**", async route =>
        {
            googleRequests.Add(route.Request.Url);
            await route.FulfillAsync(new RouteFulfillOptions { ContentType = "application/javascript", Body = "" });
        });

        await page.GotoAsync(host.Address.ToString());
        googleRequests.ShouldBeEmpty();

        await page.Locator("[data-privacy-banner] [data-privacy-action='open-settings']").ClickAsync();
        await page.Locator("[data-privacy-category='marketing']").CheckAsync();
        await page.Locator("[data-privacy-action='save-settings']").ClickAsync();
        await page.WaitForTimeoutAsync(100);

        googleRequests.ShouldHaveSingleItem().ShouldContain("/gtm.js?id=GTM-ABCDE123");
        var dataLayer = await page.EvaluateAsync<string>("JSON.stringify(window.dataLayer)");
        dataLayer.ShouldContain("syntax_circus_consent_update");
        dataLayer.ShouldContain("\"marketing\":true");
        dataLayer.ShouldContain("\"analytics\":false");

        await page.Locator("[data-privacy-settings-link]").ClickAsync();
        await page.Locator("[data-privacy-category='analytics']").CheckAsync();
        await page.Locator("[data-privacy-action='save-settings']").ClickAsync();
        await page.WaitForTimeoutAsync(100);

        googleRequests.ShouldHaveSingleItem();
        var consentEventCount = await page.EvaluateAsync<int>("window.dataLayer.filter(entry => entry.event === 'syntax_circus_consent_update').length");
        consentEventCount.ShouldBe(2);
    }

    [Fact]
    public async Task RejectAllDoesNotLoadGoogleTagManagerAfterReload()
    {
        await using var host = await BrowserTestHost.StartAsync("gtm");
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();
        var googleRequests = new List<string>();
        await page.RouteAsync("https://www.googletagmanager.com/**", async route =>
        {
            googleRequests.Add(route.Request.Url);
            await route.FulfillAsync(new RouteFulfillOptions { ContentType = "application/javascript", Body = "" });
        });

        await page.GotoAsync(host.Address.ToString());
        await page.Locator("[data-privacy-action='reject-all']").ClickAsync();
        await page.ReloadAsync();
        await page.WaitForTimeoutAsync(100);

        googleRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task PolicyVersionChangeInvalidatesStoredConsent()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();
        var googleRequests = new List<string>();
        await page.RouteAsync("https://www.googletagmanager.com/**", async route =>
        {
            googleRequests.Add(route.Request.Url);
            await route.FulfillAsync(new RouteFulfillOptions { ContentType = "application/javascript", Body = "" });
        });

        await using (var firstHost = await BrowserTestHost.StartAsync("ga4", "1"))
        {
            await page.GotoAsync(firstHost.Address.ToString());
            await page.Locator("[data-privacy-action='accept-all']").ClickAsync();
            await page.WaitForTimeoutAsync(100);
        }

        await using (var secondHost = await BrowserTestHost.StartAsync("ga4", "2"))
        {
            await page.GotoAsync(secondHost.Address.ToString());
            (await page.Locator("[data-privacy-banner]").IsVisibleAsync()).ShouldBeTrue();
        }

        googleRequests.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task UmamiLoadsWithoutConsentAndDoesNotShowTheBanner()
    {
        await using var host = await BrowserTestHost.StartAsync("umami", umamiRequireConsent: false);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();
        var umamiRequests = new List<string>();
        await page.RouteAsync("https://analytics.example.test/**", async route =>
        {
            umamiRequests.Add(route.Request.Url);
            await route.FulfillAsync(new RouteFulfillOptions { ContentType = "application/javascript", Body = "" });
        });

        await page.GotoAsync(host.Address.ToString());
        await page.WaitForTimeoutAsync(100);

        umamiRequests.ShouldHaveSingleItem().ShouldContain("/tracker.js");
        (await page.Locator("[data-privacy-banner]").IsVisibleAsync()).ShouldBeFalse();
    }
}
