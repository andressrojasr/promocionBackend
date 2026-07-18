using System.Globalization;
using PromocionBackend.Application.Abstractions.External;
using PromocionBackend.Domain.ValueObjects;

namespace PromocionBackend.Application.Mapping;

/// <summary>
/// Convierte el contrato del sistema de RRHH al modelo de dominio que consume
/// el motor de elegibilidad, parseando las fechas "yyyy-MM-dd".
/// </summary>
public static class TeacherProfileMapper
{
    public static TeacherProfile Map(HrTeacherDetails details) => new()
    {
        TeacherId = details.TeacherId,
        Identification = details.Identification,
        FullName = details.FullName,
        CurrentPosition = details.CurrentPosition,
        CurrentPositionStartDate = ParseDate(details.CurrentPositionStartDate) ?? DateOnly.MinValue,
        ScorePercentage = details.Score?.Percentage,
        Publications = [.. details.Publications.Select(p =>
            new PublicationRecord(p.Id, p.Name, ParseDate(p.PublicationDate), p.Language, p.Status))],
        ReceivedTrainings = [.. details.ReceivedTrainings.Select(t =>
            new TrainingRecord(t.Id, t.Name, t.TrainingCategory, ParseDate(t.EndDate), t.Hours))],
        GivenTrainings = [.. details.GivenTrainings.Select(t =>
            new TrainingRecord(t.Id, t.Name, t.TrainingCategory, ParseDate(t.EndDate), t.Hours))],
        ResearchProjects = [.. details.ResearchProjects.Select(p =>
            new ProjectRecord(p.Id, p.Name, ParseDate(p.StartDate), ParseDate(p.EndDate), p.Role, p.Country))],
        DoctoralTheses = [.. details.DoctoralTheses.Select(t =>
            new ThesisRecord(t.Id, t.Title, ParseDate(t.ApprovalDate), t.Role))],
        Languages = [.. details.Languages.Select(l =>
            new LanguageRecord(l.Id, l.Language, l.Level, ParseDate(l.ExpirationDate)))]
    };

    public static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
}
