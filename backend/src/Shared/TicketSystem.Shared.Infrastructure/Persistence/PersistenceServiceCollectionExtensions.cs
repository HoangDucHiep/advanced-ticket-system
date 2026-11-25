using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using TicketSystem.Shared.Abstractions.Persistence;

namespace TicketSystem.Shared.Infrastructure.Persistence;

/// <summary>
/// Extension methods for registering persistence services in the dependency injection container.
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddUnitOfWork(this IServiceCollection services, string connectionString)
    {
        services.AddNpgsqlDataSource(connectionString);
        services.AddScoped<NpgsqlUnitOfWork>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<NpgsqlUnitOfWork>());
        return services;
    }

    public static IServiceCollection AddModuleDbContext<TContext>(this IServiceCollection services, string schema)
        where TContext : DbContext
    {
        services.AddScoped(sp =>
        {
            var unitOfWork = sp.GetRequiredService<NpgsqlUnitOfWork>();
            var options = new DbContextOptionsBuilder<TContext>()
                .UseNpgsql(unitOfWork.Connection,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schema))
                .AddInterceptors(new RequireUnitOfWorkInterceptor())
                .Options;

            var context = ActivatorUtilities.CreateInstance<TContext>(sp, options);
            unitOfWork.Enlist(context);
            return context;
        });

        return services;
    }
}