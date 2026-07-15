namespace Respira.ServiceDefaults.Models;

/// <summary>
/// Base entity class
/// </summary>
public class Base
{
    /// <summary>
    /// Entity ID (UUID v7)
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();
    /// <summary>
    /// Boolean flag for soft delete
    /// </summary>
    public bool IsDeleted { get; set; }
    /// <summary>
    /// Create timestamp. Default to now
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Update timestamp. Default to now
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Delete (soft delete) timestamp. Default to null
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }
}