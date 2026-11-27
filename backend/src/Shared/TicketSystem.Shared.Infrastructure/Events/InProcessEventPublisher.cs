using System.Collections.Concurrent;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

using TicketSystem.Shared.Abstractions.Domain;
using TicketSystem.Shared.Abstractions.Events;

namespace TicketSystem.Shared.Infrastructure.Events;

/// <summary>
/// B13 bus: resolves <see cref="IEventHandler{TEvent}"/> for the event's runtime type from the request
/// scope and awaits each one in turn, inside the caller's transaction. Exceptions propagate (⇒ rollback).
/// </summary>
internal sealed class InProcessEventPublisher(IServiceProvider services) : IEventPublisher
{
    private delegate Task Invoker(object handler, object @event, CancellationToken cancellationToken);

    private static readonly ConcurrentDictionary<Type, (Type HandlerType, Invoker Invoke)> Cache = new();

    private static readonly MethodInfo InvokeMethod =
        typeof(InProcessEventPublisher).GetMethod(nameof(InvokeTyped), BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException($"{nameof(InvokeTyped)} not found.");

    public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        return DispatchAsync(domainEvent, cancellationToken);
    }

    public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return DispatchAsync(integrationEvent, cancellationToken);
    }

    private async Task DispatchAsync(object @event, CancellationToken cancellationToken)
    {
        var (handlerType, invoke) = Cache.GetOrAdd(@event.GetType(), static eventType =>
        (
            typeof(IEventHandler<>).MakeGenericType(eventType),
            InvokeMethod.MakeGenericMethod(eventType).CreateDelegate<Invoker>()
        ));

        foreach (var handler in services.GetServices(handlerType))
        {
            if (handler is not null)
            {
                await invoke(handler, @event, cancellationToken);
            }
        }
    }

    private static Task InvokeTyped<TEvent>(object handler, object @event, CancellationToken cancellationToken)
        where TEvent : notnull =>
        ((IEventHandler<TEvent>)handler).HandleAsync((TEvent)@event, cancellationToken);
}