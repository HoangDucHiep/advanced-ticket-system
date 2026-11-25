using System.Data.Common;

namespace TicketSystem.Shared.Abstractions.Persistence;

/// <summary>
/// Coordinates database operations for one request using a shared connection and transaction.
/// Participating DbContexts and direct SQL operations must use these same resources so the
/// operations in a use case can be committed atomically.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Gets whether a database transaction is currently active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Gets the database connection shared by participating DbContexts and direct SQL operations.
    /// </summary>
    DbConnection Connection { get; }

    /// <summary>
    /// Gets the transaction shared by participating DbContexts and direct SQL operations.
    /// </summary>
    DbTransaction Transaction { get; }

    /// <summary>
    /// Begins the shared database transaction.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task BeginAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the shared database transaction.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CommitAsync(CancellationToken cancellationToken = default);
}