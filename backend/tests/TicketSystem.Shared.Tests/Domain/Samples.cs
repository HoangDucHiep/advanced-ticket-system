using TicketSystem.Shared.Abstractions.Domain;

namespace TicketSystem.Shared.Tests.Domain;

// Test-only samples that prove the domain primitives can be used as B9 describes.
// Real ids, entities and aggregates live in their modules.

internal readonly record struct SampleId(Guid Value) : IEntityId<SampleId>
{
    public static SampleId From(Guid value) => new(value);
}

internal sealed class SampleEntity : Entity<SampleId>
{
    public SampleEntity(SampleId id, string name)
        : base(id)
    {
        Name = name;
    }

    public string Name { get; private set; }
}

internal sealed class OtherSampleEntity : Entity<SampleId>
{
    public OtherSampleEntity(SampleId id)
        : base(id)
    {
    }
}
