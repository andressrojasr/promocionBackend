namespace PromocionBackend.Domain.Entities;

/// <summary>
/// Ítem de evidencia seleccionado por el docente al postular. Referencia por
/// identificador externo a un elemento de su hoja de vida (publicación,
/// capacitación, proyecto, tesis, idioma o experiencia).
/// </summary>
public class ApplicationItem
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public string ExternalItemId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? DocumentUrl { get; set; }

    public PromotionApplication Application { get; set; } = null!;
}
