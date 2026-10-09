namespace BSX.BuildingBlocks.Persistence;

/// <summary>
/// Marks an entity that is soft-deleted rather than removed from the store.
/// Hard deletes should remain rare and explicit.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Gets or sets a value indicating whether the entity is deleted.
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the deletion timestamp, in UTC.
    /// </summary>
    DateTimeOffset? DeletedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user that deleted the entity.
    /// </summary>
    string? DeletedBy { get; set; }
}
