using Microsoft.Extensions.DependencyInjection;

namespace woliver13.X12Net.CLI;

/// <summary>
/// Extension methods for registering X12Net services with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers application-layer services: <see cref="IX12ToolService"/> → <see cref="X12ToolService"/>.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IX12ToolService, X12ToolService>();
        return services;
    }

    /// <summary>
    /// Registers infrastructure-layer services (logging and host infrastructure are
    /// handled by the host builder; this method is a registration point for future
    /// infrastructure concerns).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services) =>
        services;
}
