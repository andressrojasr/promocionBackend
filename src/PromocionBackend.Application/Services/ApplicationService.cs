using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Abstractions.External;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.Applications;
using PromocionBackend.Application.DTOs.Eligibility;
using PromocionBackend.Application.Mapping;
using PromocionBackend.Domain.Constants;
using PromocionBackend.Domain.Entities;
using PromocionBackend.Domain.Services;

namespace PromocionBackend.Application.Services;

/// <summary>
/// Ciclo de vida de las postulaciones: envío con validación de elegibilidad en el
/// servidor, revisiones por etapa (TH → CP → CA), apelaciones dentro del plazo de
/// 3 días y expiración perezosa del plazo de apelación.
/// </summary>
public class ApplicationService(
    IAppDbContext db,
    EligibilityService eligibilityService,
    NotificationService notificationService,
    IHrApiClient hrApi)
{
    public async Task<ApplicationDetailDto> SubmitAsync(SubmitApplicationRequest request, ICurrentUserService currentUser, string externalAccessToken, CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        var process = await db.Processes
            .FirstOrDefaultAsync(p => p.Id == request.ProcessId, cancellationToken)
            ?? throw AppException.NotFound("Proceso de promoción no encontrado.");

        if (!process.IsOpenAt(utcNow))
        {
            throw AppException.Conflict("El proceso no está abierto para postulaciones.");
        }

        var alreadyApplied = await db.Applications
            .AnyAsync(a => a.ProcessId == request.ProcessId && a.TeacherUserId == currentUser.UserId, cancellationToken);

        if (alreadyApplied)
        {
            throw AppException.Conflict("Ya existe una postulación suya en este proceso.");
        }

        var (details, eligibility) = await eligibilityService
            .EvaluateInternalAsync(request.ProcessId, currentUser.UserId, externalAccessToken, cancellationToken);

        if (!eligibility.IsEligible)
        {
            var missing = eligibility.Requirements.Where(r => !r.Met).Select(r => r.Label);
            throw AppException.Conflict(
                $"No cumple todos los requisitos para postular. Requisitos pendientes: {string.Join("; ", missing)}.");
        }

        var requirement = await db.ProcessRequirements
            .FirstOrDefaultAsync(r => r.ProcessId == process.Id && r.FromPosition == details.CurrentPosition, cancellationToken)
            ?? throw AppException.NotFound("Configuración de requisitos no encontrada para esta transición.");

        var items = ResolveItems(request.Items, details, requirement);

        var application = new PromotionApplication
        {
            Id = Guid.NewGuid(),
            ProcessId = process.Id,
            TeacherUserId = currentUser.UserId,
            FromPosition = eligibility.FromPosition,
            ToPosition = eligibility.ToPosition,
            Status = ApplicationStatus.Submitted,
            SubmittedAt = utcNow,
            TeacherId = details.TeacherId,
            TeacherName = details.FullName,
            CurrentPosition = details.CurrentPosition,
            ScorePct = details.Score?.Percentage,
            Items = items
        };

        db.Applications.Add(application);

        await notificationService.NotifyRoleAsync(
            Roles.Th,
            "Nueva postulación recibida",
            $"El proceso \"{process.Name}\" tiene una nueva postulación pendiente de revisión.",
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return await GetDetailAsync(application.Id, currentUser, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<ApplicationSummaryDto>> ListAsync(
        ICurrentUserService currentUser, string? status, Guid? processId, string? teacherId, CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        var query = db.Applications
            .Include(a => a.Process)
            .Include(a => a.Teacher)
            .AsQueryable();

        query = currentUser.Role switch
        {
            Roles.Teacher => query.Where(a => a.TeacherUserId == currentUser.UserId),
            // CA solo ve postulaciones que llegaron a apelación.
            Roles.Ca => query.Where(a => a.Status == ApplicationStatus.Appealed || a.Appeal != null),
            Roles.Th or Roles.Cp or Roles.Admin => query,
            _ => throw AppException.Forbidden("Su rol no tiene acceso a las postulaciones.")
        };

        if (processId is { } pid)
        {
            query = query.Where(a => a.ProcessId == pid);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (ApplicationStatusExtensions.TryFromStringValue(status, out var parsedStatus))
            {
                query = query.Where(a => a.Status == parsedStatus);
            }
            else
            {
                // Status inválido, retorna vacío
                return [];
            }
        }

        if (!string.IsNullOrWhiteSpace(teacherId))
        {
            query = query.Where(a => a.Teacher.Identification.Contains(teacherId));
        }

        var applications = await query
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync(cancellationToken);

        await ApplyAppealExpiryAsync(applications, utcNow, cancellationToken);

        var summaries = applications.Select(a => ToSummary(a, utcNow));

        // El filtro de status ya se aplicó en la query, así que no es necesario filtrar aquí
        // pero si la expiración cambió los estados, verificamos de nuevo
        return [.. summaries];
    }

    public async Task<ApplicationDetailDto> GetDetailAsync(
        Guid applicationId,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        var application = await db.Applications
            .Include(a => a.Process)
            .Include(a => a.Process.Requirements)
            .Include(a => a.Teacher)
            .Include(a => a.ReviewLocker)
            .Include(a => a.Items)
            .Include(a => a.Reviews).ThenInclude(r => r.Reviewer)
            .Include(a => a.Appeal)
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken)
            ?? throw AppException.NotFound("Postulación no encontrada.");

        EnsureCanView(application, currentUser);

        // Limpiar locks expirados
        await CleanExpiredLocksAsync(cancellationToken);

        // Liberar si el lock expiró
        if (application.ReviewLockedBy == currentUser.UserId &&
            application.ReviewLockExpiresAt.HasValue &&
            utcNow > application.ReviewLockExpiresAt)
        {
            application.ReviewLockedBy = null;
            application.ReviewLockedAt = null;
            application.ReviewLockExpiresAt = null;
        }

        // Crear/renovar lock para usuario actual (si puede revisar)
        var reviewableStatus = ApplicationStateMachine.ReviewableStatusFor(currentUser.Role);
        var canReview = reviewableStatus == application.Status;
        if (canReview)
        {
            const int LOCK_TIMEOUT_MINUTES = 30;
            application.ReviewLockedBy = currentUser.UserId;
            application.ReviewLockedAt = utcNow;
            application.ReviewLockExpiresAt = utcNow.AddMinutes(LOCK_TIMEOUT_MINUTES);
            await db.SaveChangesAsync(cancellationToken);
        }

        await ApplyAppealExpiryAsync([application], utcNow, cancellationToken);

        var canAppeal = currentUser.Role == Roles.Teacher &&
                        application.TeacherUserId == currentUser.UserId &&
                        ApplicationStateMachine.CanAppeal(application.Status, application.CpDecisionAt, null, utcNow);

        // Retornar estructura básica de requisitos del proceso (sin cálculos)
        var requirement = application.Process.Requirements
            .FirstOrDefault(r => r.FromPosition == application.FromPosition);

        EligibilityDto? eligibility = null;
        if (requirement is not null)
        {
            var requirements = new List<RequirementEvaluationDto>
            {
                new("YEARS_IN_RANK", $"Experiencia mínima como {PositionLadder.Label(application.FromPosition)}", $"{requirement.MinYearsInPosition} años", "", false, "", requirement.MinYearsInPosition),
                new("PUBLICATIONS", "Obras de relevancia o artículos indexados publicados", $"{requirement.MinPublications} publicaciones", "", false, "", requirement.MinPublications),
                new("TRAINING_HOURS", $"Horas de capacitación en los últimos {requirement.TrainingWindowYears} años", $"{requirement.MinTrainingHours} horas", "", false, "", requirement.MinTrainingHours),
            };

            if (requirement.MinPedagogicalTrainingPct.HasValue)
            {
                requirements.Add(new("PEDAGOGICAL_HOURS", $"Actualización pedagógica ({requirement.MinPedagogicalTrainingPct}%)", $"{(int)(requirement.MinTrainingHours * requirement.MinPedagogicalTrainingPct.Value / 100)} horas", "", false, "", null));
            }

            requirements.Add(new("LANGUAGE_LEVEL", "Idioma distinto al castellano certificado", requirement.RequiredLanguageLevel ?? "B1", "", false, "", null));

            eligibility = new EligibilityDto(
                application.FromPosition,
                application.ToPosition,
                PositionLadder.Label(application.FromPosition),
                PositionLadder.Label(application.ToPosition),
                false,
                requirements,
                requirement.Notes,
                null);
        }

        var lockInfo = application.ReviewLockedBy.HasValue && application.ReviewLocker != null
            ? new ReviewLockInfoDto(application.ReviewLocker.FullName, application.ReviewLockedAt, application.ReviewLockExpiresAt)
            : null;

        var ecuadorTz = TimeZoneInfo.FindSystemTimeZoneById("America/Guayaquil");

        return new ApplicationDetailDto(
            ToSummary(application, utcNow),
            [.. application.Items.Select(i => new ApplicationItemDto(i.ItemType, i.ExternalItemId, i.Title, i.DocumentUrl, i.DocumentDateOriginal))],
            [.. application.Reviews
                .OrderBy(r => r.CreatedAt)
                .Select(r => new ReviewDto(
                    r.Stage,
                    r.Reviewer.FullName,
                    r.ReviewerRole,
                    r.Decision,
                    r.Feedback,
                    FormatDateTimeToEcuadorString(r.CreatedAt, ecuadorTz)))],
            application.Appeal is { } appeal
                ? new AppealDto(appeal.Justification, FormatDateTimeToEcuadorString(appeal.SubmittedAt, ecuadorTz))
                : null,
            eligibility,
            canAppeal,
            lockInfo);
    }


    public async Task<ApplicationDetailDto> ReviewAsync(
        Guid applicationId, ReviewRequest request, ICurrentUserService currentUser, CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        var stage = ApplicationStateMachine.StageForRole(currentUser.Role)
            ?? throw AppException.Forbidden("Su rol no participa en la revisión de postulaciones.");

        var application = await db.Applications
            .Include(a => a.Process)
            .Include(a => a.Teacher)
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken)
            ?? throw AppException.NotFound("Postulación no encontrada.");

        await ApplyAppealExpiryAsync([application], utcNow, cancellationToken);

        var approved = request.Decision == ReviewDecisions.Approved;

        if (!approved && string.IsNullOrWhiteSpace(request.Feedback))
        {
            throw AppException.BadRequest("La retroalimentación es obligatoria cuando se rechaza una postulación.");
        }

        var nextStatus = ApplicationStateMachine.GetNextStatus(application.Status, stage, approved)
            ?? throw AppException.Conflict(
                $"La postulación no se encuentra en un estado revisable por su rol (estado actual: {application.Status}).");

        application.Status = nextStatus;

        if (stage == ReviewStages.Cp && !approved)
        {
            application.CpDecisionAt = utcNow;
        }

        if (nextStatus.IsFinal())
        {
            application.DecidedAt = utcNow;
        }

        db.ApplicationReviews.Add(new ApplicationReview
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            Stage = stage,
            ReviewerUserId = currentUser.UserId,
            ReviewerRole = currentUser.Role,
            Decision = request.Decision,
            Feedback = request.Feedback,
            CreatedAt = utcNow
        });

        NotifyTeacherOfDecision(application, stage, approved, request.Feedback, utcNow);

        await db.SaveChangesAsync(cancellationToken);

        // Liberar lock después de guardar la decisión
        application.ReviewLockedBy = null;
        application.ReviewLockedAt = null;
        application.ReviewLockExpiresAt = null;
        await db.SaveChangesAsync(cancellationToken);

        return await GetDetailAsync(applicationId, currentUser, cancellationToken: cancellationToken);
    }

    public async Task<ApplicationDetailDto> AppealAsync(
        Guid applicationId, AppealRequest request, ICurrentUserService currentUser, CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        var application = await db.Applications
            .Include(a => a.Process)
            .Include(a => a.Teacher)
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.TeacherUserId == currentUser.UserId, cancellationToken)
            ?? throw AppException.NotFound("Postulación no encontrada.");

        var expired = await ApplyAppealExpiryAsync([application], utcNow, cancellationToken);

        if (expired)
        {
            throw AppException.Conflict("El plazo de 3 días para apelar ha vencido; la postulación quedó rechazada de forma definitiva.");
        }

        if (!ApplicationStateMachine.CanAppeal(application.Status, application.CpDecisionAt, null, utcNow))
        {
            throw AppException.Conflict("La postulación no se encuentra en un estado apelable.");
        }

        db.Appeals.Add(new Appeal
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            Justification = request.Justification.Trim(),
            SubmittedAt = utcNow
        });

        application.Status = ApplicationStatus.Appealed;

        await notificationService.NotifyRoleAsync(
            Roles.Ca,
            "Nueva apelación recibida",
            $"El docente {application.Teacher.FullName} apeló el rechazo de su postulación en el proceso \"{application.Process.Name}\".",
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return await GetDetailAsync(applicationId, currentUser, cancellationToken: cancellationToken);
    }

    // ---------- Auxiliares ----------

    /// <summary>
    /// Expiración perezosa del plazo de apelación: si un rechazo de CP superó los 3 días
    /// sin apelación, la postulación pasa a rechazo definitivo y se notifica al docente.
    /// Devuelve true si alguna postulación expiró en esta pasada.
    /// </summary>
    private async Task<bool> ApplyAppealExpiryAsync(IReadOnlyList<PromotionApplication> applications, DateTime utcNow, CancellationToken cancellationToken)
    {
        var anyExpired = false;

        foreach (var application in applications)
        {
            if (application.Status != ApplicationStatus.CpRejected)
            {
                continue;
            }

            var effective = ApplicationStateMachine.GetEffectiveStatus(application.Status, application.CpDecisionAt, null, utcNow);

            if (effective == ApplicationStatus.Rejected)
            {
                application.Status = ApplicationStatus.Rejected;
                application.DecidedAt = application.CpDecisionAt?.AddDays(ApplicationStateMachine.AppealWindowDays) ?? utcNow;

                notificationService.Notify(
                    application.TeacherUserId,
                    "Plazo de apelación vencido",
                    "El plazo de 3 días para apelar el rechazo de su postulación venció, por lo que quedó rechazada de forma definitiva.");

                anyExpired = true;
            }
        }

        if (anyExpired)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return anyExpired;
    }

    private void NotifyTeacherOfDecision(PromotionApplication application, string stage, bool approved, string? feedback, DateTime utcNow)
    {
        var processName = application.Process.Name;
        var feedbackSuffix = string.IsNullOrWhiteSpace(feedback) ? string.Empty : $" Retroalimentación: {feedback}";

        var (title, message) = (stage, approved) switch
        {
            (ReviewStages.Th, true) => (
                "Postulación aprobada por Talento Humano",
                $"Su postulación al proceso \"{processName}\" fue aprobada por Talento Humano y pasó a revisión de la Comisión de Promoción.{feedbackSuffix}"),
            (ReviewStages.Th, false) => (
                "Postulación rechazada por Talento Humano",
                $"Su postulación al proceso \"{processName}\" fue rechazada por Talento Humano.{feedbackSuffix}"),
            (ReviewStages.Cp, true) => (
                "¡Promoción aprobada!",
                $"La Comisión de Promoción aprobó su postulación al proceso \"{processName}\". Su promoción ha finalizado con éxito.{feedbackSuffix}"),
            (ReviewStages.Cp, false) => (
                "Postulación rechazada por la Comisión de Promoción",
                $"Su postulación al proceso \"{processName}\" fue rechazada por la Comisión de Promoción. " +
                $"Tiene un plazo de {ApplicationStateMachine.AppealWindowDays} días (hasta el {utcNow.AddDays(ApplicationStateMachine.AppealWindowDays):yyyy-MM-dd HH:mm} UTC) para apelar.{feedbackSuffix}"),
            (ReviewStages.Ca, true) => (
                "Apelación aceptada: promoción aprobada",
                $"La Comisión de Apelaciones aceptó su apelación en el proceso \"{processName}\". Su promoción ha finalizado con éxito.{feedbackSuffix}"),
            _ => (
                "Apelación rechazada",
                $"La Comisión de Apelaciones rechazó su apelación en el proceso \"{processName}\". La postulación quedó rechazada de forma definitiva.{feedbackSuffix}")
        };

        notificationService.Notify(application.TeacherUserId, title, message);
    }

    private static void EnsureCanView(PromotionApplication application, ICurrentUserService currentUser)
    {
        var canView = currentUser.Role switch
        {
            Roles.Teacher => application.TeacherUserId == currentUser.UserId,
            Roles.Th or Roles.Cp or Roles.Ca or Roles.Admin => true,
            _ => false
        };

        if (!canView)
        {
            throw AppException.Forbidden("No tiene acceso a esta postulación.");
        }
    }

    private static string FormatDateTimeToEcuadorString(DateTime utcDateTime, TimeZoneInfo ecuadorTz)
    {
        var ecuadorTime = TimeZoneInfo.ConvertTime(
            DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc),
            ecuadorTz);
        var offset = ecuadorTz.GetUtcOffset(ecuadorTime);
        var dateTimeOffset = new DateTimeOffset(ecuadorTime, offset);
        return dateTimeOffset.ToString("O");
    }

    private static ApplicationSummaryDto ToSummary(PromotionApplication application, DateTime utcNow)
    {
        var ecuadorTz = TimeZoneInfo.FindSystemTimeZoneById("America/Guayaquil");
        var submittedAtStr = FormatDateTimeToEcuadorString(application.SubmittedAt, ecuadorTz);

        var appealDeadline = ApplicationStateMachine.GetAppealDeadline(application.Status, application.CpDecisionAt, null);
        var appealDeadlineStr = appealDeadline.HasValue
            ? FormatDateTimeToEcuadorString(appealDeadline.Value, ecuadorTz)
            : null;

        return new(
            application.Id,
            application.ProcessId,
            application.Process.Name,
            application.TeacherUserId,
            application.Teacher.FullName,
            application.Teacher.Identification,
            application.FromPosition,
            application.ToPosition,
            PositionLadder.Label(application.FromPosition),
            PositionLadder.Label(application.ToPosition),
            ApplicationStateMachine.GetEffectiveStatus(application.Status, application.CpDecisionAt, null, utcNow).ToStringValue(),
            submittedAtStr,
            appealDeadlineStr,
            application.ScorePct,
            application.DecidedAt.HasValue ? (int?)(application.DecidedAt.Value - application.SubmittedAt).Days : null,
            application.Reviews.FirstOrDefault()?.Reviewer?.FullName);
    }

    /// <summary>
    /// Valida que cada ítem seleccionado exista en la hoja de vida congelada y
    /// resuelve su título y documento de respaldo.
    /// </summary>
    private static List<ApplicationItem> ResolveItems(List<ApplicationItemRequest> requests, HrTeacherDetails details, ProcessRequirement requirement)
    {
        if (requests.Count == 0)
        {
            throw AppException.BadRequest("Debe seleccionar al menos un documento de respaldo para su postulación.");
        }

        var catalog = BuildItemCatalog(details);
        var allowedTypes = GetAllowedItemTypes(requirement);
        var items = new List<ApplicationItem>();
        var seen = new HashSet<(string, string)>();

        foreach (var request in requests)
        {
            if (!ApplicationItemTypes.IsValid(request.ItemType))
            {
                throw AppException.BadRequest($"Tipo de ítem inválido: '{request.ItemType}'.");
            }

            if (!allowedTypes.Contains(request.ItemType))
            {
                throw AppException.BadRequest(
                    $"El tipo de ítem '{request.ItemType}' no es válido para los requisitos de este proceso.");
            }

            if (!seen.Add((request.ItemType, request.ExternalItemId)))
            {
                throw AppException.BadRequest($"El ítem '{request.ExternalItemId}' está duplicado.");
            }

            if (!catalog.TryGetValue((request.ItemType, request.ExternalItemId), out var info))
            {
                throw AppException.BadRequest(
                    $"El ítem '{request.ExternalItemId}' ({request.ItemType}) no existe en su hoja de vida.");
            }

            items.Add(new ApplicationItem
            {
                Id = Guid.NewGuid(),
                ItemType = request.ItemType,
                ExternalItemId = request.ExternalItemId,
                Title = info.Title,
                DocumentUrl = info.DocumentUrl,
                DocumentDateOriginal = request.DocumentDateOriginal
            });
        }

        return items;
    }

    private static HashSet<string> GetAllowedItemTypes(ProcessRequirement requirement)
    {
        var allowed = new HashSet<string>();

        if (requirement.MinPublications > 0 || requirement.MinPublicationsInOtherLanguage > 0)
            allowed.Add(ApplicationItemTypes.Publication);

        if (requirement.MinTrainingHours > 0 || requirement.MinPedagogicalTrainingPct.HasValue)
            allowed.Add(ApplicationItemTypes.ReceivedTraining);

        if (requirement.MinGivenTrainingHours.HasValue && requirement.MinGivenTrainingHours > 0)
            allowed.Add(ApplicationItemTypes.GivenTraining);

        if (requirement.MinProjectMonths.HasValue && requirement.MinProjectMonths > 0)
            allowed.Add(ApplicationItemTypes.ResearchProject);

        if (requirement.MinDoctoralTheses.HasValue && requirement.MinDoctoralTheses > 0)
            allowed.Add(ApplicationItemTypes.DoctoralThesis);

        if (!string.IsNullOrWhiteSpace(requirement.RequiredLanguageLevel))
            allowed.Add(ApplicationItemTypes.Language);

        // La experiencia siempre es un requisito implícito (mínimo años en posición)
        if (requirement.MinYearsInPosition > 0)
            allowed.Add(ApplicationItemTypes.Experience);

        return allowed;
    }

    private static Dictionary<(string Type, string Id), (string Title, string? DocumentUrl)> BuildItemCatalog(HrTeacherDetails details)
    {
        var catalog = new Dictionary<(string, string), (string, string?)>();

        foreach (var p in details.Publications)
            catalog[(ApplicationItemTypes.Publication, p.Id)] = (p.Name, p.SupportingDocumentUrl);

        foreach (var t in details.ReceivedTrainings)
            catalog[(ApplicationItemTypes.ReceivedTraining, t.Id)] = (t.Name, t.SupportingDocumentUrl);

        foreach (var t in details.GivenTrainings)
            catalog[(ApplicationItemTypes.GivenTraining, t.Id)] = (t.Name, t.SupportingDocumentUrl);

        foreach (var r in details.ResearchProjects)
            catalog[(ApplicationItemTypes.ResearchProject, r.Id)] = (r.Name, r.SupportingDocumentUrl);

        foreach (var d in details.DoctoralTheses)
            catalog[(ApplicationItemTypes.DoctoralThesis, d.Id)] = (d.Title, d.SupportingDocumentUrl);

        foreach (var l in details.Languages)
            catalog[(ApplicationItemTypes.Language, l.Id)] = ($"Certificación {l.Language} {l.Level}", l.SupportingDocumentUrl);

        foreach (var e in details.Experience)
            catalog[(ApplicationItemTypes.Experience, e.Id)] = ($"{e.Position} - {e.Institution}", e.SupportingDocumentUrl);

        return catalog;
    }

    private async Task CleanExpiredLocksAsync(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var expiredLocks = await db.Applications
            .Where(a => a.ReviewLockedBy != null &&
                        a.ReviewLockExpiresAt.HasValue &&
                        a.ReviewLockExpiresAt <= utcNow)
            .ToListAsync(cancellationToken);

        foreach (var app in expiredLocks)
        {
            app.ReviewLockedBy = null;
            app.ReviewLockedAt = null;
            app.ReviewLockExpiresAt = null;
        }

        if (expiredLocks.Any())
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
