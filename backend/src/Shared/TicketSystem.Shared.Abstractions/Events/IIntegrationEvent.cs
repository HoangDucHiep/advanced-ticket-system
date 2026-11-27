namespace TicketSystem.Shared.Abstractions.Events;

/// <summary>
/// A contract between modules. Lives in the publishing module's <c>.Contracts</c>, carries only
/// flat types, and may only gain fields over time.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>Gets the unique id of this event occurrence.</summary>
    Guid EventId { get; }

    /// <summary>Gets when the underlying change happened.</summary>
    DateTimeOffset OccurredAt { get; }
}