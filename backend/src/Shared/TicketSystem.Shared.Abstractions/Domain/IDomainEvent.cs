namespace TicketSystem.Shared.Abstractions.Domain;

/// <summary>Represents a domain event raised by an aggregate root.</summary>
public interface IDomainEvent
{
    /// <summary>Gets the UTC timestamp when the event occurred.</summary>
    DateTimeOffset OccurredAt { get; }
}


