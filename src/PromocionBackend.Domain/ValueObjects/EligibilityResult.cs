namespace PromocionBackend.Domain.ValueObjects;

/// <summary>
/// Resultado de evaluar la hoja de vida de un docente contra los requisitos
/// configurados para su transición de escalafón.
/// </summary>
public sealed class EligibilityResult
{
    public string FromPosition { get; init; } = string.Empty;
    public string ToPosition { get; init; } = string.Empty;
    public bool IsEligible { get; init; }
    public IReadOnlyList<RequirementEvaluation> Requirements { get; init; } = [];
}

/// <summary>
/// Evaluación de un requisito individual: valor exigido, valor alcanzado y si se cumple.
/// </summary>
public sealed record RequirementEvaluation(
    string Code,
    string Label,
    string Required,
    string Actual,
    bool Met,
    string Detail);
