
namespace TicketSystem.Shared.Abstractions.Results;

public class Result
{
    private readonly Error? _error;
    protected Result(Error? error)
    {
        _error = error;
    }

    public bool IsSuccess => _error is null;
    public bool IsFailure => !IsSuccess;
    public Error Error => _error ?? throw new InvalidOperationException("Result is successful and has no error.");
    public static Result Success() => new(null);
    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(error);
    }

    public static implicit operator Result(Error error) => Failure(error);
}
