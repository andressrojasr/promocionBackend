using System.ComponentModel.DataAnnotations;

namespace PromocionBackend.Application.DTOs.Processes;

/// <summary>
/// Configuración de requisitos de una transición. Se usa tanto para crear procesos
/// como para exponer los valores por defecto del reglamento.
/// </summary>
public class RequirementConfigDto
{
    [Required]
    public string FromPosition { get; set; } = string.Empty;

    [Required]
    public string ToPosition { get; set; } = string.Empty;

    [Range(0, 50)]
    public int MinYearsInPosition { get; set; }

    [Range(0, 100)]
    public int MinPublications { get; set; }

    [Range(0, 100)]
    public int MinPublicationsInOtherLanguage { get; set; }

    [Range(0, 100)]
    public decimal MinEvaluationScorePct { get; set; }

    [Range(0, 2000)]
    public int MinTrainingHours { get; set; }

    [Range(1, 20)]
    public int TrainingWindowYears { get; set; } = 3;

    [Range(0, 100)]
    public decimal? MinPedagogicalTrainingPct { get; set; }

    [Range(0, 2000)]
    public int? MinGivenTrainingHours { get; set; }

    [Range(0, 600)]
    public int? MinProjectMonths { get; set; }

    public string ProjectRoleScope { get; set; } = "any";

    public bool ApplyRoleMultipliers { get; set; }

    [Range(0, 50)]
    public int? MinInternationalProjects { get; set; }

    [Range(0, 50)]
    public int? MinDoctoralTheses { get; set; }

    [Range(0, 50)]
    public int? MinDoctoralThesesInRank { get; set; }

    public string? RequiredLanguageLevel { get; set; }

    public string? Notes { get; set; }
}

public class CreateProcessRequest
{
    [Required(ErrorMessage = "El nombre del proceso es obligatorio.")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public List<RequirementConfigDto> Requirements { get; set; } = [];
}

public record TransitionDto(string FromPosition, string ToPosition, string FromLabel, string ToLabel);

public record ProcessSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    int ApplicationsCount,
    string CreatedByName,
    DateTime CreatedAt,
    TransitionDto? MyTransition,
    bool? HasApplied);

public record ProcessDetailDto(
    ProcessSummaryDto Summary,
    IReadOnlyList<RequirementConfigDto> Requirements);
