namespace TicketSystem.Shared.Abstractions.Domain;

/// <summary>Factory helpers for creating strongly-typed entity ids.</summary>
public static class EntityId
{
    /// <summary>The only place that creates new ids: UUIDv7, time-ordered (C-08).</summary>
    public static TId New<TId>()
        where TId : struct, IEntityId<TId> =>
        TId.From(Guid.CreateVersion7());
}