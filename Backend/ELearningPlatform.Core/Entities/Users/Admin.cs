namespace ELearningPlatform.Core;


/// <summary>
/// This Entity represents an Admin user in the e-learning platform. It inherits from the User class 
/// and can have additional properties or methods specific to admin users in the future.
/// Reviewed by Mohamed Refaat
/// </summary>
public class Admin : User
{
    public decimal ProfitPercentage { get; set; }
}
