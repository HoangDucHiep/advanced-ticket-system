using Npgsql;

using Testcontainers.PostgreSql;

namespace TicketSystem.IntegrationTests.Persistence;

/// <summary>A real PostgreSQL instance (Testcontainers) shared by the test class, with two schemas simulating modules.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            CREATE SCHEMA alpha;
            CREATE TABLE alpha.notes ("Id" uuid PRIMARY KEY, "Text" text NOT NULL);
            CREATE SCHEMA beta;
            CREATE TABLE beta.notes ("Id" uuid PRIMARY KEY, "Text" text NOT NULL);
            """,
            connection);
        await command.ExecuteNonQueryAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>Counts rows through a separate connection, which only sees committed data.</summary>
    public async Task<long> CountCommittedAsync(string schema, Guid id)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            $"SELECT count(*) FROM {schema}.notes WHERE \"Id\" = @id", connection);
        command.Parameters.AddWithValue("id", id);
        return (long)(await command.ExecuteScalarAsync())!;
    }
}