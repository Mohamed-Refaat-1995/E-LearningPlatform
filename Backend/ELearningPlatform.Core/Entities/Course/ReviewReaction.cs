namespace ELearningPlatform.Core;

/// <summary>
/// A single emoji reaction by a user (student or instructor) on a review.
/// One reaction per (ReviewId, UserId) — picking a new emoji replaces the old one,
/// picking the same emoji again removes it (toggle), enforced by the controller.
/// </summary>
public class ReviewReaction : BaseEntity
{
    public int ReviewId { get; set; }
    public Review Review { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Emoji { get; set; } = string.Empty;
}
