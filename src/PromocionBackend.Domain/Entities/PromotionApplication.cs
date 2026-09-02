using PromocionBackend.Domain.Constants;
using PromocionBackend.Domain.Services;

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
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;
    public DateTime SubmittedAt { get; set; }

    /// <summary>Perfil del docente al momento de postular (mínimo necesario para auditoría).</summary>
    public string TeacherId { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string CurrentPosition { get; set; } = string.Empty;
    public decimal? ScorePct { get; set; }

    /// <summary>Momento del rechazo de la Comisión de Promoción; inicia el plazo de apelación.</summary>
    public DateTime? CpDecisionAt { get; set; }

    /// <summary>Momento en que la postulación alcanzó un estado final.</summary>
    public DateTime? DecidedAt { get; set; }

    /// <summary>Usuario que actualmente tiene bloqueada la postulación para revisión.</summary>
    public Guid? ReviewLockedBy { get; set; }

    /// <summary>Cuándo se creó el bloqueo de revisión.</summary>
    public DateTime? ReviewLockedAt { get; set; }

    /// <summary>Cuándo expira el bloqueo de revisión (30 minutos después de la apertura).</summary>
    public DateTime? ReviewLockExpiresAt { get; set; }

    public PromotionProcess Process { get; set; } = null!;
    public User Teacher { get; set; } = null!;
    public User? ReviewLocker { get; set; }
    public ICollection<ApplicationItem> Items { get; set; } = [];
    public ICollection<ApplicationReview> Reviews { get; set; } = [];
    public Appeal? Appeal { get; set; }

    /// <summary>Verifica si está bloqueada para el usuario actual.</summary>
    public bool IsReviewLockedBy(Guid userId, DateTime utcNow)
    {
        if (ReviewLockedBy != userId) return false;
        if (!ReviewLockExpiresAt.HasValue) return false;
        return utcNow <= ReviewLockExpiresAt.Value;
    }

    /// <summary>Verifica si está bloqueada por otro usuario.</summary>
    public bool IsReviewLockedByOther(Guid userId, DateTime utcNow)
    {
        if (ReviewLockedBy == userId) return false;
        if (ReviewLockedBy == null) return false;
        if (!ReviewLockExpiresAt.HasValue) return false;
        return utcNow <= ReviewLockExpiresAt.Value;
    }

    /// <summary>¿Está en un estado final?</summary>
    public bool IsFinal() => Status.IsFinal();

    /// <summary>¿Se puede apelar?</summary>
    public bool CanBeAppealed(DateTime utcNow) =>
        ApplicationStateMachine.CanAppeal(Status, CpDecisionAt, null, utcNow);

    /// <summary>¿Cuál es el estado efectivo considerando expiración?</summary>
    public ApplicationStatus GetEffectiveStatus(DateTime utcNow) =>
        ApplicationStateMachine.GetEffectiveStatus(Status, CpDecisionAt, null, utcNow);

    /// <summary>¿Es revisable por este rol?</summary>
    public bool IsReviewableBy(string role)
    {
        var stage = ApplicationStateMachine.StageForRole(role);
        if (stage == null) return false;

        var reviewableStatus = ApplicationStateMachine.ReviewableStatusFor(stage);
        return reviewableStatus == Status;
    }
}
