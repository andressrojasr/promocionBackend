using PromocionBackend.Domain.Constants;

namespace PromocionBackend.Domain.Services;

/// <summary>
/// Máquina de estados de la postulación:
/// submitted → (TH) → th_approved | th_rejected(final)
/// th_approved → (CP) → approved(final) | cp_rejected
/// cp_rejected → apelación en ≤ 3 días → appealed → (CA) → approved(final) | rejected(final)
/// cp_rejected → sin apelación en 3 días → rejected(final)
/// </summary>
public static class ApplicationStateMachine
{
    public const int AppealWindowDays = 3;

    /// <summary>
    /// Devuelve el estado que puede revisar cada etapa.
    /// </summary>
    public static ApplicationStatus? ReviewableStatusFor(string stage) => stage switch
    {
        ReviewStages.Th => ApplicationStatus.Submitted,
        ReviewStages.Cp => ApplicationStatus.ThApproved,
        ReviewStages.Ca => ApplicationStatus.Appealed,
        _ => null
    };

    /// <summary>
    /// Etapa de revisión que corresponde a un rol de la aplicación, o null si el rol no revisa.
    /// </summary>
    public static string? StageForRole(string? role) => role?.ToLowerInvariant() switch
    {
        Roles.Th => ReviewStages.Th,
        Roles.Cp => ReviewStages.Cp,
        Roles.Ca => ReviewStages.Ca,
        _ => null
    };

    /// <summary>
    /// Calcula el siguiente estado tras una decisión de revisión, o null si la
    /// transición no es válida para el estado actual.
    /// </summary>
    public static ApplicationStatus? GetNextStatus(ApplicationStatus currentStatus, string stage, bool approved)
    {
        if (ReviewableStatusFor(stage) != currentStatus)
        {
            return null;
        }

        return (stage, approved) switch
        {
            (ReviewStages.Th, true) => ApplicationStatus.ThApproved,
            (ReviewStages.Th, false) => ApplicationStatus.ThRejected,
            (ReviewStages.Cp, true) => ApplicationStatus.Approved,
            (ReviewStages.Cp, false) => ApplicationStatus.CpRejected,
            (ReviewStages.Ca, true) => ApplicationStatus.Approved,
            (ReviewStages.Ca, false) => ApplicationStatus.Rejected,
            _ => null
        };
    }

    /// <summary>
    /// Estado efectivo considerando la expiración del plazo de apelación:
    /// un rechazo de TH o CP sin apelación dentro del plazo se convierte en rechazo definitivo.
    /// </summary>
    public static ApplicationStatus GetEffectiveStatus(ApplicationStatus status, DateTime? cpDecisionAt, DateTime? thDecisionAt, DateTime utcNow)
    {
        // ThRejected es final sin apelación
        if (status == ApplicationStatus.ThRejected)
        {
            return ApplicationStatus.ThRejected;
        }

        // CpRejected es final después de 3 días sin apelar
        if (status == ApplicationStatus.CpRejected && IsAppealWindowExpired(cpDecisionAt, utcNow))
        {
            return ApplicationStatus.Rejected;
        }

        return status;
    }

    public static DateTime? GetAppealDeadline(ApplicationStatus status, DateTime? cpDecisionAt, DateTime? thDecisionAt) =>
        status == ApplicationStatus.CpRejected && cpDecisionAt is { } cpDecided
            ? cpDecided.AddDays(AppealWindowDays)
            : status == ApplicationStatus.ThRejected && thDecisionAt is { } thDecided
                ? thDecided.AddDays(AppealWindowDays)
                : null;

    public static bool CanAppeal(ApplicationStatus status, DateTime? cpDecisionAt, DateTime? thDecisionAt, DateTime utcNow)
    {
        if (status == ApplicationStatus.CpRejected)
            return !IsAppealWindowExpired(cpDecisionAt, utcNow);
        if (status == ApplicationStatus.ThRejected)
            return !IsAppealWindowExpired(thDecisionAt, utcNow);
        return false;
    }

    /// <summary>Valida si una transición es válida según las reglas del negocio.</summary>
    public static bool IsValidTransition(ApplicationStatus from, ApplicationStatus to, string reviewerRole)
    {
        var stage = StageForRole(reviewerRole);
        if (stage == null) return false;

        var nextStatus = GetNextStatus(from, stage, to.IsFinal());
        return nextStatus == to;
    }

    private static bool IsAppealWindowExpired(DateTime? cpDecisionAt, DateTime utcNow) =>
        cpDecisionAt is not { } decidedAt || utcNow > decidedAt.AddDays(AppealWindowDays);
}
