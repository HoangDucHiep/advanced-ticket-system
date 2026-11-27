using Microsoft.Extensions.Logging;

using TicketSystem.Shared.Abstractions.Application;
using TicketSystem.Shared.Abstractions.Results;

namespace TicketSystem.Shared.Infrastructure.Application;

/// <summary>Wraps every command handler with logging and tracing (B6). Registered by the assembly scan.</summary>
internal sealed class CommandHandlerDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    ILogger<CommandHandlerDecorator<TCommand, TResult>> logger)
    : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken) =>
        HandlerInstrumentation.RunAsync(
            inner.GetType().Name,
            logger,
            () => inner.HandleAsync(command, cancellationToken));
}