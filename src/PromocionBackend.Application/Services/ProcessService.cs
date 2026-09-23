using Microsoft.EntityFrameworkCore;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.Commissions;
using PromocionBackend.Application.DTOs.Processes;
using PromocionBackend.Domain.Constants;
using PromocionBackend.Domain.Entities;
using PromocionBackend.Domain.Services;

namespace PromocionBackend.Application.Services;

/// <summary>
/// Gestión de procesos de promoción: creación con la configuración de requisitos
/// por transición, listado con estado derivado de la ventana de postulación y detalle.
/// </summary>
public class ProcessService(IAppDbContext db, CommissionService commissionService)
{
    private static readonly HashSet<string> ValidLanguageLevels =
        new(StringComparer.OrdinalIgnoreCase) { "A1", "A2", "B1", "B2", "C1", "C2" };

    public async Task<ProcessDetailDto> CreateAsync(CreateProcessRequest request, Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var process = new PromotionProcess
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            Requirements = [.. request.Requirements.Select(r => new ProcessRequirement
            {
                Id = Guid.NewGuid(),
                FromPosition = r.FromPosition,
                ToPosition = r.ToPosition,
                MinYearsInPosition = r.MinYearsInPosition,
                MinPublications = r.MinPublications,
                MinPublicationsInOtherLanguage = r.MinPublicationsInOtherLanguage,
                MinEvaluationScorePct = r.MinEvaluationScorePct,
                MinTrainingHours = r.MinTrainingHours,
                TrainingWindowYears = r.TrainingWindowYears,
                MinPedagogicalTrainingPct = r.MinPedagogicalTrainingPct,
                MinGivenTrainingHours = r.MinGivenTrainingHours,
                MinProjectMonths = r.MinProjectMonths,
                ProjectRoleScope = r.ProjectRoleScope,
                ApplyRoleMultipliers = r.ApplyRoleMultipliers,
                MinInternationalProjects = r.MinInternationalProjects,
                MinDoctoralTheses = r.MinDoctoralTheses,
                MinDoctoralThesesInRank = r.MinDoctoralThesesInRank,
                RequiredLanguageLevel = string.IsNullOrWhiteSpace(r.RequiredLanguageLevel) ? null : r.RequiredLanguageLevel.ToUpperInvariant(),
                Notes = r.Notes
            })]
        };

        db.Processes.Add(process);
        await db.SaveChangesAsync(cancellationToken);

        await commissionService.CreateInternalAsync(
            new CreateCommissionRequest
            {
                ProcessId = process.Id,
                Type = CommissionTypes.Cp,
                IsPrincipal = true,
                Date = DateTime.UtcNow.Date,
                Members = request.CommissionMembers
            },
            createdByUserId,
            cancellationToken);

