using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using TicketSystem.Shared.Abstractions.Domain;
using TicketSystem.Shared.Abstractions.Events;
using TicketSystem.Shared.Infrastructure.Application;
using TicketSystem.Shared.Tests.Application;

namespace TicketSystem.Shared.Tests.Events;

/// <summary>B13: the in-process bus runs every handler of the event's runtime type, in order, synchronously.</summary>
public sealed class EventPublisherTests : IDisposable
{
    private readonly EventLog _log = new();
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly IEventPublisher _publisher;

    public EventPublisherTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_log);
        services.AddApplicationHandlers(typeof(EventPublisherTests).Assembly);
        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        _publisher = _scope.ServiceProvider.GetRequiredService<IEventPublisher>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    [Fact]
    public async Task EventBus_runs_all_handlers_of_the_event_in_registration_order()
    {
        await _publisher.PublishAsync(new SampleDomainEvent(DateTimeOffset.UtcNow));

        _log.Entries.ToArray().ShouldBe(["first", "second"]);
    }

    [Fact]
    public async Task EventBus_dispatches_by_runtime_type()
    {
        IDomainEvent declaredAsInterface = new SampleDomainEvent(DateTimeOffset.UtcNow);

        await _publisher.PublishAsync(declaredAsInterface);

        _log.Entries.Count.ShouldBe(2);
    }

    [Fact]
    public async Task EventBus_delivers_integration_events()
    {
        var @event = new SampleIntegrationEvent(Guid.CreateVersion7(), DateTimeOffset.UtcNow);

        await _publisher.PublishAsync(@event);

        _log.Entries.ShouldHaveSingleItem().ShouldBe($"integration:{@event.EventId}");
    }

    [Fact]
    public async Task EventBus_with_no_handlers_is_noop()
    {
        await Should.NotThrowAsync(() => _publisher.PublishAsync(new NobodyListensDomainEvent(DateTimeOffset.UtcNow)));

        _log.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Publishing_null_is_rejected()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => _publisher.PublishAsync((IDomainEvent)null!));
        await Should.ThrowAsync<ArgumentNullException>(() => _publisher.PublishAsync((IIntegrationEvent)null!));
    }
}
