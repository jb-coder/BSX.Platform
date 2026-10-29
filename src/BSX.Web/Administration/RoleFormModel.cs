using System.ComponentModel.DataAnnotations;

namespace BSX.Web.Administration;

/// <summary>Form model for creating and editing a role.</summary>
public sealed class RoleFormModel
{
    /// <summary>The role name.</summary>
    [Required(ErrorMessage = "Role name is required.")]
    [StringLength(100, ErrorMessage = "Role name must be 100 characters or fewer.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>The optional description.</summary>
    [StringLength(500, ErrorMessage = "Description must be 500 characters or fewer.")]
    public string? Description { get; set; }

    /// <summary>The selected permission codes.</summary>
    public HashSet<string> Permissions { get; set; } = new(StringComparer.Ordinal);

    /// <summary>The complete permission catalog.</summary>
    public IReadOnlyList<BSX.Modules.Identity.Application.Dtos.PermissionDto> Catalog { get; set; } = [];
}
