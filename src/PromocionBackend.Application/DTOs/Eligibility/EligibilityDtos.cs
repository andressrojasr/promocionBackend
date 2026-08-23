namespace PromocionBackend.Application.DTOs.Eligibility;

public record RequirementEvaluationDto(
    string Code,
    string Label,
    string Required,
    string Actual,
    bool Met,
    string Detail,
    int? RequiredNumeric = null);

public record RequirementConfigForApplicationDto(
    int MinYearsInPosition,
    int MinPublications,
    int MinPublicationsInOtherLanguage,
    decimal MinEvaluationScorePct,
    int MinTrainingHours,
    int TrainingWindowYears,
    decimal? MinPedagogicalTrainingPct,
    int? MinGivenTrainingHours,
    int? MinProjectMonths,
    string ProjectRoleScope,
    bool ApplyRoleMultipliers,
    int? MinInternationalProjects,
    int? MinDoctoralTheses,
    int? MinDoctoralThesesInRank,
    string? RequiredLanguageLevel);

public record EligibilityDto(
    string FromPosition,
    string ToPosition,
    string FromLabel,
    string ToLabel,
    bool IsEligible,
    IReadOnlyList<RequirementEvaluationDto> Requirements,
    string? Notes,
    RequirementConfigForApplicationDto? RequirementConfig = null);
