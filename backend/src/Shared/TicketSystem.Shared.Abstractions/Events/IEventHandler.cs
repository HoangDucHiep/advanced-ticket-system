namespace TicketSystem.Shared.Abstractions.Events;

/// <summary>
/// Reacts to one event type inside the publisher's transaction. Never opens or commits a transaction;
/// throwing rolls back the whole use case.
/// </summary>
public interface IEventHandler<in TEvent>
    where TEvent : notnull
{
    /// <summary>Handles <paramref name="event"/>.</summary>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
}