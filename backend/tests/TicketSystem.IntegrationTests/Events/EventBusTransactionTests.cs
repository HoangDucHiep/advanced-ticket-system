using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using TicketSystem.IntegrationTests.Persistence;
using TicketSystem.Shared.Abstractions.Application;
using TicketSystem.Shared.Abstractions.Events;
using TicketSystem.Shared.Abstractions.Persistence;
using TicketSystem.Shared.Infrastructure.Application;
using TicketSystem.Shared.Infrastructure.Persistence;

namespace TicketSystem.IntegrationTests.Events;

/// <summary>
/// B13 / ADR-0009 / ADR-0019 on a real Postgres: events run inside the publisher's transaction, so a failing
/// handler anywhere rolls back the whole use case. Red if the bus delivers after commit, swallows exceptions,
/// or the translator to the integration event is skipped.
/// </summary>
public sealed class EventBusTransactionTests(PostgresFixture database) : IClassFixture<PostgresFixture>
{
    private ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddUnitOfWork(database.ConnectionString);
        services.AddModuleDbContext<AlphaDbContext>("alpha");
        services.AddModuleDbContext<BetaDbContext>("beta");
        services.AddScoped<TransactionRecorder>();
        services.AddApplicationHandlers(typeof(EventBusTransactionTests).Assembly);
        return services.BuildServiceProvider(validateScopes: true);
    }

    private async Task<Guid> RunAsync(AddAlphaNote command)
    {
        await using var provider = BuildServices();
        await using var scope = provider.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<AddAlphaNote, Guid>>();
        var result = await handler.HandleAsync(command, CancellationToken.None);
        return result.Value;
    }

    [Fact]
    public async Task EventBus_runs_all_handlers_in_same_transaction()
    {
        await using var provider = BuildServices();
        await using var scope = provider.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        var recorder = scope.ServiceProvider.GetRequiredService<TransactionRecorder>();

        await unitOfWork.BeginAsync();
        await publisher.PublishAsync(new PingIntegrationEvent(Guid.CreateVersion7(), DateTimeOffset.UtcNow));

        recorder.Seen.Count.ShouldBe(2);
        recorder.Seen.ShouldAllBe(transaction => ReferenceEquals(transaction, unitOfWork.Transaction));
    }

    [Fact]
    public async Task Domain_event_translated_to_integration_event_in_same_transaction()
    {
        var id = Guid.CreateVersion7();

        await RunAsync(new AddAlphaNote(id, "hello"));

        (await database.CountCommittedAsync("alpha", id)).ShouldBe(1);
        (await database.CountCommittedAsync("beta", id)).ShouldBe(1, "beta reacts to alpha's integration event");
    }

    [Fact]
    public async Task Failing_event_handler_rolls_back_whole_use_case()
    {
        var id = Guid.CreateVersion7();

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            RunAsync(new AddAlphaNote(id, "boom", Explode: true)));

        exception.Message.ShouldBe("domain event handler failed");
        (await database.CountCommittedAsync("alpha", id)).ShouldBe(0);
    }

    [Fact]
    public async Task Failing_integration_handler_rolls_back_publisher()
    {
        var id = Guid.CreateVersion7();

        await Should.ThrowAsync<InvalidOperationException>(() =>
            RunAsync(new AddAlphaNote(id, "rejected", RejectedByBeta: true)));

        (await database.CountCommittedAsync("alpha", id)).ShouldBe(0, "alpha must roll back when beta fails");
        (await database.CountCommittedAsync("beta", id)).ShouldBe(0);
    }
}
