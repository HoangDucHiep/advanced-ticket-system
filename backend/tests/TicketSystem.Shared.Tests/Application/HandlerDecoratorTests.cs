using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shouldly;

using TicketSystem.Shared.Abstractions.Application;
using TicketSystem.Shared.Abstractions.Persistence;
using TicketSystem.Shared.Infrastructure.Application;

namespace TicketSystem.Shared.Tests.Application;

/// <summary>B6: the shared decorator observes every handler (log + trace) and never owns the transaction (B7).</summary>
public sealed class HandlerDecoratorTests : IDisposable
{
    private readonly CapturingLoggerProvider _logs = new();
    private readonly CountingUnitOfWork _unitOfWork = new();
    private readonly ServiceProvider _provider;

    public HandlerDecoratorTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(_logs));
        services.AddSingleton<EventLog>();
        services.AddSingleton<IUnitOfWork>(_unitOfWork);
        services.AddApplicationHandlers(typeof(HandlerDecoratorTests).Assembly);
        _provider = services.BuildServiceProvider();
    }

    public void Dispose() => _provider.Dispose();

    private async Task RunCommandAsync<TCommand, TResult>(TCommand command)
        where TCommand : ICommand<TResult>
    {
        using var scope = _provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<TCommand, TResult>>();
        await handler.HandleAsync(command, CancellationToken.None);
    }

    [Fact]
    public async Task Decorator_passes_the_result_through()
    {
        using var scope = _provider.CreateScope();
        var greet = scope.ServiceProvider.GetRequiredService<ICommandHandler<GreetCommand, string>>();
        var reject = scope.ServiceProvider.GetRequiredService<ICommandHandler<RejectCommand, string>>();
        var query = scope.ServiceProvider.GetRequiredService<IQueryHandler<DoubleQuery, int>>();

        (await greet.HandleAsync(new GreetCommand("An"), CancellationToken.None)).Value.ShouldBe("hello An");
        (await reject.HandleAsync(new RejectCommand(), CancellationToken.None)).Error.ShouldBeSameAs(RejectHandler.Rejected);
        (await query.HandleAsync(new DoubleQuery(21), CancellationToken.None)).Value.ShouldBe(42);
    }

    [Fact]
    public async Task Decorator_logs_success_with_duration()
    {
        await RunCommandAsync<GreetCommand, string>(new GreetCommand("An"));

        var entries = _logs.Logs.Where(l => Equals(l.State.GetValueOrDefault("Handler"), nameof(GreetHandler))).ToList();
        entries.Count.ShouldBe(2, "one start entry and one end entry");
        var end = entries[^1];
        end.State.ShouldContainKey("ElapsedMs");
        end.State.ShouldNotContainKey("ErrorCode");
    }

    [Fact]
    public async Task Decorator_logs_failure_code()
    {
        await RunCommandAsync<RejectCommand, string>(new RejectCommand());

        var end = _logs.Logs.Last(l => Equals(l.State.GetValueOrDefault("Handler"), nameof(RejectHandler)));
        end.Level.ShouldBe(LogLevel.Warning);
        end.State["ErrorCode"].ShouldBe("SAMPLE_REJECTED");
        end.State.ShouldContainKey("ElapsedMs");
    }

    [Fact]
    public async Task Decorator_logs_queries_too()
    {
        using var scope = _provider.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IQueryHandler<DoubleQuery, int>>();

        await query.HandleAsync(new DoubleQuery(1), CancellationToken.None);

        _logs.Logs.Count(l => Equals(l.State.GetValueOrDefault("Handler"), nameof(DoubleHandler))).ShouldBe(2);
    }

    [Fact]
    public async Task Decorator_creates_activity_named_after_handler()
    {
        var started = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ApplicationDiagnostics.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = started.Add,
        };
        ActivitySource.AddActivityListener(listener);

        await RunCommandAsync<RejectCommand, string>(new RejectCommand());

        var activity = started.Where(a => a.DisplayName == nameof(RejectHandler)).ShouldHaveSingleItem();
        activity.DisplayName.ShouldBe(nameof(RejectHandler));
        activity.GetTagItem("error.code").ShouldBe("SAMPLE_REJECTED");
    }

    [Fact]
    public async Task Exception_propagates_and_marks_activity_as_error()
    {
        var stopped = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ApplicationDiagnostics.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = stopped.Add,
        };
        ActivitySource.AddActivityListener(listener);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            RunCommandAsync<ExplodeCommand, string>(new ExplodeCommand()));

        stopped.Where(a => a.DisplayName == nameof(ExplodeHandler)).ShouldHaveSingleItem().Status.ShouldBe(ActivityStatusCode.Error);
    }

    [Fact]
    public async Task Decorator_does_not_open_transaction()
    {
        await RunCommandAsync<TouchNothingCommand, string>(new TouchNothingCommand());
        await RunCommandAsync<RejectCommand, string>(new RejectCommand());

        _unitOfWork.BeginCalls.ShouldBe(0);
        _unitOfWork.CommitCalls.ShouldBe(0);
    }
}
