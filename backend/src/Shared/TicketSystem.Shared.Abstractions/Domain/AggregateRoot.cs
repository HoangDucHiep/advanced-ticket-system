namespace TicketSystem.Shared.Abstractions.Domain;

/// <summary>Base aggregate root that collects domain events raised during a use case.</summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : struct, IEntityId<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Initializes the aggregate root with a non-empty typed id.</summary>
    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    /// <summary>Parameterless constructor for EF materialization only.</summary>
    protected AggregateRoot()
    {
    }

    /// <summary>Records a domain event to be dispatched after persistence.</summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>Returns a snapshot of the collected events and clears them.</summary>
    public IReadOnlyList<IDomainEvent> DequeueDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();
        return events;
    }
}