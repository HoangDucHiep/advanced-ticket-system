using System.Reflection;
using System.Runtime.CompilerServices;
using Shouldly;
using TicketSystem.Shared.Abstractions.Domain;

namespace TicketSystem.Shared.Tests.Domain;

public class EntityTests
{
    private static readonly SampleId FixedId = SampleId.From(Guid.Parse("0193a3c1-7a2b-7c00-8000-000000000001"));
    private static readonly SampleId OtherFixedId = SampleId.From(Guid.Parse("0193a3c1-7a2b-7c00-8000-000000000002"));

    [Fact]
    public void Entities_with_same_id_are_equal()
    {
        var first = new SampleEntity(FixedId, "Alice");
        var second = new SampleEntity(FixedId, "Bob");

        first.ShouldNotBeSameAs(second);
        first.Equals(second).ShouldBeTrue();
        (first == second).ShouldBeTrue();
        (first != second).ShouldBeFalse();
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Fact]
    public void Entities_with_different_id_or_type_are_not_equal()
    {
        var first = new SampleEntity(FixedId, "Alice");
        var differentId = new SampleEntity(OtherFixedId, "Alice");
        var differentType = new OtherSampleEntity(FixedId);

        first.Equals(differentId).ShouldBeFalse();
        (first == differentId).ShouldBeFalse();
        (first != differentId).ShouldBeTrue();

        first.Equals(differentType).ShouldBeFalse();
        (first == differentType).ShouldBeFalse();
        (first != differentType).ShouldBeTrue();

        first.Equals(null).ShouldBeFalse();
        (first == null).ShouldBeFalse();
        (null == first).ShouldBeFalse();
    }

    [Theory]
    [InlineData(typeof(Entity<SampleId>))]
    [InlineData(typeof(AggregateRoot<SampleId>))]
    public void Entity_has_no_public_setters(Type baseType)
    {
        var properties = baseType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        properties.ShouldContain(p => p.Name == "Id");
        properties
            .Where(p => p.SetMethod is { IsPublic: true })
            .Select(p => p.Name)
            .ShouldBeEmpty();
    }

    [Fact]
    public void TypedId_new_is_uuidv7_and_ordered()
    {
        var ids = new List<SampleId>();
        for (var i = 0; i < 5; i++)
        {
            ids.Add(EntityId.New<SampleId>());
            // UUIDv7 is only ordered across milliseconds; inside one millisecond the tail is random.
            Thread.Sleep(2);
        }

        ids.ShouldAllBe(id => id.Value.Version == 7);
        var texts = ids.Select(id => id.Value.ToString()).ToList();
        texts.ShouldBe(texts.Order(StringComparer.Ordinal).ToList());
        texts.Distinct().Count().ShouldBe(ids.Count);
    }

    [Fact]
    public void TypedId_default_is_rejected_by_entity()
    {
        default(SampleId).Value.ShouldBe(Guid.Empty);
        EntityId.New<SampleId>().ShouldNotBe(default);

        Should.Throw<ArgumentException>(() => new SampleEntity(default, "Alice"));
        Should.Throw<ArgumentException>(() => new SampleAggregate(default));
    }
}

public class AggregateRootTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 27, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AggregateRoot_collects_domain_events()
    {
        var aggregate = new SampleAggregate(EntityId.New<SampleId>());

        aggregate.DoSomething(1, Now);
        aggregate.DoSomething(2, Now);
        aggregate.DoSomething(3, Now);

        aggregate.DequeueDomainEvents().ShouldBe(
        [
            new SampleHappened(1, Now),
            new SampleHappened(2, Now),
            new SampleHappened(3, Now),
        ]);
    }

    [Fact]
    public void Dequeue_returns_events_and_clears()
    {
        var aggregate = new SampleAggregate(EntityId.New<SampleId>());
        aggregate.DoSomething(1, Now);
        aggregate.DoSomething(2, Now);

        var first = aggregate.DequeueDomainEvents();
        var second = aggregate.DequeueDomainEvents();
        aggregate.DoSomething(3, Now);

        // The returned list is a snapshot: clearing or raising later must not change it.
        first.Count.ShouldBe(2);
        second.ShouldBeEmpty();
        aggregate.DequeueDomainEvents().ShouldBe([new SampleHappened(3, Now)]);
    }
}

public class ValueObjectTests
{
    [Fact]
    public void ValueObject_equality_is_by_value()
    {
        var first = SampleMoney.Create(100_000, "VND").Value;
        var same = SampleMoney.Create(100_000, "VND").Value;
        var different = SampleMoney.Create(200_000, "VND").Value;

        first.ShouldNotBeSameAs(same);
        first.ShouldBe(same);
        (first == same).ShouldBeTrue();
        first.GetHashCode().ShouldBe(same.GetHashCode());
        first.ShouldNotBe(different);
        (first != different).ShouldBeTrue();
    }

    [Theory]
    [InlineData(typeof(ValueObject))]
    [InlineData(typeof(SampleMoney))]
    public void ValueObject_is_immutable(Type type)
    {
        type.IsAssignableTo(typeof(ValueObject)).ShouldBeTrue();

        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod is { IsPublic: true } setter && !IsInitOnly(setter))
            .Select(p => p.Name)
            .ShouldBeEmpty();
    }

    private static bool IsInitOnly(MethodInfo setter) =>
        setter.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));
}
