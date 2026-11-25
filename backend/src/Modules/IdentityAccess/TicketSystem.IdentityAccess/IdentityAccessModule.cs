using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TicketSystem.IdentityAccess;

/// <summary>
/// Extension methods for registering the IdentityAccess module services in the dependency injection container.
/// </summary>
public static class IdentityAccessModule
{
    public static IServiceCollection AddIdentityAccessModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Ticket nghiệp vụ thêm vào đây: AddModuleDbContext<...>("<schema>"), use case, ...
        return services;
    }
}
