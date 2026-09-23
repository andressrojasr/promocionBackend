using System.ComponentModel.DataAnnotations;

namespace PromocionBackend.Application.DTOs.ReviewSessions;

public class CreateReviewSessionRequest
{
    [Required]
    public Guid ProcessId { get; set; }

    [Required(ErrorMessage = "El tipo de sesión es obligatorio.")]
    [RegularExpression("cp|ca", ErrorMessage = "El tipo debe ser 'cp' o 'ca'.")]
    public string Type { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe seleccionar la comisión que revisará en esta sesión.")]
    public Guid CommissionId { get; set; }

    [Required(ErrorMessage = "Debe seleccionar la facultad.")]
    [MaxLength(50)]
    public string FacultyId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de la facultad es obligatorio.")]
    [MaxLength(300)]
    public string FacultyName { get; set; } = string.Empty;
}

public record ReviewSessionDto(
    Guid Id,
    Guid ProcessId,
    string ProcessName,
    string Type,
    Guid CommissionId,
    DateTime CommissionDate,
    bool CommissionIsPrincipal,
    string FacultyId,
    string FacultyName,
    Guid CreatedByUserId,
    string CreatedByName,
    DateTime CreatedAt,
    DateTime? ClosedAt);
