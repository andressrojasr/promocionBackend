using System.ComponentModel.DataAnnotations;

namespace PromocionBackend.Application.DTOs.Users;

public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    bool IsActive,
    string? Identification,
    string? TeacherId,
    string? CurrentPosition,
    DateTime? LastLoginAt,
    DateTime CreatedAt);

public class UpdateRoleRequest
{
    [Required(ErrorMessage = "El rol es obligatorio.")]
    public string Role { get; set; } = string.Empty;
}

public class UpdateStatusRequest
{
    public bool IsActive { get; set; }
}
