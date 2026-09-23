namespace PromocionBackend.Domain.Entities;

/// <summary>
/// Sesión de revisión: registro trazable de que un usuario CP/CA inició trabajo sobre
/// un proceso + comisión + facultad determinados. Toda decisión de CP/CA tomada durante
/// la sesión queda ligada a ella (además de a la comisión, que se sigue usando para el acta).
/// Guarda un snapshot de proceso y comisión para poder listar/buscar sin joins.
/// </summary>
public class ReviewSession
{
    public Guid Id { get; set; }
    public Guid ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Guid CommissionId { get; set; }
    public DateTime CommissionDate { get; set; }
    public bool CommissionIsPrincipal { get; set; }
    public string FacultyId { get; set; } = string.Empty;
    public string FacultyName { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Cuándo se cerró la sesión; nula mientras sigue activa/reanudable.</summary>
    public DateTime? ClosedAt { get; set; }

    public PromotionProcess Process { get; set; } = null!;
    public Commission Commission { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public ICollection<ApplicationReview> Reviews { get; set; } = [];
}
