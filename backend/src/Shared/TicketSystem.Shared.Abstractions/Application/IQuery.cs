namespace TicketSystem.Shared.Abstractions.Application;

/// <summary>Marker for the input of a read-only use case that returns <typeparamref name="TResult"/>.</summary>
public interface IQuery<TResult>;