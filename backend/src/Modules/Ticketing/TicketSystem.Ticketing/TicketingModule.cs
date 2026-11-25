using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TicketSystem.Ticketing;

/// <summary>
/// Extension methods for registering the Ticketing module services in the dependency injection container.
/// </summary>
public static class TicketingModule
{
    public static IServiceCollection AddTicketingModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Ticket nghiệp vụ thêm vào đây: AddModuleDbContext<...>("<schema>"), use case, ...
        return services;
    }
}
