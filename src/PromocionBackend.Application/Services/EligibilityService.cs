using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Abstractions.External;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.Eligibility;
using PromocionBackend.Application.Mapping;
using PromocionBackend.Domain.Entities;
using PromocionBackend.Domain.Services;
using PromocionBackend.Domain.ValueObjects;

namespace PromocionBackend.Application.Services;

/// <summary>
/// Evalúa la elegibilidad de un docente frente a un proceso: obtiene datos frescos,
/// determina su transición en el escalafón y ejecuta el motor de elegibilidad
/// con la configuración de requisitos del proceso.
/// </summary>
public class EligibilityService(IAppDbContext db, IHrApiClient hrApi)
{
    public async Task<EligibilityDto> EvaluateForProcessAsync(Guid processId, Guid teacherUserId, string externalAccessToken, CancellationToken cancellationToken = default)
    {
        var (_, eligibility) = await EvaluateInternalAsync(processId, teacherUserId, externalAccessToken, cancellationToken);
        return eligibility;
    }

    /// <summary>
    /// Evaluación completa usada al postular: obtiene datos frescos del servicio HR.
    /// </summary>
    internal async Task<(HrTeacherDetails Details, EligibilityDto Eligibility)> EvaluateInternalAsync(
        Guid processId, Guid teacherUserId, string externalAccessToken, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == teacherUserId, cancellationToken)
            ?? throw AppException.NotFound("Usuario no encontrado.");

        var identification = user.Identification
            ?? throw AppException.Unauthorized("El usuario no tiene identificación registrada.");

        // Obtener datos frescos de RRHH
        var details = await hrApi.GetTeacherDetailsAsync(identification, externalAccessToken, cancellationToken);

        var nextPosition = PositionLadder.GetNextPosition(details.CurrentPosition)
            ?? throw AppException.Conflict(
                $"No existe una transición de promoción disponible desde {PositionLadder.Label(details.CurrentPosition)}. " +
                "Por reglamento, no es posible postular desde Titular Agregado 3 hacia Titular Principal 1, y Titular Principal 3 es el grado máximo.");

        var requirement = await db.ProcessRequirements
            .FirstOrDefaultAsync(r => r.ProcessId == processId && r.FromPosition == details.CurrentPosition, cancellationToken)
            ?? throw AppException.NotFound(
                $"El proceso no tiene configurados requisitos para la transición desde {PositionLadder.Label(details.CurrentPosition)}.");

        var profile = TeacherProfileMapper.Map(details);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = EligibilityEngine.Evaluate(profile, requirement, today);

        return (details, ToDto(result, requirement.Notes, requirement));
    }

    internal static EligibilityDto ToDto(EligibilityResult result, string? notes, ProcessRequirement? requirement = null) => new(
        result.FromPosition,
        result.ToPosition,
        PositionLadder.Label(result.FromPosition),
        PositionLadder.Label(result.ToPosition),
        result.IsEligible,
        [.. result.Requirements.Select(r => new RequirementEvaluationDto(
            r.Code,
            r.Label,
            r.Required,
            r.Actual,
            r.Met,
            r.Detail,
            ExtractNumericValue(r.Required)))],
        notes,
        requirement is not null ? new RequirementConfigForApplicationDto(
            requirement.MinYearsInPosition,
            requirement.MinPublications,
            requirement.MinPublicationsInOtherLanguage,
            requirement.MinEvaluationScorePct,
            requirement.MinTrainingHours,
            requirement.TrainingWindowYears,
            requirement.MinPedagogicalTrainingPct,
            requirement.MinGivenTrainingHours,
            requirement.MinProjectMonths,
            requirement.ProjectRoleScope,
            requirement.ApplyRoleMultipliers,
            requirement.MinInternationalProjects,
            requirement.MinDoctoralTheses,
            requirement.MinDoctoralThesesInRank,
            requirement.RequiredLanguageLevel) : null);

    private static int? ExtractNumericValue(string required)
    {
        // Extrae el número del texto, ej: "3 publicaciones" → 3, "30 horas" → 30
        var parts = required.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0 && int.TryParse(parts[0], out var value))
        {
            return value;
        }
        return null;
    }
}
