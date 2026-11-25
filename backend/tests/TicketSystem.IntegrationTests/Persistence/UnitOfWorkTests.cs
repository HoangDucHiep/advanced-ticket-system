using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using TicketSystem.Shared.Abstractions.Persistence;
using TicketSystem.Shared.Infrastructure.Persistence;

namespace TicketSystem.IntegrationTests.Persistence;

/// <summary>
/// ADR-0008: every DbContext in a scope shares ONE connection and ONE transaction.
/// This test fails if AddModuleDbContext opens a separate connection for each DbContext
/// (UseNpgsql(connectionString)): SaveChanges would then commit each schema independently,
/// and the rollback test would find leftover data.
/// </summary>
public sealed class UnitOfWorkTests(PostgresFixture database) : IClassFixture<PostgresFixture>
{
    private ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddUnitOfWork(database.ConnectionString);
        services.AddModuleDbContext<AlphaDbContext>("alpha");
        services.AddModuleDbContext<BetaDbContext>("beta");
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Both_DbContexts_share_the_UnitOfWork_connection_and_transaction()
    {
        await using var provider = BuildServices();
        await using var scope = provider.CreateAsyncScope();
        var alpha = scope.ServiceProvider.GetRequiredService<AlphaDbContext>();
        var beta = scope.ServiceProvider.GetRequiredService<BetaDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await unitOfWork.BeginAsync();

        alpha.Database.GetDbConnection().ShouldBeSameAs(unitOfWork.Connection);
        beta.Database.GetDbConnection().ShouldBeSameAs(unitOfWork.Connection);
        alpha.Database.CurrentTransaction.ShouldNotBeNull().GetDbTransaction().ShouldBeSameAs(unitOfWork.Transaction);
        beta.Database.CurrentTransaction.ShouldNotBeNull().GetDbTransaction().ShouldBeSameAs(unitOfWork.Transaction);
    }

    [Fact]
    public async Task Commit_persists_changes_to_both_schemas()
    {
        var id = Guid.CreateVersion7();

        await using (var provider = BuildServices())
        await using (var scope = provider.CreateAsyncScope())
        {
            var alpha = scope.ServiceProvider.GetRequiredService<AlphaDbContext>();
            var beta = scope.ServiceProvider.GetRequiredService<BetaDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await unitOfWork.BeginAsync();
            alpha.Notes.Add(new Note { Id = id, Text = "alpha" });
            await alpha.SaveChangesAsync();
            beta.Notes.Add(new Note { Id = id, Text = "beta" });
            await beta.SaveChangesAsync();
            await unitOfWork.CommitAsync();
        }

        (await database.CountCommittedAsync("alpha", id)).ShouldBe(1);
        (await database.CountCommittedAsync("beta", id)).ShouldBe(1);
    }

    [Fact]
    public async Task Without_commit_both_schemas_are_rolled_back()
    {
        var id = Guid.CreateVersion7();

        await using (var provider = BuildServices())
        await using (var scope = provider.CreateAsyncScope())
        {
            var alpha = scope.ServiceProvider.GetRequiredService<AlphaDbContext>();
            var beta = scope.ServiceProvider.GetRequiredService<BetaDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await unitOfWork.BeginAsync();
            alpha.Notes.Add(new Note { Id = id, Text = "alpha" });
            await alpha.SaveChangesAsync();
            beta.Notes.Add(new Note { Id = id, Text = "beta" });
            await beta.SaveChangesAsync();
            // No CommitAsync, as if the use case threw an exception midway through.
        }

        (await database.CountCommittedAsync("alpha", id)).ShouldBe(0);
        (await database.CountCommittedAsync("beta", id)).ShouldBe(0);
    }

    [Fact]
    public async Task Using_a_DbContext_before_BeginAsync_is_blocked()
    {
        await using var provider = BuildServices();
        await using var scope = provider.CreateAsyncScope();
        var alpha = scope.ServiceProvider.GetRequiredService<AlphaDbContext>();

        await Should.ThrowAsync<InvalidOperationException>(() => alpha.Notes.CountAsync());
    }
}