using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace SyntaxCircus.Blazor.Tracking.Tests;

public sealed class TrackingOptionsValidatorTests
{
    [Fact]
    public void RejectsEnabledGoogleAnalyticsWithoutMeasurementId()
    {
        var result = new TrackingOptionsValidator().Validate(null, new TrackingOptions
        {
            GoogleAnalytics = new GoogleAnalyticsOptions { Enabled = true }
        });

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(failure => failure.Contains("MeasurementId", StringComparison.Ordinal));
    }

    [Fact]
    public void AcceptsDisabledProviders()
    {
        var result = new TrackingOptionsValidator().Validate(null, new TrackingOptions());

        result.Succeeded.ShouldBeTrue();
    }
}
