namespace PromocionBackend.Domain.Entities;

/// <summary>
/// Proceso de promoción creado por la Comisión de Promoción. Define la ventana de
/// postulación y la configuración de requisitos por transición de escalafón.
/// </summary>
public class PromotionProcess
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User CreatedBy { get; set; } = null!;
    public ICollection<ProcessRequirement> Requirements { get; set; } = [];
    public ICollection<PromotionApplication> Applications { get; set; } = [];

    public bool IsOpenAt(DateTime utcNow) => utcNow >= StartDate && utcNow <= EndDate;

    public string GetStatusAt(DateTime utcNow) =>
        utcNow < StartDate ? "scheduled" : utcNow > EndDate ? "closed" : "open";
}
