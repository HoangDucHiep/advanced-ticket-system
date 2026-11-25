using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TicketSystem.Payment;

/// <summary>
/// Extension methods for registering the Payment module services in the dependency injection container.
/// </summary>
public static class PaymentModule
{
    public static IServiceCollection AddPaymentModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Ticket nghiệp vụ thêm vào đây: AddModuleDbContext<...>("<schema>"), use case, ...
        return services;
    }
}
