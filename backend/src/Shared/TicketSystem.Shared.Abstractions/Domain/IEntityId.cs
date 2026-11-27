
namespace TicketSystem.Shared.Abstractions.Domain;


/// <summary>Non-generic marker for every typed id; scanned by EF / Dapper conventions.</summary>
public interface IEntityId
{
    /// <summary>Gets the underlying <see cref="Guid"/> value.</summary>
    Guid Value { get; }
}

/// <summary>A typed id that can be rebuilt from its <see cref="Guid"/> without reflection.</summary>
public interface IEntityId<out TSelf> : IEntityId
    where TSelf : struct, IEntityId<TSelf>
{
    /// <summary>Creates a typed id from an existing <see cref="Guid"/>.</summary>
    static abstract TSelf From(Guid value);
}