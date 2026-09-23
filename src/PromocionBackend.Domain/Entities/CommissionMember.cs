namespace PromocionBackend.Domain.Entities;

/// <summary>
/// Integrante de una comisión en un cargo determinado. El docente/autoridad se
/// congela por nombre e identificación al momento de integrar la comisión, igual
/// que el resto de snapshots del sistema.
/// </summary>
public class CommissionMember
{
    public Guid Id { get; set; }
    public Guid CommissionId { get; set; }
    public int OrderIndex { get; set; }
    public string CargoLabel { get; set; } = string.Empty;
    public string TeacherIdentification { get; set; } = string.Empty;
    public string TeacherFullName { get; set; } = string.Empty;
    public string? TeacherExternalId { get; set; }

    public Commission Commission { get; set; } = null!;
}
