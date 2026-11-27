using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using TicketSystem.Shared.Abstractions.Application;
using TicketSystem.Shared.Abstractions.Events;
using TicketSystem.Shared.Infrastructure.Events;

namespace TicketSystem.Shared.Infrastructure.Application;

/// <summary>Registers a module's handlers in one line from <c>Add&lt;Module&gt;Module</c> (B6, B13).</summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Scans <paramref name="assembly"/> once at startup and registers, as Scoped: every command / query
    /// handler behind its logging + tracing decorator, every event handler, and the in-process bus.
    /// </summary>
    public static IServiceCollection AddApplicationHandlers(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        services.TryAddScoped<IEventPublisher, InProcessEventPublisher>();

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
            {
                continue;
            }

            foreach (var contract in type.GetInterfaces().Where(i => i.IsGenericType))
            {
                var definition = contract.GetGenericTypeDefinition();

                if (definition == typeof(ICommandHandler<,>))
                {
                    RegisterDecorated(services, contract, type, typeof(CommandHandlerDecorator<,>));
                }
                else if (definition == typeof(IQueryHandler<,>))
                {
                    RegisterDecorated(services, contract, type, typeof(QueryHandlerDecorator<,>));
                }
                else if (definition == typeof(IEventHandler<>))
                {
                    services.TryAddEnumerable(ServiceDescriptor.Scoped(contract, type));
                }
            }
        }

        return services;
    }

    private static void RegisterDecorated(
        IServiceCollection services, Type contract, Type implementation, Type openDecorator)
    {
        var decorator = openDecorator.MakeGenericType(contract.GetGenericArguments());

        services.TryAddScoped(implementation);
        services.AddScoped(contract, provider =>
            ActivatorUtilities.CreateInstance(provider, decorator, provider.GetRequiredService(implementation)));
    }
}