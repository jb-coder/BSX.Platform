namespace BSX.Web.Notifications;

/// <summary>Provides transient, accessible user feedback.</summary>
public interface IToastService
{
    /// <summary>Raised when the toast collection changes.</summary>
    event Action? Changed;

    /// <summary>Gets the current notifications.</summary>
    IReadOnlyList<ToastMessage> Messages { get; }

    /// <summary>Shows a notification.</summary>
    /// <param name="kind">The visual tone.</param>
    /// <param name="title">The short title.</param>
    /// <param name="message">The optional detail.</param>
    void Show(ToastKind kind, string title, string? message = null);

    /// <summary>Shows a success notification.</summary>
    /// <param name="title">The short title.</param>
    /// <param name="message">The optional detail.</param>
    void Success(string title, string? message = null);

    /// <summary>Shows an error notification.</summary>
    /// <param name="title">The short title.</param>
    /// <param name="message">The optional detail.</param>
    void Error(string title, string? message = null);

    /// <summary>Dismisses a notification.</summary>
    /// <param name="id">The notification identifier.</param>
    void Dismiss(Guid id);
}
