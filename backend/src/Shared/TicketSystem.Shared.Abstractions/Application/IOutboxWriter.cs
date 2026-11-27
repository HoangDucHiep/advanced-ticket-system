namespace TicketSystem.Shared.Abstractions.Application;

/// <summary>
/// Writes an outbox message of the calling module inside the current transaction.
/// Processed after commit, at least once. Implemented with the dispatcher in the outbox ticket.
/// </summary>
public interface IOutboxWriter
{
    /// <summary>Adds <paramref name="message"/> to the module's outbox; due now or at <paramref name="availableAt"/>.</summary>
    Task EnqueueAsync<TMessage>(
        TMessage message,
        DateTimeOffset? availableAt = null,
        CancellationToken cancellationToken = default
    ) where TMessage : notnull;
}
