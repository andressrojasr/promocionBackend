using System.ComponentModel.DataAnnotations;

namespace PromocionBackend.Application.DTOs.Commissions;

public class CommissionMemberRequest
{
    [Required(ErrorMessage = "El cargo del integrante es obligatorio.")]
    [MaxLength(500)]
    public string CargoLabel { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe seleccionar un docente/autoridad para el cargo.")]
    [MaxLength(20)]
    public string TeacherIdentification { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del integrante es obligatorio.")]
    [MaxLength(500)]
    public string TeacherFullName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? TeacherExternalId { get; set; }
}

public class CreateCommissionRequest
{
    [Required]
    public Guid ProcessId { get; set; }

    [Required(ErrorMessage = "El tipo de comisión es obligatorio.")]
    [RegularExpression("cp|ca", ErrorMessage = "El tipo de comisión debe ser 'cp' o 'ca'.")]
    public string Type { get; set; } = string.Empty;

    public bool IsPrincipal { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    [MinLength(6, ErrorMessage = "La comisión debe tener exactamente 6 integrantes.")]
    [MaxLength(6, ErrorMessage = "La comisión debe tener exactamente 6 integrantes.")]
    public List<CommissionMemberRequest> Members { get; set; } = [];
}

public record CommissionMemberDto(
    int OrderIndex,
    string CargoLabel,
    string TeacherIdentification,
    string TeacherFullName,
    string? TeacherExternalId);

public record CommissionDto(
    Guid Id,
    Guid ProcessId,
    string Type,
    bool IsPrincipal,
    DateTime Date,
    DateTime CreatedAt,
    string CreatedByName,
    IReadOnlyList<CommissionMemberDto> Members);
