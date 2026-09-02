namespace PromocionBackend.Domain.Constants;

/// <summary>
/// Métodos helper para ApplicationStatus. Centraliza toda la lógica de
/// comparación y convierte entre enum y strings de BD.
/// </summary>
public static class ApplicationStatusExtensions
{
    private static readonly Dictionary<ApplicationStatus, string> StatusToStringMap = new()
    {
        { ApplicationStatus.Submitted, "submitted" },
        { ApplicationStatus.ThApproved, "th_approved" },
        { ApplicationStatus.ThRejected, "th_rejected" },
        { ApplicationStatus.CpRejected, "cp_rejected" },
        { ApplicationStatus.Appealed, "appealed" },
        { ApplicationStatus.Approved, "approved" },
        { ApplicationStatus.Rejected, "rejected" }
    };

    private static readonly Dictionary<string, ApplicationStatus> StringToStatusMap =
        StatusToStringMap.ToDictionary(x => x.Value, x => x.Key);

    /// <summary>Convierte enum a string para serialización (formato BD).</summary>
    public static string ToStringValue(this ApplicationStatus status) =>
        StatusToStringMap.TryGetValue(status, out var value)
            ? value
            : throw new ArgumentException($"Estado inválido: {status}");

    /// <summary>Convierte string de BD a enum.</summary>
    public static ApplicationStatus FromStringValue(string value) =>
        StringToStatusMap.TryGetValue(value, out var status)
            ? status
            : throw new ArgumentException($"Estado desconocido: {value}");

    /// <summary>Intenta convertir string sin lanzar excepción.</summary>
    public static bool TryFromStringValue(string? value, out ApplicationStatus status)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            status = default;
            return false;
        }

        return StringToStatusMap.TryGetValue(value, out status);
    }

    /// <summary>Estados finales (decisión tomada, no hay más transiciones).</summary>
    public static bool IsFinal(this ApplicationStatus status) =>
        status is ApplicationStatus.ThRejected
            or ApplicationStatus.Approved
            or ApplicationStatus.Rejected;

    /// <summary>Estados que permiten apelar (solo CpRejected).</summary>
    public static bool IsAppealable(this ApplicationStatus status) =>
        status == ApplicationStatus.CpRejected;

    /// <summary>Estados en progreso (ni final ni completado).</summary>
    public static bool IsInProgress(this ApplicationStatus status) =>
        status is ApplicationStatus.Submitted
            or ApplicationStatus.ThApproved
            or ApplicationStatus.CpRejected
            or ApplicationStatus.Appealed;

    /// <summary>Estados revisables por TH.</summary>
    public static bool IsReviewableByTh(this ApplicationStatus status) =>
        status == ApplicationStatus.Submitted;

    /// <summary>Estados revisables por CP.</summary>
    public static bool IsReviewableByCp(this ApplicationStatus status) =>
        status == ApplicationStatus.ThApproved;

    /// <summary>Estados revisables por CA.</summary>
    public static bool IsReviewableByCa(this ApplicationStatus status) =>
        status == ApplicationStatus.Appealed;

    /// <summary>Obtiene todos los valores posibles de enum como lista de strings.</summary>
    public static IReadOnlyList<string> GetAllValues() =>
        Enum.GetValues<ApplicationStatus>()
            .Select(s => s.ToStringValue())
            .ToList()
            .AsReadOnly();

    /// <summary>Valida si un string representa un estado válido.</summary>
    public static bool IsValidStatusValue(string? value) =>
        TryFromStringValue(value, out _);
}
