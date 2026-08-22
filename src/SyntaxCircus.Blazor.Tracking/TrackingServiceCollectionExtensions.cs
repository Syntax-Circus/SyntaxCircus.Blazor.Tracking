using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SyntaxCircus.Blazor.Tracking;

public static class TrackingServiceCollectionExtensions
{
    public static IServiceCollection AddSyntaxCircusTracking(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<TrackingOptions>()
            .Bind(configuration.GetSection(TrackingOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<Microsoft.Extensions.Options.IValidateOptions<TrackingOptions>, TrackingOptionsValidator>();

        return services;
    }
}
