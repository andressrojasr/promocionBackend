namespace PromocionBackend.Domain.Constants;

/// <summary>
/// Estados de una postulación, fuertemente tipados como enum.
/// Mapea 1:1 con los valores string para compatibilidad de BD.
/// </summary>
public enum ApplicationStatus
{
    /// <summary>Enviada por el docente; pendiente TH</summary>
    Submitted = 0,

    /// <summary>Aprobada por TH; pendiente CP</summary>
    ThApproved = 1,

    /// <summary>Rechazada por TH (final)</summary>
    ThRejected = 2,

    /// <summary>Rechazada por CP; apelable en 3 días</summary>
    CpRejected = 3,

    /// <summary>Apelada; pendiente CA</summary>
    Appealed = 4,

    /// <summary>Promoción aprobada (final)</summary>
    Approved = 5,

    /// <summary>Rechazada definitivamente (final)</summary>
    Rejected = 6
}
