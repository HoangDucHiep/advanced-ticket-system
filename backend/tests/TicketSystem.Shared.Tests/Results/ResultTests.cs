using Shouldly;
using TicketSystem.Shared.Abstractions.Results;

namespace TicketSystem.Shared.Tests.Results;

public class ResultTests
{
    private static Error SampleError(IReadOnlyDictionary<string, object?>? extensions = null) => new()
    {
        Code = "SEATS_UNAVAILABLE",
        Title = "Some seats are taken",
        Action = "Pick other seats",
        Type = ErrorType.Conflict,
        Extensions = extensions,
    };

    [Fact]
    public void Success_has_no_error()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void Failure_exposes_error()
    {
        var error = SampleError();

        var result = Result.Failure(error);

        result.IsFailure.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(error);
    }

    [Fact]
    public void Failure_with_null_error_is_rejected()
    {
        // The null-forgiving operator is deliberate: the test feeds an illegal input on purpose.
        Should.Throw<ArgumentNullException>(() => Result.Failure(null!));
    }

    [Fact]
    public void Failure_value_access_throws()
    {
        var result = Result<int>.Failure(SampleError());

        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Success_value_is_returned()
    {
        var result = Result<string>.Success("hello");

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("hello");
        Should.Throw<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void Implicit_conversion_from_value()
    {
        Result<int> result = 5;

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(5);
    }

    [Fact]
    public void Implicit_conversion_from_error()
    {
        var error = SampleError(new Dictionary<string, object?> { ["unavailableSeatIds"] = new[] { "A1" } });

        Result<int> generic = error;
        Result plain = error;

        generic.IsFailure.ShouldBeTrue();
        generic.Error.ShouldBeSameAs(error);
        plain.IsFailure.ShouldBeTrue();
        plain.Error.ShouldBeSameAs(error);
    }

    [Fact]
    public void Generic_result_is_usable_as_result()
    {
        Result success = Result<int>.Success(1);
        Result failure = Result<int>.Failure(SampleError());

        success.IsSuccess.ShouldBeTrue();
        failure.IsFailure.ShouldBeTrue();
        failure.Error.Code.ShouldBe("SEATS_UNAVAILABLE");
    }

    [Fact]
    public void Error_extensions_are_preserved()
    {
        var seatIds = new[] { "A1", "A2" };
        var error = SampleError(new Dictionary<string, object?> { ["unavailableSeatIds"] = seatIds });

        var plain = Result.Failure(error);
        var generic = Result<int>.Failure(error);

        plain.Error.Extensions.ShouldNotBeNull();
        plain.Error.Extensions["unavailableSeatIds"].ShouldBeSameAs(seatIds);
        generic.Error.Extensions.ShouldNotBeNull();
        generic.Error.Extensions["unavailableSeatIds"].ShouldBeSameAs(seatIds);
    }

    [Fact]
    public void PagedResponse_exposes_page_metadata()
    {
        var response = new PagedResponse<string>(["a", "b"], Page: 2, PageSize: 20, TotalCount: 45);

        response.Items.ShouldBe(["a", "b"]);
        response.Page.ShouldBe(2);
        response.PageSize.ShouldBe(20);
        response.TotalCount.ShouldBe(45);
    }
}
