using BlazorJSComponents;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Defines extension methods for configuring JavaScript components.
/// </summary>
public static class BlazorJSComponentsServiceCollectionExtensions
{
    /// <summary>
    /// Registers services required for JavaScript components.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
    /// <param name="configure">A callback to configure <see cref="JSComponentOptions"/>.</param>
    /// <returns></returns>
    public static IServiceCollection AddJSComponents(this IServiceCollection services, Action<JSComponentOptions>? configure = null)
    {
        services.AddSingleton<JSComponentManager>();
        services.AddScoped<UniqueIdAllocator>();
        services.AddTransient<JSElementReferenceScope>(static sp =>
        {
            var allocator = sp.GetRequiredService<UniqueIdAllocator>();
            var id = allocator.GetNextId();
            return new(id);
        });

        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services;
    }
}
