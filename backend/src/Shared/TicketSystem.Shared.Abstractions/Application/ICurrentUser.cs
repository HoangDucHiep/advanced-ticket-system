namespace TicketSystem.Shared.Abstractions.Application;

/// <summary>
/// The caller of the current request. Implemented by the auth foundation (M0-06); the role comes from
/// the role contract, never from a JWT claim.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Gets whether the request carries a valid identity.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Gets the caller's user id, or <c>null</c> when anonymous.</summary>
    Guid? UserId { get; }

    /// <summary>Returns whether the caller currently has the given role.</summary>
    bool IsInRole(string role);
}