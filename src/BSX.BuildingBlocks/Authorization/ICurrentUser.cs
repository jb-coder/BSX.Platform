namespace BSX.BuildingBlocks.Authorization;

/// <summary>
/// Provides read-only access to the currently authenticated user.
/// Implemented by the hosting application and consumed by any module.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// Gets the identifier of the current user, if authenticated.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets the email of the current user, if available.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Gets a value indicating whether the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the permissions granted to the current user.
    /// </summary>
    IReadOnlyCollection<string> Permissions { get; }
}
