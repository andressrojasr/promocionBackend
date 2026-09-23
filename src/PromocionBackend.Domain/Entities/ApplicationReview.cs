namespace PromocionBackend.Domain.Entities;

/// <summary>
/// Registro de auditoría de cada decisión de revisión: quién revisó, en qué etapa,
/// qué decidió, con qué retroalimentación y cuándo.
/// </summary>
public class ApplicationReview
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string Stage { get; set; } = string.Empty;
    public Guid ReviewerUserId { get; set; }
    public string ReviewerRole { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
    public string? Feedback { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Comisión (CP o CA) que tomó esta decisión; nulo para revisiones de Talento Humano.</summary>
    public Guid? CommissionId { get; set; }

    /// <summary>Sesión de revisión (proceso+comisión+facultad) bajo la cual se tomó la decisión; nula para TH.</summary>
    public Guid? ReviewSessionId { get; set; }

    public PromotionApplication Application { get; set; } = null!;
    public User Reviewer { get; set; } = null!;
    public Commission? Commission { get; set; }
    public ReviewSession? ReviewSession { get; set; }
}
