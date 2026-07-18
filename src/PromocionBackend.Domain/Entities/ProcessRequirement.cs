using PromocionBackend.Domain.Constants;

namespace PromocionBackend.Domain.Entities;

/// <summary>
/// Configuración de requisitos para una transición de escalafón dentro de un proceso.
/// Cada proceso tiene una fila por transición; los valores nulos indican que el
/// requisito no aplica para esa transición.
/// </summary>
public class ProcessRequirement
{
    public Guid Id { get; set; }
    public Guid ProcessId { get; set; }
    public string FromPosition { get; set; } = string.Empty;
    public string ToPosition { get; set; } = string.Empty;

    /// <summary>Años mínimos de experiencia en el grado actual.</summary>
    public int MinYearsInPosition { get; set; }

    /// <summary>Publicaciones u obras de relevancia realizadas durante el grado actual.</summary>
    public int MinPublications { get; set; }

    /// <summary>De las publicaciones, cuántas deben estar en un idioma distinto a la lengua materna.</summary>
    public int MinPublicationsInOtherLanguage { get; set; }

    /// <summary>Puntaje mínimo de la evaluación integral de desempeño (porcentaje).</summary>
    public decimal MinEvaluationScorePct { get; set; }

    /// <summary>Horas mínimas de capacitación recibida dentro de la ventana de años configurada.</summary>
    public int MinTrainingHours { get; set; }

    /// <summary>Ventana (en años hacia atrás) en la que cuentan las capacitaciones recibidas.</summary>
    public int TrainingWindowYears { get; set; } = 3;

    /// <summary>Porcentaje de las horas mínimas que debe ser de actualización pedagógica.</summary>
    public decimal? MinPedagogicalTrainingPct { get; set; }

    /// <summary>Horas mínimas de capacitación impartida.</summary>
    public int? MinGivenTrainingHours { get; set; }

    /// <summary>Meses mínimos en proyectos de investigación/vinculación durante el grado actual.</summary>
    public int? MinProjectMonths { get; set; }

    /// <summary>Roles de proyecto que cuentan: cualquier participación o solo dirección/codirección.</summary>
    public string ProjectRoleScope { get; set; } = ProjectRoleScopes.Any;

    /// <summary>Si aplica el multiplicador reglamentario (x2 coordinador principal, x1.5 subrogante).</summary>
    public bool ApplyRoleMultipliers { get; set; }

    /// <summary>Proyectos mínimos que impliquen investigadores, instituciones o redes extranjeras.</summary>
    public int? MinInternationalProjects { get; set; }

    /// <summary>Tesis de doctorado mínimas dirigidas o codirigidas.</summary>
    public int? MinDoctoralTheses { get; set; }

    /// <summary>De las tesis, cuántas deben haberse dirigido durante el grado actual.</summary>
    public int? MinDoctoralThesesInRank { get; set; }

    /// <summary>Nivel mínimo de idioma distinto al castellano (MCER). Nulo = sin requisito de idioma.</summary>
    public string? RequiredLanguageLevel { get; set; }

    /// <summary>Excepciones reglamentarias u observaciones que se muestran en el dashboard.</summary>
    public string? Notes { get; set; }

    public PromotionProcess Process { get; set; } = null!;
}
