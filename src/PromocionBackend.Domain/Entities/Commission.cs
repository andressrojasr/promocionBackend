namespace PromocionBackend.Domain.Entities;

/// <summary>
/// Comisión (de Promoción o de Apelaciones) que resuelve las postulaciones de un
/// proceso en una fecha determinada. Cada proceso tiene una comisión "principal" por
/// tipo, y puede tener comisiones adicionales para días concretos en los que algún
/// integrante fue reemplazado por su delegado.
/// </summary>
public class Commission
{
    public Guid Id { get; set; }
    public Guid ProcessId { get; set; }
    public string Type { get; set; } = string.Empty;
    public bool IsPrincipal { get; set; }
    public DateTime Date { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public PromotionProcess Process { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public ICollection<CommissionMember> Members { get; set; } = [];
}