        return await GetDetailAsync(process.Id, currentUser: null, cancellationToken);
    }

    public async Task<IReadOnlyList<ProcessSummaryDto>> ListAsync(ICurrentUserService? currentUser, CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        var processes = await db.Processes
            .Include(p => p.CreatedBy)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                Process = p,
                ApplicationsCount = p.Applications.Count
            })
            .ToListAsync(cancellationToken);

        var teacherContext = await GetTeacherContextAsync(currentUser, cancellationToken);

        return [.. processes.Select(p => ToSummary(p.Process, p.ApplicationsCount, utcNow, teacherContext))];
    }

    public async Task<ProcessDetailDto> GetDetailAsync(Guid processId, ICurrentUserService? currentUser, CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        var process = await db.Processes
            .Include(p => p.CreatedBy)
            .Include(p => p.Requirements)
            .FirstOrDefaultAsync(p => p.Id == processId, cancellationToken)
            ?? throw AppException.NotFound("Proceso de promoción no encontrado.");

        var applicationsCount = await db.Applications.CountAsync(a => a.ProcessId == processId, cancellationToken);
        var teacherContext = await GetTeacherContextAsync(currentUser, cancellationToken);

        var requirements = process.Requirements
            .OrderBy(r => LadderOrder(r.FromPosition))
            .Select(ToConfigDto)
            .ToList();

        return new ProcessDetailDto(
            ToSummary(process, applicationsCount, utcNow, teacherContext),
            requirements);
    }

    public IReadOnlyList<RequirementConfigDto> GetDefaults() => Defaults.RequirementDefaults.All;

    // ---------- Auxiliares ----------

    private sealed record TeacherContext(string? CurrentPosition, HashSet<Guid> AppliedProcessIds);

    private async Task<TeacherContext?> GetTeacherContextAsync(ICurrentUserService? currentUser, CancellationToken cancellationToken)
    {
        if (currentUser is null || currentUser.Role != Roles.Teacher)
        {
            return null;
        }

        var applications = await db.Applications
            .Where(a => a.TeacherUserId == currentUser.UserId)
            .Select(a => new { a.ProcessId, a.CurrentPosition })
            .ToListAsync(cancellationToken);

        // Usa la posición de la postulación más reciente
        var currentPosition = applications.FirstOrDefault()?.CurrentPosition;

        return new TeacherContext(currentPosition, [.. applications.Select(a => a.ProcessId)]);
    }

    private static ProcessSummaryDto ToSummary(PromotionProcess process, int applicationsCount, DateTime utcNow, TeacherContext? teacherContext)
    {
        TransitionDto? myTransition = null;
        bool? hasApplied = null;

        if (teacherContext is not null)
        {
            hasApplied = teacherContext.AppliedProcessIds.Contains(process.Id);

            if (teacherContext.CurrentPosition is { } position &&
                PositionLadder.GetNextPosition(position) is { } nextPosition)
            {
                myTransition = new TransitionDto(
                    position,
                    nextPosition,
                    PositionLadder.Label(position),
                    PositionLadder.Label(nextPosition));
            }
        }

        return new ProcessSummaryDto(
            process.Id,
            process.Name,
            process.Description,
            process.StartDate,
            process.EndDate,
            process.GetStatusAt(utcNow),
            applicationsCount,
            process.CreatedBy.FullName,
            process.CreatedAt,
            myTransition,
            hasApplied);
    }

    internal static RequirementConfigDto ToConfigDto(ProcessRequirement r) => new()
    {
        FromPosition = r.FromPosition,
        ToPosition = r.ToPosition,
        MinYearsInPosition = r.MinYearsInPosition,
        MinPublications = r.MinPublications,
        MinPublicationsInOtherLanguage = r.MinPublicationsInOtherLanguage,
        MinEvaluationScorePct = r.MinEvaluationScorePct,
        MinTrainingHours = r.MinTrainingHours,
        TrainingWindowYears = r.TrainingWindowYears,
        MinPedagogicalTrainingPct = r.MinPedagogicalTrainingPct,
        MinGivenTrainingHours = r.MinGivenTrainingHours,
        MinProjectMonths = r.MinProjectMonths,
        ProjectRoleScope = r.ProjectRoleScope,
        ApplyRoleMultipliers = r.ApplyRoleMultipliers,
        MinInternationalProjects = r.MinInternationalProjects,
        MinDoctoralTheses = r.MinDoctoralTheses,
        MinDoctoralThesesInRank = r.MinDoctoralThesesInRank,
        RequiredLanguageLevel = r.RequiredLanguageLevel,
        Notes = r.Notes
    };

    private static int LadderOrder(string fromPosition) =>
        PositionLadder.TransitionFromPositions.ToList().IndexOf(fromPosition);

    private static void ValidateRequest(CreateProcessRequest request)
    {
        if (request.EndDate <= request.StartDate)
        {
            throw AppException.BadRequest("La fecha de cierre debe ser posterior a la fecha de inicio del proceso.");
        }

        var expectedTransitions = PositionLadder.TransitionFromPositions;

        if (request.Requirements.Count != expectedTransitions.Count)
        {
            throw AppException.BadRequest(
                $"Se debe configurar exactamente {expectedTransitions.Count} transiciones de requisitos (una por cada transición del escalafón).");
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var requirement in request.Requirements)
        {
            var expectedTo = PositionLadder.GetNextPosition(requirement.FromPosition)
                ?? throw AppException.BadRequest($"La posición '{requirement.FromPosition}' no tiene una transición de promoción válida.");

            if (!string.Equals(expectedTo, requirement.ToPosition, StringComparison.OrdinalIgnoreCase))
            {
                throw AppException.BadRequest(
                    $"La transición desde '{requirement.FromPosition}' debe ser hacia '{expectedTo}'.");
            }

            if (!seen.Add(requirement.FromPosition))
            {
                throw AppException.BadRequest($"La transición desde '{requirement.FromPosition}' está duplicada.");
            }

            if (requirement.ProjectRoleScope is not (ProjectRoleScopes.Any or ProjectRoleScopes.Direction))
            {
                throw AppException.BadRequest("El alcance de roles de proyecto debe ser 'any' o 'direction'.");
            }

            if (!string.IsNullOrWhiteSpace(requirement.RequiredLanguageLevel) &&
                !ValidLanguageLevels.Contains(requirement.RequiredLanguageLevel))
            {
                throw AppException.BadRequest("El nivel de idioma debe ser un nivel MCER válido (A1, A2, B1, B2, C1, C2).");
            }
        }
    }
}
