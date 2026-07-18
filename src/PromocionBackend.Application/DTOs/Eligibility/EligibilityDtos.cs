namespace PromocionBackend.Application.DTOs.Eligibility;

public record RequirementEvaluationDto(
    string Code,
    string Label,
    string Required,
    string Actual,
    bool Met,
    string Detail);

public record EligibilityDto(
    string FromPosition,
    string ToPosition,
    string FromLabel,
    string ToLabel,
    bool IsEligible,
    IReadOnlyList<RequirementEvaluationDto> Requirements,
    string? Notes);
