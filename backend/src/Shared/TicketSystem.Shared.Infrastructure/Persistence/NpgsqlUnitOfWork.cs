using System.Data.Common;

using Microsoft.EntityFrameworkCore;

using Npgsql;

using TicketSystem.Shared.Abstractions.Persistence;

namespace TicketSystem.Shared.Infrastructure.Persistence;

/// <summary>
/// Coordinates a PostgreSQL connection and transaction shared by the enlisted EF Core contexts.
/// </summary>
/// <remarks>
/// The connection is created when first accessed. Call <see cref="BeginAsync"/> before issuing
/// database operations, then call <see cref="CommitAsync"/> to commit them. Disposing an
/// uncommitted unit of work rolls its transaction back.
/// </remarks>
public sealed class NpgsqlUnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private bool _disposed;
    private readonly NpgsqlDataSource _dataSource;
    private readonly List<DbContext> _contexts = [];
    private NpgsqlConnection? _connection;
    private NpgsqlTransaction? _transaction;
    private bool _committed;

    /// <summary>
    /// Initializes a unit of work that creates connections from the supplied data source.
    /// </summary>
    /// <param name="dataSource">The data source used to create the PostgreSQL connection.</param>
    public NpgsqlUnitOfWork(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    /// <summary>
    /// Gets whether a transaction has been started and has not been committed.
    /// </summary>
    public bool IsActive => _transaction is not null && !_committed;

    /// <summary>
    /// Gets the lazily created PostgreSQL connection shared by enlisted contexts.
    /// The connection is opened when <see cref="BeginAsync"/> is called.
    /// </summary>
    public DbConnection Connection => _connection ??= _dataSource.CreateConnection();

    /// <summary>
    /// Gets the shared transaction.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The transaction has not been started.
    /// </exception>
    public DbTransaction Transaction => _transaction ?? throw new InvalidOperationException("Transaction has not been started. Call BeginAsync() first.");

    /// <summary>
    /// Opens the shared connection, starts a transaction, and attaches previously enlisted contexts to it.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel opening the connection or transaction.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">A transaction has already been started.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException("Transaction has already been started.");
        }

        var connection = (NpgsqlConnection)Connection;
        await connection.OpenAsync(cancellationToken);
        _transaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (var context in _contexts)
        {
            await context.Database.UseTransactionAsync(_transaction, cancellationToken);
        }
    }

    /// <summary>
    /// Commits the shared transaction.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the commit operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">No uncommitted transaction has been started.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Transaction has not been started. Call BeginAsync() first.");
        }
        await _transaction!.CommitAsync(cancellationToken);
        _committed = true;
    }

    /// <summary>
    /// Enlists a context so its database operations use this unit of work's connection and transaction.
    /// </summary>
    /// <param name="context">The context to enlist.</param>
    internal void Enlist(DbContext context)
    {
        _contexts.Add(context);
        if (_transaction is not null)
        {
            context.Database.UseTransaction(_transaction);
        }
    }

    /// <summary>
    /// Rolls back the transaction if it has not been committed, then asynchronously disposes
    /// the transaction and connection.
    /// </summary>
    /// <returns>A task that represents the asynchronous disposal operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        if (_transaction is not null)
        {
            if (!_committed)
            {
                await _transaction.RollbackAsync();
            }
            await _transaction.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}