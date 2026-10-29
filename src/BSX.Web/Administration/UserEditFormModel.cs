using System.ComponentModel.DataAnnotations;

namespace BSX.Web.Administration;

/// <summary>Form model for editing a user's profile fields.</summary>
public sealed class UserEditFormModel
{
    /// <summary>The display name.</summary>
    [Required(ErrorMessage = "Display name is required.")]
    [StringLength(200, ErrorMessage = "Display name must be 200 characters or fewer.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>The globally unique email address.</summary>
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(320, ErrorMessage = "Email must be 320 characters or fewer.")]
    public string Email { get; set; } = string.Empty;
}
