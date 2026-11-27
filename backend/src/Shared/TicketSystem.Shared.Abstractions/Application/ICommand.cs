namespace TicketSystem.Shared.Abstractions.Application;

/// <summary>Marker for the input of a use case that changes state and returns <typeparamref name="TResult"/>.</summary>
public interface ICommand<TResult>;