using TicketSystem.Shared.Abstractions.Results;

namespace TicketSystem.Shared.Abstractions.Application;

/// <summary>Handles exactly one command use case. Owns its transaction boundary.</summary>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    /// <summary>Runs the use case; expected business failures come back as a failed <see cref="Result{T}"/>.</summary>
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}