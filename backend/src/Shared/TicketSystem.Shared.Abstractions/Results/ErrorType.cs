namespace TicketSystem.Shared.Abstractions.Results;

/// <summary>
/// Categorizes expected domain or application failures for HTTP status mapping.
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// Input failed validation rules (maps to 400).
    /// </summary>
    Validation,

    /// <summary>
    /// Caller is not authenticated (maps to 401).
    /// </summary>
    Unauthorized,

    /// <summary>
    /// Caller is authenticated but lacks permission (maps to 403).
    /// </summary>
    Forbidden,

    /// <summary>
    /// Requested resource does not exist (maps to 404).
    /// </summary>
    NotFound,

    /// <summary>
    /// Operation conflicts with current resource state (maps to 409).
    /// </summary>
    Conflict,

    /// <summary>
    /// Operation violates a domain invariant or business rule (maps to 422).
    /// </summary>
    BusinessRule,
}
