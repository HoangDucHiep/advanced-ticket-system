using Microsoft.EntityFrameworkCore;

namespace TicketSystem.IntegrationTests.Persistence;

// Two DbContexts simulate two modules, each with its own schema, following ADR-0008.

public sealed class Note
{
    public Guid Id { get; set; }
    public string Text { get; set; } = "";
}

public sealed class AlphaDbContext(DbContextOptions<AlphaDbContext> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("alpha");
        modelBuilder.Entity<Note>().ToTable("notes");
    }
}

public sealed class BetaDbContext(DbContextOptions<BetaDbContext> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("beta");
        modelBuilder.Entity<Note>().ToTable("notes");
    }
}