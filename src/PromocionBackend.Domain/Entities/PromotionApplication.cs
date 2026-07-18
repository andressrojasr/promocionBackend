using PromocionBackend.Domain.Constants;

namespace PromocionBackend.Domain.Entities;

/// <summary>
/// Postulación de un docente a un proceso de promoción. Congela la hoja de vida
/// y el resultado de elegibilidad al momento de postular, de modo que las
/// revisiones evalúen exactamente lo que se presentó.
/// </summary>
public class PromotionApplication
{
    public Guid Id { get; set; }
    public Guid ProcessId { get; set; }
    public Guid TeacherUserId { get; set; }
    public string FromPosition { get; set; } = string.Empty;
    public string ToPosition { get; set; } = string.Empty;
    public string Status { get; set; } = ApplicationStatuses.Submitted;
    public DateTime SubmittedAt { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public string EligibilityJson { get; set; } = string.Empty;

    /// <summary>Momento del rechazo de la Comisión de Promoción; inicia el plazo de apelación.</summary>
    public DateTime? CpDecisionAt { get; set; }

    /// <summary>Momento en que la postulación alcanzó un estado final.</summary>
    public DateTime? DecidedAt { get; set; }

    public PromotionProcess Process { get; set; } = null!;
    public User Teacher { get; set; } = null!;
    public ICollection<ApplicationItem> Items { get; set; } = [];
    public ICollection<ApplicationReview> Reviews { get; set; } = [];
    public Appeal? Appeal { get; set; }
}
