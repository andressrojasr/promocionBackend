namespace PromocionBackend.Domain.Entities;

/// <summary>
/// Última copia de la hoja de vida del docente obtenida del sistema de RRHH.
/// Se actualiza en cada inicio de sesión y alimenta el cálculo de elegibilidad.
/// </summary>
public class TeacherSnapshot
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CurrentPosition { get; set; } = string.Empty;
    public DateOnly CurrentPositionStartDate { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }

    public User User { get; set; } = null!;
}
