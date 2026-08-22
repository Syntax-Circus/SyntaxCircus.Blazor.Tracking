using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace SyntaxCircus.Blazor.Tracking;

public sealed class TrackingOptions
{
    public const string SectionName = "Tracking";

    public UmamiOptions Umami { get; set; } = new();

    public GoogleAnalyticsOptions GoogleAnalytics { get; set; } = new();

    public GoogleTagManagerOptions GoogleTagManager { get; set; } = new();

    public ConsentOptions Consent { get; set; } = new();
}

public sealed class UmamiOptions
{
    public bool Enabled { get; set; }

    public string? ScriptUrl { get; set; }

    public string? WebsiteId { get; set; }
}

public sealed class GoogleAnalyticsOptions
{
    public bool Enabled { get; set; }

    public string? MeasurementId { get; set; }
}

public sealed class GoogleTagManagerOptions
{
    public bool Enabled { get; set; }

    public string? ContainerId { get; set; }
}

public sealed class ConsentOptions
{
    public string PolicyVersion { get; set; } = "1";

    public string CookieName { get; set; } = "syntax_circus_tracking_consent";

    public int CookieLifetimeDays { get; set; } = 180;

    public string? PrivacyPolicyUrl { get; set; }
}

internal sealed class TrackingOptionsValidator : IValidateOptions<TrackingOptions>
{
    private static readonly Regex GoogleAnalyticsMeasurementIdPattern = new("\\AG-[A-Z0-9]+\\z", RegexOptions.CultureInvariant);
    private static readonly Regex GoogleTagManagerContainerIdPattern = new("\\AGTM-[A-Z0-9]+\\z", RegexOptions.CultureInvariant);

    public ValidateOptionsResult Validate(string? name, TrackingOptions options)
    {
        List<string> failures = [];

        if (options.Umami.Enabled && (string.IsNullOrWhiteSpace(options.Umami.ScriptUrl) || string.IsNullOrWhiteSpace(options.Umami.WebsiteId)))
        {
            failures.Add("Tracking:Umami requires ScriptUrl and WebsiteId when Enabled is true.");
        }

        if (options.GoogleAnalytics.Enabled && string.IsNullOrWhiteSpace(options.GoogleAnalytics.MeasurementId))
        {
            failures.Add("Tracking:GoogleAnalytics requires MeasurementId when Enabled is true.");
        }
        else if (options.GoogleAnalytics.Enabled && !GoogleAnalyticsMeasurementIdPattern.IsMatch(options.GoogleAnalytics.MeasurementId!))
        {
            failures.Add("Tracking:GoogleAnalytics:MeasurementId must use the GA4 G- identifier format.");
        }

        if (options.GoogleTagManager.Enabled && string.IsNullOrWhiteSpace(options.GoogleTagManager.ContainerId))
        {
            failures.Add("Tracking:GoogleTagManager requires ContainerId when Enabled is true.");
        }
        else if (options.GoogleTagManager.Enabled && !GoogleTagManagerContainerIdPattern.IsMatch(options.GoogleTagManager.ContainerId!))
        {
            failures.Add("Tracking:GoogleTagManager:ContainerId must use the GTM- identifier format.");
        }

        if (options.GoogleAnalytics.Enabled && options.GoogleTagManager.Enabled)
        {
            failures.Add("Tracking:GoogleAnalytics and Tracking:GoogleTagManager cannot both be enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.Consent.PolicyVersion))
        {
            failures.Add("Tracking:Consent:PolicyVersion is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Consent.CookieName))
        {
            failures.Add("Tracking:Consent:CookieName is required.");
        }

        if (options.Consent.CookieLifetimeDays is < 1 or > 400)
        {
            failures.Add("Tracking:Consent:CookieLifetimeDays must be between 1 and 400.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
