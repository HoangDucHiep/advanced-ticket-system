using Microsoft.Extensions.Logging;

using TicketSystem.Shared.Abstractions.Application;
using TicketSystem.Shared.Abstractions.Results;

namespace TicketSystem.Shared.Infrastructure.Application;

/// <summary>Wraps every query handler with logging and tracing (B6). Registered by the assembly scan.</summary>
internal sealed class QueryHandlerDecorator<TQuery, TResult>(
    IQueryHandler<TQuery, TResult> inner,
    ILogger<QueryHandlerDecorator<TQuery, TResult>> logger)
    : IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken) =>
        HandlerInstrumentation.RunAsync(
            inner.GetType().Name,
            logger,
            () => inner.HandleAsync(query, cancellationToken));
}