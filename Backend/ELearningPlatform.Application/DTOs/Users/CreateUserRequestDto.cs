using ELearningPlatform.Core.Enums;

namespace ELearningPlatform.Application.DTOs.Users;

public record CreateUserRequestDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    UserRole Role,
    string? Bio
);

public record UpdateProfileRequestDto(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? Bio
);

public record SetActiveRequestDto(bool IsActive);

public record ChangeRoleRequestDto(UserRole Role);
