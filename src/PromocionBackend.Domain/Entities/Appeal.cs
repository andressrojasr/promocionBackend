namespace PromocionBackend.Domain.Entities;

/// <summary>
/// Apelación presentada por el docente dentro del plazo de 3 días posteriores
/// al rechazo de la Comisión de Promoción.
/// </summary>
public class Appeal
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string Justification { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }

    public PromotionApplication Application { get; set; } = null!;
}
