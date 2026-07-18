namespace PromocionBackend.Domain.Constants;

/// <summary>
/// Estados posibles de una postulación.
/// </summary>
public static class ApplicationStatuses
{
    /// <summary>Enviada por el docente; pendiente de revisión de Talento Humano.</summary>
    public const string Submitted = "submitted";

    /// <summary>Aprobada por Talento Humano; pendiente de revisión de la Comisión de Promoción.</summary>
    public const string ThApproved = "th_approved";

    /// <summary>Rechazada por Talento Humano (estado final).</summary>
    public const string ThRejected = "th_rejected";

    /// <summary>Rechazada por la Comisión de Promoción; el docente tiene 3 días para apelar.</summary>
    public const string CpRejected = "cp_rejected";

    /// <summary>Apelada por el docente; pendiente de la Comisión de Apelaciones.</summary>
    public const string Appealed = "appealed";

    /// <summary>Promoción aprobada (estado final).</summary>
    public const string Approved = "approved";

    /// <summary>Rechazada de forma definitiva (estado final).</summary>
    public const string Rejected = "rejected";

    public static readonly IReadOnlyList<string> All =
        [Submitted, ThApproved, ThRejected, CpRejected, Appealed, Approved, Rejected];

    public static bool IsFinal(string status) =>
        status is ThRejected or Approved or Rejected;
}
