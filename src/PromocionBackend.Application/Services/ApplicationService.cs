using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Abstractions.External;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.Applications;
using PromocionBackend.Application.DTOs.Eligibility;
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
    NotificationService notificationService)
{
    public async Task<ApplicationDetailDto> SubmitAsync(SubmitApplicationRequest request, ICurrentUserService currentUser, CancellationToken cancellationToken = default)
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

        var (snapshot, details, eligibility) = await eligibilityService
            .EvaluateInternalAsync(request.ProcessId, currentUser.UserId, cancellationToken);

        if (!eligibility.IsEligible)
        {
            var missing = eligibility.Requirements.Where(r => !r.Met).Select(r => r.Label);
            throw AppException.Conflict(
                $"No cumple todos los requisitos para postular. Requisitos pendientes: {string.Join("; ", missing)}.");
        }

        var items = ResolveItems(request.Items, details);

        var application = new PromotionApplication
        {
            Id = Guid.NewGuid(),
            ProcessId = process.Id,
            TeacherUserId = currentUser.UserId,
            FromPosition = eligibility.FromPosition,
            ToPosition = eligibility.ToPosition,
            Status = ApplicationStatuses.Submitted,
            SubmittedAt = utcNow,
            SnapshotJson = snapshot.SnapshotJson,
            EligibilityJson = JsonSerializer.Serialize(eligibility, AppJson.Options),
            Items = items
        };

        db.Applications.Add(application);

        await notificationService.NotifyRoleAsync(
            Roles.Th,
            "Nueva postulación recibida",
            $"El proceso \"{process.Name}\" tiene una nueva postulación pendiente de revisión.",
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return await GetDetailAsync(application.Id, currentUser, cancellationToken);
    }

    public async Task<IReadOnlyList<ApplicationSummaryDto>> ListAsync(
        ICurrentUserService currentUser, string? status, Guid? processId, CancellationToken cancellationToken = default)
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
            Roles.Ca => query.Where(a => a.Status == ApplicationStatuses.Appealed || a.Appeal != null),
            Roles.Th or Roles.Cp or Roles.Admin => query,
            _ => throw AppException.Forbidden("Su rol no tiene acceso a las postulaciones.")
        };

        if (processId is { } pid)
        {
            query = query.Where(a => a.ProcessId == pid);
        }

        var applications = await query
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync(cancellationToken);

        await ApplyAppealExpiryAsync(applications, utcNow, cancellationToken);

        var summaries = applications.Select(a => ToSummary(a, utcNow));

        if (!string.IsNullOrWhiteSpace(status))
        {
            summaries = summaries.Where(s => s.Status == status);
        }

        return [.. summaries];
    }

    public async Task<ApplicationDetailDto> GetDetailAsync(Guid applicationId, ICurrentUserService currentUser, CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        var application = await db.Applications
            .Include(a => a.Process)
            .Include(a => a.Teacher)
            .Include(a => a.Items)
            .Include(a => a.Reviews).ThenInclude(r => r.Reviewer)
            .Include(a => a.Appeal)
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken)
            ?? throw AppException.NotFound("Postulación no encontrada.");

        EnsureCanView(application, currentUser);

        await ApplyAppealExpiryAsync([application], utcNow, cancellationToken);

        var eligibility = string.IsNullOrWhiteSpace(application.EligibilityJson)
            ? null
            : JsonSerializer.Deserialize<EligibilityDto>(application.EligibilityJson, AppJson.Options);

        var canAppeal = currentUser.Role == Roles.Teacher &&
                        application.TeacherUserId == currentUser.UserId &&
                        ApplicationStateMachine.CanAppeal(application.Status, application.CpDecisionAt, utcNow);

        return new ApplicationDetailDto(
            ToSummary(application, utcNow),
            [.. application.Items.Select(i => new ApplicationItemDto(i.ItemType, i.ExternalItemId, i.Title, i.DocumentUrl))],
            [.. application.Reviews
                .OrderBy(r => r.CreatedAt)
                .Select(r => new ReviewDto(r.Stage, r.Reviewer.FullName, r.ReviewerRole, r.Decision, r.Feedback, r.CreatedAt))],
            application.Appeal is { } appeal ? new AppealDto(appeal.Justification, appeal.SubmittedAt) : null,
            eligibility,
            canAppeal);
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

        if (ApplicationStatuses.IsFinal(nextStatus))
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

        return await GetDetailAsync(applicationId, currentUser, cancellationToken);
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

        if (!ApplicationStateMachine.CanAppeal(application.Status, application.CpDecisionAt, utcNow))
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

        application.Status = ApplicationStatuses.Appealed;

        await notificationService.NotifyRoleAsync(
            Roles.Ca,
            "Nueva apelación recibida",
            $"El docente {application.Teacher.FullName} apeló el rechazo de su postulación en el proceso \"{application.Process.Name}\".",
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return await GetDetailAsync(applicationId, currentUser, cancellationToken);
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
            if (application.Status != ApplicationStatuses.CpRejected)
            {
                continue;
            }

            var effective = ApplicationStateMachine.GetEffectiveStatus(application.Status, application.CpDecisionAt, utcNow);

            if (effective == ApplicationStatuses.Rejected)
            {
                application.Status = ApplicationStatuses.Rejected;
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

    private static ApplicationSummaryDto ToSummary(PromotionApplication application, DateTime utcNow) => new(
        application.Id,
        application.ProcessId,
        application.Process.Name,
        application.TeacherUserId,
        application.Teacher.FullName,
        application.Teacher.TeacherId,
        application.FromPosition,
        application.ToPosition,
        PositionLadder.Label(application.FromPosition),
        PositionLadder.Label(application.ToPosition),
        ApplicationStateMachine.GetEffectiveStatus(application.Status, application.CpDecisionAt, utcNow),
        application.SubmittedAt,
        ApplicationStateMachine.GetAppealDeadline(application.Status, application.CpDecisionAt));

    /// <summary>
    /// Valida que cada ítem seleccionado exista en la hoja de vida congelada y
    /// resuelve su título y documento de respaldo.
    /// </summary>
    private static List<ApplicationItem> ResolveItems(List<ApplicationItemRequest> requests, HrTeacherDetails details)
    {
        if (requests.Count == 0)
        {
            throw AppException.BadRequest("Debe seleccionar al menos un documento de respaldo para su postulación.");
        }

        var catalog = BuildItemCatalog(details);
        var items = new List<ApplicationItem>();
        var seen = new HashSet<(string, string)>();

        foreach (var request in requests)
        {
            if (!ApplicationItemTypes.IsValid(request.ItemType))
            {
                throw AppException.BadRequest($"Tipo de ítem inválido: '{request.ItemType}'.");
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
                DocumentUrl = info.DocumentUrl
            });
        }

        return items;
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
}
