using PromocionBackend.Application.DTOs.Processes;
using PromocionBackend.Domain.Constants;
using PromocionBackend.Domain.Services;

namespace PromocionBackend.Application.Defaults;

/// <summary>
/// Valores por defecto de los requisitos de promoción según el reglamento de
/// escalafón. Pre-llenan el formulario de creación de procesos; la Comisión de
/// Promoción puede ajustarlos por proceso.
/// </summary>
public static class RequirementDefaults
{
    private const string AuthorityExceptionNote =
        "Excepción de horas de capacitación si ha sido autoridad por más de dos años en la Universidad o Escuela Politécnica.";

    private const string LanguageExceptionNote =
        "Excepción del requisito de idioma si obtuvo un título de educación superior en un país con lengua diferente al castellano.";

    public static IReadOnlyList<RequirementConfigDto> All =>
    [
        new RequirementConfigDto
        {
            FromPosition = PositionLadder.Auxiliar1,
            ToPosition = PositionLadder.Auxiliar2,
            MinYearsInPosition = 4,
            MinPublications = 1,
            MinPublicationsInOtherLanguage = 0,
            MinEvaluationScorePct = 75,
            MinTrainingHours = 96,
            TrainingWindowYears = 3,
            MinPedagogicalTrainingPct = 25,
            MinGivenTrainingHours = null,
            MinProjectMonths = null,
            ProjectRoleScope = ProjectRoleScopes.Any,
            ApplyRoleMultipliers = false,
            MinInternationalProjects = null,
            MinDoctoralTheses = null,
            MinDoctoralThesesInRank = null,
            RequiredLanguageLevel = "B1",
            Notes = $"{AuthorityExceptionNote} {LanguageExceptionNote}"
        },
        new RequirementConfigDto
        {
            FromPosition = PositionLadder.Auxiliar2,
            ToPosition = PositionLadder.Agregado1,
            MinYearsInPosition = 4,
            MinPublications = 2,
            MinPublicationsInOtherLanguage = 0,
            MinEvaluationScorePct = 75,
            MinTrainingHours = 96,
            TrainingWindowYears = 3,
            MinPedagogicalTrainingPct = 25,
            MinGivenTrainingHours = null,
            MinProjectMonths = 12,
            ProjectRoleScope = ProjectRoleScopes.Any,
            ApplyRoleMultipliers = false,
            MinInternationalProjects = null,
            MinDoctoralTheses = null,
            MinDoctoralThesesInRank = null,
            RequiredLanguageLevel = "B1",
            Notes = $"{AuthorityExceptionNote} {LanguageExceptionNote}"
        },
        new RequirementConfigDto
        {
            FromPosition = PositionLadder.Agregado1,
            ToPosition = PositionLadder.Agregado2,
            MinYearsInPosition = 4,
            MinPublications = 3,
            MinPublicationsInOtherLanguage = 0,
            MinEvaluationScorePct = 75,
            MinTrainingHours = 128,
            TrainingWindowYears = 3,
            MinPedagogicalTrainingPct = 25,
            MinGivenTrainingHours = null,
            MinProjectMonths = 24,
            ProjectRoleScope = ProjectRoleScopes.Any,
            ApplyRoleMultipliers = true,
            MinInternationalProjects = null,
            MinDoctoralTheses = null,
            MinDoctoralThesesInRank = null,
            RequiredLanguageLevel = "B1",
            Notes = $"Los 24 meses pueden ser consecutivos o no. {AuthorityExceptionNote} {LanguageExceptionNote}"
        },
        new RequirementConfigDto
        {
            FromPosition = PositionLadder.Agregado2,
            ToPosition = PositionLadder.Agregado3,
            MinYearsInPosition = 4,
            MinPublications = 5,
            MinPublicationsInOtherLanguage = 0,
            MinEvaluationScorePct = 75,
            MinTrainingHours = 160,
            TrainingWindowYears = 3,
            MinPedagogicalTrainingPct = 25,
            MinGivenTrainingHours = null,
            MinProjectMonths = 24,
            ProjectRoleScope = ProjectRoleScopes.Any,
            ApplyRoleMultipliers = true,
            MinInternationalProjects = null,
            MinDoctoralTheses = null,
            MinDoctoralThesesInRank = null,
            RequiredLanguageLevel = "B1",
            Notes = $"Los 24 meses pueden ser consecutivos o no. {AuthorityExceptionNote} {LanguageExceptionNote}"
        },
        new RequirementConfigDto
        {
            FromPosition = PositionLadder.Principal1,
            ToPosition = PositionLadder.Principal2,
            MinYearsInPosition = 3,
            MinPublications = 8,
            MinPublicationsInOtherLanguage = 1,
            MinEvaluationScorePct = 75,
            MinTrainingHours = 160,
            TrainingWindowYears = 3,
            MinPedagogicalTrainingPct = 25,
            MinGivenTrainingHours = 40,
            MinProjectMonths = 24,
            ProjectRoleScope = ProjectRoleScopes.Direction,
            ApplyRoleMultipliers = true,
            MinInternationalProjects = 1,
            MinDoctoralTheses = 2,
            MinDoctoralThesesInRank = 1,
            RequiredLanguageLevel = "B1",
            Notes = "Al menos un proyecto debe implicar investigadores, instituciones o redes de investigación extranjeros. " +
                    $"{AuthorityExceptionNote} {LanguageExceptionNote}"
        },
        new RequirementConfigDto
        {
            FromPosition = PositionLadder.Principal2,
            ToPosition = PositionLadder.Principal3,
            MinYearsInPosition = 3,
            MinPublications = 12,
            MinPublicationsInOtherLanguage = 2,
            MinEvaluationScorePct = 75,
            MinTrainingHours = 256,
            TrainingWindowYears = 3,
            MinPedagogicalTrainingPct = 25,
            MinGivenTrainingHours = 80,
            MinProjectMonths = 36,
            ProjectRoleScope = ProjectRoleScopes.Direction,
            ApplyRoleMultipliers = true,
            MinInternationalProjects = 2,
            MinDoctoralTheses = 3,
            MinDoctoralThesesInRank = 1,
            RequiredLanguageLevel = null,
            Notes = "Al menos dos proyectos deben implicar investigadores, instituciones o redes de investigación extranjeros. " +
                    AuthorityExceptionNote
        }
    ];
}
