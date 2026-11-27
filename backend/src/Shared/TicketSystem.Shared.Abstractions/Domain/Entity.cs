namespace TicketSystem.Shared.Abstractions.Domain;

/// <summary>Base entity with identity-based equality.</summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : struct, IEntityId<TId>
{
    /// <summary>Gets the strongly-typed identifier of this entity.</summary>
    public TId Id { get; }

    /// <summary>Initializes the entity with a non-empty typed id.</summary>
    protected Entity(TId id)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException("Entity id must not be empty.", nameof(id));
        }

        Id = id;
    }

    /// <summary>Parameterless constructor for EF materialization only; do not use in code.</summary>
    protected Entity() { }

    /// <inheritdoc />
    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other.GetType() == GetType() && Id.Equals(other.Id);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Entity<TId>);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    /// <summary>Compares two entities by runtime type and id.</summary>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left?.Equals(right) ?? right is null;

    /// <summary>Compares two entities for inequality by runtime type and id.</summary>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
