using TicketSystem.Shared.Abstractions.Results;

namespace TicketSystem.Shared.Abstractions.Application;

/// <summary>Handles exactly one read-only query use case.</summary>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    /// <summary>Runs the query; "not found" and similar outcomes come back as a failed <see cref="Result{T}"/>.</summary>
    Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}