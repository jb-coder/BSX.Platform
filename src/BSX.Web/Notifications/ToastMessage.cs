namespace BSX.Web.Notifications;

/// <summary>The visual tone of a toast.</summary>
public enum ToastKind
{
    /// <summary>An informational message.</summary>
    Info = 0,

    /// <summary>A successful operation.</summary>
    Success = 1,

    /// <summary>A recoverable warning.</summary>
    Warning = 2,

    /// <summary>An error that needs attention.</summary>
    Error = 3,
}

/// <summary>A transient notification.</summary>
/// <param name="Id">The notification identifier.</param>
/// <param name="Kind">The visual tone.</param>
/// <param name="Title">The short title.</param>
/// <param name="Message">The optional detail.</param>
public sealed record ToastMessage(Guid Id, ToastKind Kind, string Title, string? Message);
