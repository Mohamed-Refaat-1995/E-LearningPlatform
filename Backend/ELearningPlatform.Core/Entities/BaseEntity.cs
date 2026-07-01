namespace ELearningPlatform.Core;

/// <summary>
/// This entity serves as a base class for all entities in the e-learning platform.
/// It provides common properties such as Id, CreatedAt, UpdatedAt, and IsDeleted that can be inherited by other entity classes.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
}
