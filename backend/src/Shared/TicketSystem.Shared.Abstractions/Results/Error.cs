namespace TicketSystem.Shared.Abstractions.Results;

/// <summary>
/// Represents an immutable domain or application error returned by a <c>Result</c>.
/// </summary>
public sealed record Error
{
    /// <summary>
    /// Gets the stable, machine-readable error code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the human-readable summary of the error.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the suggested follow-up action for the caller or end user.
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Gets the error category used to map to an HTTP status code.
    /// </summary>
    public required ErrorType Type { get; init; }

    /// <summary>
    /// Gets optional structured metadata associated with the error.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Extensions { get; init; }
}
