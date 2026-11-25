using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TicketSystem.Inventory;

/// <summary>
/// Extension methods for registering the Inventory module services in the dependency injection container.
/// </summary>
public static class InventoryModule
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Ticket nghiệp vụ thêm vào đây: AddModuleDbContext<...>("<schema>"), use case, ...
        return services;
    }
}
