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

    [Theory]
    [InlineData("G-INVALID-ID")]
    [InlineData("GTM-ABCDE")]
    [InlineData("measurement-id")]
    public void RejectsEnabledGoogleAnalyticsWithAnInvalidMeasurementId(string measurementId)
    {
        var result = new TrackingOptionsValidator().Validate(null, new TrackingOptions
        {
            GoogleAnalytics = new GoogleAnalyticsOptions { Enabled = true, MeasurementId = measurementId }
        });

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(failure => failure.Contains("G- identifier", StringComparison.Ordinal));
    }

    [Fact]
    public void AcceptsEnabledGoogleAnalyticsWithAMeasurementId()
    {
        var result = new TrackingOptionsValidator().Validate(null, new TrackingOptions
        {
            GoogleAnalytics = new GoogleAnalyticsOptions { Enabled = true, MeasurementId = "G-ABCDE123" }
        });

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void RejectsEnabledGoogleTagManagerWithoutContainerId()
    {
        var result = new TrackingOptionsValidator().Validate(null, new TrackingOptions
        {
            GoogleTagManager = new GoogleTagManagerOptions { Enabled = true }
        });

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(failure => failure.Contains("ContainerId", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("G-ABCDE")]
    [InlineData("GTM-invalid")]
    [InlineData("container-id")]
    public void RejectsEnabledGoogleTagManagerWithAnInvalidContainerId(string containerId)
    {
        var result = new TrackingOptionsValidator().Validate(null, new TrackingOptions
        {
            GoogleTagManager = new GoogleTagManagerOptions { Enabled = true, ContainerId = containerId }
        });

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(failure => failure.Contains("GTM- identifier", StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsEnablingGoogleAnalyticsAndGoogleTagManagerTogether()
    {
        var result = new TrackingOptionsValidator().Validate(null, new TrackingOptions
        {
            GoogleAnalytics = new GoogleAnalyticsOptions { Enabled = true, MeasurementId = "G-ABCDE123" },
            GoogleTagManager = new GoogleTagManagerOptions { Enabled = true, ContainerId = "GTM-ABCDE123" }
        });

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(failure => failure.Contains("cannot both be enabled", StringComparison.Ordinal));
    }

    [Fact]
    public void AcceptsEnabledGoogleTagManagerWithAContainerId()
    {
        var result = new TrackingOptionsValidator().Validate(null, new TrackingOptions
        {
            GoogleTagManager = new GoogleTagManagerOptions { Enabled = true, ContainerId = "GTM-ABCDE123" }
        });

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void AcceptsDisabledProviders()
    {
        var result = new TrackingOptionsValidator().Validate(null, new TrackingOptions());

        result.Succeeded.ShouldBeTrue();
    }
}
