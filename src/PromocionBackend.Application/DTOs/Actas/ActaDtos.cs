namespace PromocionBackend.Application.DTOs.Actas;

/// <summary>Categoría (transición) con decisiones de CP dentro de una sesión; una acta se genera por categoría.</summary>
public record ActaCategoryDto(
    string FromPosition,
    string ToPosition,
    string FromLabel,
    string ToLabel,
    int ApprovedCount,
    int RejectedCount,
    /// <summary>Rechazos dentro del plazo de apelación o con apelación en trámite; mientras haya, el acta es provisional.</summary>
    int PendingAppeals);

public record ActaMemberRow(string CargoLabel, string FullName);

public record ActaApprovedRow(
    string Identification,
    string FullName,
    string ResultingCategoryLabel,
    string? Observation);

public record ActaRejectedRow(
    string Identification,
    string FullName,
    string? Observation);

/// <summary>
/// Datos ya resueltos para maquetar el acta de promoción en PDF; el generador de PDF
/// solo se encarga de la disposición visual, no de ninguna consulta a la base de datos.
/// </summary>
public record ActaData(
    int Day,
    string MonthName,
    int Year,
    string Time,
    string FacultyName,
    string OriginCategoryLabel,
    string DestinationCategoryLabel,
    IReadOnlyList<ActaMemberRow> Members,
    IReadOnlyList<string> ReviewedTeacherNames,
    IReadOnlyList<ActaApprovedRow> Approved,
    IReadOnlyList<ActaRejectedRow> Rejected,
    int PendingCount = 0)
{
    /// <summary>Provisional mientras haya rechazos que aún pueden cambiar por apelación.</summary>
    public bool IsProvisional => PendingCount > 0;
}
