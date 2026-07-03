namespace ELearningPlatform.Core;


/// <summary>
/// This Entity represents an Admin user in the e-learning platform. It inherits from the User class 
/// and can have additional properties or methods specific to admin users in the future.
/// Reviewed by Mohamed Refaat
/// </summary>
public class Admin : User
{
    public decimal ProfitPercentage { get; set; }

    /// <summary>
    /// Platform-wide maximum number of days after enrollment during which a course
    /// may be refunded. Set by the admin in Settings; may change over time, so the
    /// value in effect at purchase is snapshotted onto each <see cref="Enrollment"/>.
    /// </summary>
    public int RefundPeriodDays { get; set; }
}
