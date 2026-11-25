using System.Data.Common;

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TicketSystem.Shared.Infrastructure.Persistence;

/// <summary>
/// Enforces that a DbContext is used within a unit of work.
/// </summary>
internal sealed class RequireUnitOfWorkInterceptor : DbConnectionInterceptor
{
    public override InterceptionResult ConnectionOpening(
        DbConnection connection, ConnectionEventData eventData, InterceptionResult result) =>
        throw NotInUnitOfWork();

    public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection, ConnectionEventData eventData, InterceptionResult result,
        CancellationToken cancellationToken = default) =>
        throw NotInUnitOfWork();

    private static InvalidOperationException NotInUnitOfWork() =>
        new("A DbContext is being used outside of a unit of work. Ensure that a unit of work is started before using the DbContext.");
}