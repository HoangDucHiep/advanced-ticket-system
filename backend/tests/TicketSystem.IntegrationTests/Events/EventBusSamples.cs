using System.Data.Common;

using TicketSystem.IntegrationTests.Persistence;
using TicketSystem.Shared.Abstractions.Application;
using TicketSystem.Shared.Abstractions.Domain;
using TicketSystem.Shared.Abstractions.Events;
using TicketSystem.Shared.Abstractions.Persistence;
using TicketSystem.Shared.Abstractions.Results;

namespace TicketSystem.IntegrationTests.Events;

// Two simulated modules: "alpha" publishes, "beta" reacts. Mirrors B13 / ADR-0019:
// use case → domain event (alpha, internal) → translator (alpha) → integration event (alpha's contract)
// → handler in beta — all inside ONE transaction of the unit of work.

// ---- alpha: domain event (would live in Alpha/Domain) ----
internal sealed record AlphaNoteAddedDomainEvent(Guid NoteId, string Text, DateTimeOffset OccurredAt) : IDomainEvent;

internal sealed record AlphaRejectedNoteAddedDomainEvent(Guid NoteId, DateTimeOffset OccurredAt) : IDomainEvent;

internal sealed record AlphaExplodingDomainEvent(DateTimeOffset OccurredAt) : IDomainEvent;

// ---- alpha: integration events (would live in Alpha.Contracts; flat types only) ----
public sealed record AlphaNoteAddedIntegrationEvent(Guid EventId, DateTimeOffset OccurredAt, Guid NoteId, string Text)
    : IIntegrationEvent;

public sealed record AlphaRejectedNoteIntegrationEvent(Guid EventId, DateTimeOffset OccurredAt, Guid NoteId)
    : IIntegrationEvent;

public sealed record PingIntegrationEvent(Guid EventId, DateTimeOffset OccurredAt) : IIntegrationEvent;

// ---- alpha: use cases ----
internal sealed record AddAlphaNote(Guid NoteId, string Text, bool RejectedByBeta = false, bool Explode = false)
    : ICommand<Guid>;

internal sealed class AddAlphaNoteHandler(IUnitOfWork unitOfWork, AlphaDbContext alpha, IEventPublisher events)
    : ICommandHandler<AddAlphaNote, Guid>
{
    public async Task<Result<Guid>> HandleAsync(AddAlphaNote command, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginAsync(cancellationToken);

        alpha.Notes.Add(new Note { Id = command.NoteId, Text = command.Text });
        await alpha.SaveChangesAsync(cancellationToken);

        IDomainEvent domainEvent = command switch
        {
            { Explode: true } => new AlphaExplodingDomainEvent(DateTimeOffset.UtcNow),
            { RejectedByBeta: true } => new AlphaRejectedNoteAddedDomainEvent(command.NoteId, DateTimeOffset.UtcNow),
            _ => new AlphaNoteAddedDomainEvent(command.NoteId, command.Text, DateTimeOffset.UtcNow),
        };
        await events.PublishAsync(domainEvent, cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);
        return command.NoteId;
    }
}

// ---- alpha: translators domain event → integration event (B13 step 4) ----
internal sealed class AlphaNoteAddedTranslator(IEventPublisher events) : IEventHandler<AlphaNoteAddedDomainEvent>
{
    public Task HandleAsync(AlphaNoteAddedDomainEvent @event, CancellationToken cancellationToken) =>
        events.PublishAsync(
            new AlphaNoteAddedIntegrationEvent(Guid.CreateVersion7(), @event.OccurredAt, @event.NoteId, @event.Text),
            cancellationToken);
}

internal sealed class AlphaRejectedNoteTranslator(IEventPublisher events) : IEventHandler<AlphaRejectedNoteAddedDomainEvent>
{
    public Task HandleAsync(AlphaRejectedNoteAddedDomainEvent @event, CancellationToken cancellationToken) =>
        events.PublishAsync(
            new AlphaRejectedNoteIntegrationEvent(Guid.CreateVersion7(), @event.OccurredAt, @event.NoteId),
            cancellationToken);
}

internal sealed class AlphaExplodingHandler : IEventHandler<AlphaExplodingDomainEvent>
{
    public Task HandleAsync(AlphaExplodingDomainEvent @event, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("domain event handler failed");
}

// ---- beta: reacts to alpha's contract ----
internal sealed class BetaCopiesAlphaNote(BetaDbContext beta) : IEventHandler<AlphaNoteAddedIntegrationEvent>
{
    public async Task HandleAsync(AlphaNoteAddedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        beta.Notes.Add(new Note { Id = @event.NoteId, Text = $"copy of {@event.Text}" });
        await beta.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class BetaRejectsAlphaNote : IEventHandler<AlphaRejectedNoteIntegrationEvent>
{
    public Task HandleAsync(AlphaRejectedNoteIntegrationEvent @event, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("beta rejected the note");
}

/// <summary>Records the transaction each Ping handler ran in (scoped: one per use case).</summary>
internal sealed class TransactionRecorder
{
    public List<DbTransaction> Seen { get; } = [];
}

internal sealed class FirstPingHandler(IUnitOfWork unitOfWork, TransactionRecorder recorder)
    : IEventHandler<PingIntegrationEvent>
{
    public Task HandleAsync(PingIntegrationEvent @event, CancellationToken cancellationToken)
    {
        recorder.Seen.Add(unitOfWork.Transaction);
        return Task.CompletedTask;
    }
}

internal sealed class SecondPingHandler(IUnitOfWork unitOfWork, TransactionRecorder recorder)
    : IEventHandler<PingIntegrationEvent>
{
    public Task HandleAsync(PingIntegrationEvent @event, CancellationToken cancellationToken)
    {
        recorder.Seen.Add(unitOfWork.Transaction);
        return Task.CompletedTask;
    }
}
