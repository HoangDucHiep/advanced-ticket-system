
using TicketSystem.Shared.Abstractions.Domain;

namespace TicketSystem.Shared.Abstractions.Events;

/// <summary>
/// In-process bus (B13): runs every handler registered for the event's runtime type, synchronously, in the
/// current transaction. Call it before <c>CommitAsync</c>.
/// </summary>
public interface IEventPublisher
{
    /// <summary>Publishes a domain event to handlers inside the same module.</summary>
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

    /// <summary>Publishes an integration event to handlers in other modules.</summary>
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}