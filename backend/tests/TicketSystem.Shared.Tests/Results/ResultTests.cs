using TicketSystem.Shared.Abstractions.Results;

namespace TicketSystem.Shared.Tests.Results;

public class ResultTests
{
    [Fact]
    public void Success_has_no_error()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => result.Error);
    }
}
