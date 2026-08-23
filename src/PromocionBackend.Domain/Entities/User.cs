using PromocionBackend.Domain.Constants;

namespace PromocionBackend.Domain.Entities;

/// <summary>
/// Usuario del sistema. Se crea automáticamente en el primer inicio de sesión
/// con el rol <see cref="Roles.Teacher"/>, salvo que su correo esté en la semilla de roles.
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Identification { get; set; }
    public string? TeacherId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.Teacher;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
