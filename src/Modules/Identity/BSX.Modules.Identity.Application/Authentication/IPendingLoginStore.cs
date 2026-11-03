namespace BSX.Modules.Identity.Application.Authentication;

/// <summary>
/// Stores verified sign-ins that are waiting for workspace selection (web sign-in flow). The
/// implementation is server-side and short-lived; it must never retain the user's password.
/// </summary>
public interface IPendingLoginStore
{
    /// <summary>Stores the pending sign-in and returns its selection and secret handles.</summary>
    /// <param name="pendingLogin">The verified sign-in.</param>
    PendingLoginHandle Save(PendingLogin pendingLogin);

    /// <summary>Reads a pending sign-in by its secret token.</summary>
    /// <param name="secretToken">The secret token held in the HttpOnly cookie.</param>
    PendingLogin? Get(string? secretToken);

    /// <summary>Reads a pending sign-in by its non-secret selection handle.</summary>
    /// <param name="selectionId">The selection handle shown in the workspace chooser URL.</param>
    PendingLogin? GetBySelectionId(string? selectionId);

    /// <summary>Atomically removes and returns a pending sign-in by its secret token.</summary>
    /// <param name="secretToken">The secret token held in the HttpOnly cookie.</param>
    PendingLogin? Consume(string? secretToken);

    /// <summary>Removes a pending sign-in by its secret token.</summary>
    /// <param name="secretToken">The secret token held in the HttpOnly cookie.</param>
    void Remove(string? secretToken);
}
