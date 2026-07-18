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
/// Evalúa la elegibilidad de un docente frente a un proceso: carga su snapshot,
/// determina su transición en el escalafón y ejecuta el motor de elegibilidad
/// con la configuración de requisitos del proceso.
/// </summary>
public class EligibilityService(IAppDbContext db)
{
    public async Task<EligibilityDto> EvaluateForProcessAsync(Guid processId, Guid teacherUserId, CancellationToken cancellationToken = default)
    {
        var (_, _, eligibility) = await EvaluateInternalAsync(processId, teacherUserId, cancellationToken);
        return eligibility;
    }

    /// <summary>
    /// Evaluación completa usada al postular: además del resultado devuelve el snapshot
    /// y la hoja de vida para congelarlos en la postulación.
    /// </summary>
    internal async Task<(TeacherSnapshot Snapshot, HrTeacherDetails Details, EligibilityDto Eligibility)> EvaluateInternalAsync(
        Guid processId, Guid teacherUserId, CancellationToken cancellationToken)
    {
        var snapshot = await db.TeacherSnapshots
            .FirstOrDefaultAsync(s => s.UserId == teacherUserId, cancellationToken)
            ?? throw AppException.NotFound("No se encontró la hoja de vida del docente. Inicie sesión nuevamente para sincronizarla.");

        var nextPosition = PositionLadder.GetNextPosition(snapshot.CurrentPosition)
            ?? throw AppException.Conflict(
                $"No existe una transición de promoción disponible desde {PositionLadder.Label(snapshot.CurrentPosition)}. " +
                "Por reglamento, no es posible postular desde Titular Agregado 3 hacia Titular Principal 1, y Titular Principal 3 es el grado máximo.");

        var requirement = await db.ProcessRequirements
            .FirstOrDefaultAsync(r => r.ProcessId == processId && r.FromPosition == snapshot.CurrentPosition, cancellationToken)
            ?? throw AppException.NotFound(
                $"El proceso no tiene configurados requisitos para la transición desde {PositionLadder.Label(snapshot.CurrentPosition)}.");

        var details = JsonSerializer.Deserialize<HrTeacherDetails>(snapshot.SnapshotJson, AppJson.Options)
            ?? throw AppException.UpstreamUnavailable("No fue posible leer la hoja de vida almacenada.");

        var profile = TeacherProfileMapper.Map(details);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = EligibilityEngine.Evaluate(profile, requirement, today);

        return (snapshot, details, ToDto(result, requirement.Notes));
    }

    internal static EligibilityDto ToDto(EligibilityResult result, string? notes) => new(
        result.FromPosition,
        result.ToPosition,
        PositionLadder.Label(result.FromPosition),
        PositionLadder.Label(result.ToPosition),
        result.IsEligible,
        [.. result.Requirements.Select(r => new RequirementEvaluationDto(r.Code, r.Label, r.Required, r.Actual, r.Met, r.Detail))],
        notes);
}
