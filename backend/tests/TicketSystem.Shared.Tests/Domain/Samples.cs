using TicketSystem.Shared.Abstractions.Domain;
using TicketSystem.Shared.Abstractions.Results;

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

internal sealed record SampleHappened(int Sequence, DateTimeOffset OccurredAt) : IDomainEvent;

internal sealed class SampleAggregate : AggregateRoot<SampleId>
{
    public SampleAggregate(SampleId id)
        : base(id)
    {
    }

    public void DoSomething(int sequence, DateTimeOffset now) =>
        RaiseDomainEvent(new SampleHappened(sequence, now));
}

internal sealed record SampleMoney : ValueObject
{
    private static readonly Error Negative = new()
    {
        Code = "SAMPLE_MONEY_NEGATIVE",
        Title = "Amount must not be negative",
        Action = "Use a positive amount",
        Type = ErrorType.Validation,
    };

    private SampleMoney(long amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public long Amount { get; }

    public string Currency { get; }

    public static Result<SampleMoney> Create(long amount, string currency) =>
        amount < 0 ? Negative : new SampleMoney(amount, currency);
}
