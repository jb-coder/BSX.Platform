using System.ComponentModel.DataAnnotations;

namespace BSX.Web.Administration;

/// <summary>Form model for creating a user account.</summary>
public sealed class UserCreateFormModel
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

    /// <summary>The initial password.</summary>
    [Required(ErrorMessage = "Initial password is required.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters.")]
    public string Password { get; set; } = string.Empty;
}
