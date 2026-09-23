namespace PromocionBackend.Application.DTOs.Actas;

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
    string? OriginCategoryLabel,
    string? DestinationCategoryLabel,
    IReadOnlyList<ActaMemberRow> Members,
    IReadOnlyList<string> ReviewedTeacherNames,
    IReadOnlyList<ActaApprovedRow> Approved,
    IReadOnlyList<ActaRejectedRow> Rejected);
