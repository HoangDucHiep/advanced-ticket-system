using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TicketSystem.Scheduling;

/// <summary>
/// Extension methods for registering the Scheduling module services in the dependency injection container.
/// </summary>
public static class SchedulingModule
{
    public static IServiceCollection AddSchedulingModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Ticket nghiệp vụ thêm vào đây: AddModuleDbContext<...>("<schema>"), use case, ...
        return services;
    }
}
