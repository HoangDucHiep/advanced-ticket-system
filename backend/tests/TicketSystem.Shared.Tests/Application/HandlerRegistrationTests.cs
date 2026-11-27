using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using TicketSystem.Shared.Abstractions.Application;
using TicketSystem.Shared.Abstractions.Events;
using TicketSystem.Shared.Infrastructure.Application;

namespace TicketSystem.Shared.Tests.Application;

/// <summary>B6: one call in Add&lt;Module&gt;Module registers every handler of the module's assembly.</summary>
public sealed class HandlerRegistrationTests
{
    private static ServiceCollection ScanThisAssembly()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<EventLog>();
        services.AddApplicationHandlers(typeof(HandlerRegistrationTests).Assembly);
        return services;
    }

    [Fact]
    public void Scan_registers_every_handler_in_assembly()
    {
        using var provider = ScanThisAssembly().BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var services = scope.ServiceProvider;

        services.GetService<ICommandHandler<GreetCommand, string>>().ShouldNotBeNull();
        services.GetService<ICommandHandler<RejectCommand, string>>().ShouldNotBeNull();
        services.GetService<IQueryHandler<DoubleQuery, int>>().ShouldNotBeNull();
        services.GetServices<IEventHandler<SampleDomainEvent>>().Count().ShouldBe(2);
        services.GetServices<IEventHandler<SampleIntegrationEvent>>().Count().ShouldBe(1);
        services.GetService<IEventPublisher>().ShouldNotBeNull();
    }

    [Fact]
    public void Handlers_and_bus_are_scoped()
    {
        var services = ScanThisAssembly();

        Lifetime<ICommandHandler<GreetCommand, string>>(services).ShouldBe(ServiceLifetime.Scoped);
        Lifetime<IQueryHandler<DoubleQuery, int>>(services).ShouldBe(ServiceLifetime.Scoped);
        Lifetime<IEventHandler<SampleIntegrationEvent>>(services).ShouldBe(ServiceLifetime.Scoped);
        Lifetime<IEventPublisher>(services).ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void Scan_ignores_non_handlers_and_abstracts()
    {
        var services = ScanThisAssembly();

        services.ShouldNotContain(d => d.ServiceType == typeof(ICommandHandler<AbstractOnlyCommand, string>));
        services.ShouldNotContain(d => d.ImplementationType == typeof(AbstractOnlyHandler));
        services.ShouldNotContain(d => d.ImplementationType == typeof(LooksLikeAHandler));
        services.ShouldNotContain(d => d.ServiceType == typeof(LooksLikeAHandler));
    }

    [Fact]
    public void Scanning_twice_does_not_duplicate_event_handlers()
    {
        var services = ScanThisAssembly();
        services.AddApplicationHandlers(typeof(HandlerRegistrationTests).Assembly);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetServices<IEventHandler<SampleDomainEvent>>().Count().ShouldBe(2);
    }

    [Fact]
    public void Resolved_handler_is_wrapped_by_the_decorator()
    {
        using var provider = ScanThisAssembly().BuildServiceProvider();
        using var scope = provider.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<GreetCommand, string>>();

        handler.ShouldNotBeOfType<GreetHandler>();
    }

    private static ServiceLifetime Lifetime<TService>(IServiceCollection services) =>
        services.Last(d => d.ServiceType == typeof(TService)).Lifetime;
}
