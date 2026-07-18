namespace PromocionBackend.Domain.ValueObjects;

/// <summary>
/// Modelo de dominio de la hoja de vida del docente, independiente del contrato
/// del sistema de RRHH. Es la entrada del motor de elegibilidad.
/// </summary>
public sealed class TeacherProfile
{
    public string TeacherId { get; init; } = string.Empty;
    public string Identification { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string CurrentPosition { get; init; } = string.Empty;
    public DateOnly CurrentPositionStartDate { get; init; }
    public decimal? ScorePercentage { get; init; }

    public IReadOnlyList<PublicationRecord> Publications { get; init; } = [];
    public IReadOnlyList<TrainingRecord> ReceivedTrainings { get; init; } = [];
    public IReadOnlyList<TrainingRecord> GivenTrainings { get; init; } = [];
    public IReadOnlyList<ProjectRecord> ResearchProjects { get; init; } = [];
    public IReadOnlyList<ThesisRecord> DoctoralTheses { get; init; } = [];
    public IReadOnlyList<LanguageRecord> Languages { get; init; } = [];
}

public sealed record PublicationRecord(
    string Id,
    string Name,
    DateOnly? PublicationDate,
    string Language,
    string Status);

public sealed record TrainingRecord(
    string Id,
    string Name,
    string Category,
    DateOnly? EndDate,
    int Hours);

public sealed record ProjectRecord(
    string Id,
    string Name,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string Role,
    string Country);

public sealed record ThesisRecord(
    string Id,
    string Title,
    DateOnly? ApprovalDate,
    string Role);

public sealed record LanguageRecord(
    string Id,
    string Language,
    string Level,
    DateOnly? ExpirationDate);
