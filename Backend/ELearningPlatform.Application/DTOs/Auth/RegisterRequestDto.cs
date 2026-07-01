using System.ComponentModel.DataAnnotations;

namespace ELearningPlatform.Application.DTOs.Auth;

public class RegisterRequestDto
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters.")]
    [RegularExpression(@"^\p{L}[\p{L}\p{M} .'\-]*$",
        ErrorMessage = "First name may only contain letters, spaces, apostrophes, hyphens and dots.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters.")]
    [RegularExpression(@"^\p{L}[\p{L}\p{M} .'\-]*$",
        ErrorMessage = "Last name may only contain letters, spaces, apostrophes, hyphens and dots.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email format is invalid.")]
    [StringLength(256, ErrorMessage = "Email must not exceed 256 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9])\S+$",
        ErrorMessage = "Password must contain an uppercase letter, a lowercase letter, a digit, a special character and no spaces.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password confirmation is required.")]
    [Compare(nameof(Password), ErrorMessage = "Password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    // Only Student (1) and Instructor (2) may self-register. Admin (3) is rejected.
    [Range(1, 2, ErrorMessage = "Role must be 1 (Student) or 2 (Instructor).")]
    public int Role { get; set; } = 1;
}
