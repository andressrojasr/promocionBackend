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
    public static string? ReviewableStatusFor(string stage) => stage switch
    {
        ReviewStages.Th => ApplicationStatuses.Submitted,
        ReviewStages.Cp => ApplicationStatuses.ThApproved,
        ReviewStages.Ca => ApplicationStatuses.Appealed,
        _ => null
    };

    /// <summary>
    /// Etapa de revisión que corresponde a un rol de la aplicación, o null si el rol no revisa.
    /// </summary>
    public static string? StageForRole(string role) => role switch
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
    public static string? GetNextStatus(string currentStatus, string stage, bool approved)
    {
        if (ReviewableStatusFor(stage) != currentStatus)
        {
            return null;
        }

        return (stage, approved) switch
        {
            (ReviewStages.Th, true) => ApplicationStatuses.ThApproved,
            (ReviewStages.Th, false) => ApplicationStatuses.ThRejected,
            (ReviewStages.Cp, true) => ApplicationStatuses.Approved,
            (ReviewStages.Cp, false) => ApplicationStatuses.CpRejected,
            (ReviewStages.Ca, true) => ApplicationStatuses.Approved,
            (ReviewStages.Ca, false) => ApplicationStatuses.Rejected,
            _ => null
        };
    }

    /// <summary>
    /// Estado efectivo considerando la expiración del plazo de apelación:
    /// un rechazo de CP sin apelación dentro del plazo se convierte en rechazo definitivo.
    /// </summary>
    public static string GetEffectiveStatus(string status, DateTime? cpDecisionAt, DateTime utcNow)
    {
        if (status == ApplicationStatuses.CpRejected && IsAppealWindowExpired(cpDecisionAt, utcNow))
        {
            return ApplicationStatuses.Rejected;
        }

        return status;
    }

    public static DateTime? GetAppealDeadline(string status, DateTime? cpDecisionAt) =>
        status == ApplicationStatuses.CpRejected && cpDecisionAt is { } decidedAt
            ? decidedAt.AddDays(AppealWindowDays)
            : null;

    public static bool CanAppeal(string status, DateTime? cpDecisionAt, DateTime utcNow) =>
        status == ApplicationStatuses.CpRejected && !IsAppealWindowExpired(cpDecisionAt, utcNow);

    private static bool IsAppealWindowExpired(DateTime? cpDecisionAt, DateTime utcNow) =>
        cpDecisionAt is not { } decidedAt || utcNow > decidedAt.AddDays(AppealWindowDays);
}
