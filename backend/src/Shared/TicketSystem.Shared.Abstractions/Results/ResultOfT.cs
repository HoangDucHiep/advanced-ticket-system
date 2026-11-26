using System.Diagnostics.CodeAnalysis;

namespace TicketSystem.Shared.Abstractions.Results;


public sealed class Result<T> : Result
{
    [AllowNull]
    private readonly T _value;

    private Result(T? value, Error? error) : base(error) => _value = value;

    public T Value => IsFailure
        ? throw new InvalidOperationException("A failed result has no value.")
        : _value;

    public static Result<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Result<T>(value, null);
    }

    public static new Result<T> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<T>(default, error);
    }

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);
}