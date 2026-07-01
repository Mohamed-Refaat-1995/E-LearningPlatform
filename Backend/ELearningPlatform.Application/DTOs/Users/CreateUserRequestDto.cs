using ELearningPlatform.Core;

namespace ELearningPlatform.Application.DTOs.Users;

public class CreateUserRequestDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRoleEnum Role { get; set; }
    public string? Bio { get; set; }
}

public class UpdateProfileRequestDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Bio { get; set; }
}

public class SetActiveRequestDto
{
    public bool IsActive { get; set; }
}

public class ChangeRoleRequestDto
{
    public UserRoleEnum Role { get; set; }
}
