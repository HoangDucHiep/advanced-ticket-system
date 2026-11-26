using Shouldly;

namespace TicketSystem.Shared.Tests.Domain;

public class EntityTests
{
    [Fact]
    public void Entities_with_same_id_are_equal()
    {
        var id = SampleId.From(Guid.Parse("0193a3c1-7a2b-7c00-8000-000000000001"));
        var first = new SampleEntity(id, "Alice");
        var second = new SampleEntity(id, "Bob");

        first.ShouldNotBeSameAs(second);
        first.Equals(second).ShouldBeTrue();
        (first == second).ShouldBeTrue();
        (first != second).ShouldBeFalse();
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }
}
