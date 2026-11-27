using System.Collections.Concurrent;
using System.Data.Common;

using Microsoft.Extensions.Logging;

using TicketSystem.Shared.Abstractions.Application;
using TicketSystem.Shared.Abstractions.Domain;
using TicketSystem.Shared.Abstractions.Events;
using TicketSystem.Shared.Abstractions.Persistence;
using TicketSystem.Shared.Abstractions.Results;

namespace TicketSystem.Shared.Tests.Application;

// Test-only use cases and events that exercise the application primitives (B6, B13).
// The assembly scan in the tests runs over THIS assembly, so every handler here is "a module's handler".

internal sealed record GreetCommand(string Name) : ICommand<string>;

internal sealed class GreetHandler : ICommandHandler<GreetCommand, string>
{
    public Task<Result<string>> HandleAsync(GreetCommand command, CancellationToken cancellationToken) =>
        Task.FromResult<Result<string>>($"hello {command.Name}");
}

internal sealed record RejectCommand : ICommand<string>;

internal sealed class RejectHandler : ICommandHandler<RejectCommand, string>
{
    public static readonly Error Rejected = new()
    {
        Code = "SAMPLE_REJECTED",
        Title = "Rejected",
        Action = "Try something else.",
        Type = ErrorType.Conflict,
    };

    public Task<Result<string>> HandleAsync(RejectCommand command, CancellationToken cancellationToken) =>
        Task.FromResult<Result<string>>(Rejected);
}

internal sealed record ExplodeCommand : ICommand<string>;

internal sealed class ExplodeHandler : ICommandHandler<ExplodeCommand, string>
{
    public Task<Result<string>> HandleAsync(ExplodeCommand command, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("boom");
}

internal sealed record DoubleQuery(int Value) : IQuery<int>;

internal sealed class DoubleHandler : IQueryHandler<DoubleQuery, int>
{
    public Task<Result<int>> HandleAsync(DoubleQuery query, CancellationToken cancellationToken) =>
        Task.FromResult<Result<int>>(query.Value * 2);
}

/// <summary>Uses the unit of work only through the decorator path — proves the decorator never touches it.</summary>
internal sealed record TouchNothingCommand : ICommand<string>;

internal sealed class TouchNothingHandler : ICommandHandler<TouchNothingCommand, string>
{
    public Task<Result<string>> HandleAsync(TouchNothingCommand command, CancellationToken cancellationToken) =>
        Task.FromResult<Result<string>>("ok");
}

internal sealed record AbstractOnlyCommand : ICommand<string>;

/// <summary>Abstract: must NOT be registered by the scan.</summary>
internal abstract class AbstractOnlyHandler : ICommandHandler<AbstractOnlyCommand, string>
{
    public abstract Task<Result<string>> HandleAsync(AbstractOnlyCommand command, CancellationToken cancellationToken);
}

/// <summary>Looks like a handler but implements no handler interface: must NOT be registered.</summary>
internal sealed class LooksLikeAHandler
{
    public Task<Result<string>> HandleAsync(GreetCommand command, CancellationToken cancellationToken) =>
        Task.FromResult<Result<string>>("not registered");
}

internal sealed record SampleDomainEvent(DateTimeOffset OccurredAt) : IDomainEvent;

internal sealed record NobodyListensDomainEvent(DateTimeOffset OccurredAt) : IDomainEvent;

internal sealed record SampleIntegrationEvent(Guid EventId, DateTimeOffset OccurredAt) : IIntegrationEvent;

/// <summary>Records which handler saw which event, in order. Registered as a singleton by the tests.</summary>
internal sealed class EventLog
{
    public ConcurrentQueue<string> Entries { get; } = new();
}

internal sealed class FirstSampleDomainEventHandler(EventLog log) : IEventHandler<SampleDomainEvent>
{
    public Task HandleAsync(SampleDomainEvent @event, CancellationToken cancellationToken)
    {
        log.Entries.Enqueue("first");
        return Task.CompletedTask;
    }
}

internal sealed class SecondSampleDomainEventHandler(EventLog log) : IEventHandler<SampleDomainEvent>
{
    public Task HandleAsync(SampleDomainEvent @event, CancellationToken cancellationToken)
    {
        log.Entries.Enqueue("second");
        return Task.CompletedTask;
    }
}

internal sealed class SampleIntegrationEventHandler(EventLog log) : IEventHandler<SampleIntegrationEvent>
{
    public Task HandleAsync(SampleIntegrationEvent @event, CancellationToken cancellationToken)
    {
        log.Entries.Enqueue($"integration:{@event.EventId}");
        return Task.CompletedTask;
    }
}

/// <summary>Counts transaction calls; any call from the decorator path is a bug (B7).</summary>
internal sealed class CountingUnitOfWork : IUnitOfWork
{
    public int BeginCalls { get; private set; }
    public int CommitCalls { get; private set; }

    public bool IsActive => false;
    public DbConnection Connection => throw new NotSupportedException();
    public DbTransaction Transaction => throw new NotSupportedException();

    public Task BeginAsync(CancellationToken cancellationToken = default)
    {
        BeginCalls++;
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        CommitCalls++;
        return Task.CompletedTask;
    }
}

internal sealed record CapturedLog(LogLevel Level, string Message, IReadOnlyDictionary<string, object?> State);

/// <summary>Keeps every log entry with its structured state so tests can assert on template values.</summary>
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    public ConcurrentQueue<CapturedLog> Logs { get; } = new();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(CapturingLoggerProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var values = state is IEnumerable<KeyValuePair<string, object?>> pairs
                ? pairs.ToDictionary(p => p.Key, p => p.Value)
                : new Dictionary<string, object?>();
            provider.Logs.Enqueue(new CapturedLog(logLevel, formatter(state, exception), values));
        }
    }
}
