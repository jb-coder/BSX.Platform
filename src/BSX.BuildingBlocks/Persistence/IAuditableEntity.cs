namespace BSX.BuildingBlocks.Persistence;

/// <summary>
/// Exposes the standard auditing fields.
/// Auditing is populated automatically by the persistence layer.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// Gets or sets the creation timestamp, in UTC.
    /// </summary>
    DateTimeOffset CreatedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user that created the entity.
    /// </summary>
    string CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the last modification timestamp, in UTC.
    /// </summary>
    DateTimeOffset? UpdatedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user that last modified the entity.
    /// </summary>
    string? UpdatedBy { get; set; }
}
