using Microsoft.EntityFrameworkCore;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.Actas;
using PromocionBackend.Domain.Constants;
using PromocionBackend.Domain.Entities;
using PromocionBackend.Domain.Services;

namespace PromocionBackend.Application.Services;

/// <summary>
/// Genera el acta de promoción (PDF) de una sesión de revisión de CP ya cerrada, con los
/// docentes aprobados y no aprobados en esa sesión para una categoría (transición) concreta.
/// </summary>
public class ActaService(IAppDbContext db, IActaPdfBuilder pdfBuilder)
{
    private static readonly string[] MonthNames =
    [
        "enero", "febrero", "marzo", "abril", "mayo", "junio",
        "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"
    ];

    /// <summary>Categorías con decisiones de CP en la sesión (para elegir de cuál generar el acta).</summary>
    public async Task<IReadOnlyList<ActaCategoryDto>> GetCpActaCategoriesBySessionAsync(
        Guid reviewSessionId, CancellationToken cancellationToken = default)
    {
        var session = await LoadCpSessionAsync(reviewSessionId, cancellationToken);
        var decisions = await LoadSessionDecisionsAsync(session, cancellationToken);

        return [.. decisions
            .GroupBy(d => (d.Application.FromPosition, d.Application.ToPosition))
            .Select(g => new ActaCategoryDto(
                g.Key.FromPosition,
                g.Key.ToPosition,
                PositionLadder.Label(g.Key.FromPosition),
                PositionLadder.Label(g.Key.ToPosition),
                g.Count(d => d.Outcome == ActaOutcome.Promoted),
                g.Count(d => d.Outcome != ActaOutcome.Promoted),
                g.Count(d => d.Outcome == ActaOutcome.PendingAppeal)))
            .OrderBy(c => c.FromLabel)
            .ThenBy(c => c.ToLabel)];
    }

    /// <summary>
    /// Genera el acta de una sesión cerrada. Si la sesión decidió postulaciones de más de una
    /// categoría, <paramref name="fromPosition"/> y <paramref name="toPosition"/> son obligatorios.
    /// </summary>
    public async Task<(byte[] Pdf, string CategoryLabel, bool IsProvisional)> GenerateCpActaBySessionAsync(
        Guid reviewSessionId, string? fromPosition, string? toPosition, CancellationToken cancellationToken = default)
    {
        var session = await LoadCpSessionAsync(reviewSessionId, cancellationToken);

        var commission = await db.Commissions
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == session.CommissionId, cancellationToken)
            ?? throw AppException.NotFound("Comisión no encontrada.");

        var decisions = await LoadSessionDecisionsAsync(session, cancellationToken);

        var categories = decisions
            .Select(d => (d.Application.FromPosition, d.Application.ToPosition))
            .Distinct()
            .ToList();

        if (categories.Count == 0)
        {
            throw AppException.NotFound("La sesión no tiene postulaciones decididas para generar el acta.");
        }

        (string From, string To) category;
        if (!string.IsNullOrWhiteSpace(fromPosition) && !string.IsNullOrWhiteSpace(toPosition))
        {
            category = (fromPosition, toPosition);
            if (!categories.Contains(category))
            {
                throw AppException.NotFound("La sesión no tiene decisiones en la categoría seleccionada.");
            }
        }
        else if (categories.Count == 1)
        {
            category = categories[0];
        }
        else
        {
            throw AppException.BadRequest("La sesión tiene varias categorías; seleccione la categoría del acta.");
        }

        var approved = new List<ActaApprovedRow>();
        var rejected = new List<ActaRejectedRow>();

        var pendingCount = 0;

        foreach (var decision in decisions.Where(d =>
                     d.Application.FromPosition == category.From && d.Application.ToPosition == category.To))
        {
            var application = decision.Application;

            if (decision.Outcome == ActaOutcome.Promoted)
            {
                approved.Add(new ActaApprovedRow(
                    application.Teacher.Identification,
                    application.TeacherName,
                    PositionLadder.Label(application.ToPosition),
                    decision.Observation));
            }
            else
            {
                if (decision.Outcome == ActaOutcome.PendingAppeal)
                {
                    pendingCount++;
                }

                rejected.Add(new ActaRejectedRow(
                    application.Teacher.Identification,
                    application.TeacherName,
                    decision.Observation));
            }
        }

        var ecuadorTz = TimeZoneInfo.FindSystemTimeZoneById("America/Guayaquil");
        var timeLocal = TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(commission.CreatedAt, DateTimeKind.Utc), ecuadorTz);

        var reviewedNames = approved.Select(a => a.FullName)
            .Concat(rejected.Select(r => r.FullName))
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        var originLabel = PositionLadder.Label(category.From);
        var destinationLabel = PositionLadder.Label(category.To);

        var data = new ActaData(
            commission.Date.Day,
            MonthNames[commission.Date.Month - 1],
            commission.Date.Year,
            timeLocal.ToString("HH:mm"),
            session.FacultyName,
            originLabel,
            destinationLabel,
            [.. commission.Members.OrderBy(m => m.OrderIndex).Select(m => new ActaMemberRow(m.CargoLabel, m.TeacherFullName))],
            reviewedNames,
            [.. approved.OrderBy(a => a.FullName)],
            [.. rejected.OrderBy(r => r.FullName)],
            pendingCount);

        return (pdfBuilder.Build(data), $"{originLabel} a {destinationLabel}", data.IsProvisional);
    }

    private async Task<ReviewSession> LoadCpSessionAsync(Guid reviewSessionId, CancellationToken cancellationToken)
    {
        var session = await db.ReviewSessions
            .FirstOrDefaultAsync(s => s.Id == reviewSessionId, cancellationToken)
            ?? throw AppException.NotFound("Sesión de revisión no encontrada.");

        if (session.Type != CommissionTypes.Cp)
        {
            throw AppException.Forbidden("El acta de promoción solo está disponible para sesiones de CP.");
        }

        if (session.ClosedAt is null)
        {
            throw AppException.Conflict("Debe cerrar la sesión de revisión antes de generar el acta.");
        }

        return session;
    }

    private enum ActaOutcome
    {
        /// <summary>Se promociona (aprobado por CP o aprobado en apelación por CA).</summary>
        Promoted,

        /// <summary>No se promociona de forma definitiva.</summary>
        NotPromoted,

        /// <summary>Rechazo de CP que todavía puede cambiar: plazo de apelación vigente o apelación en trámite.</summary>
        PendingAppeal
    }

    private sealed record SessionDecision(
        PromotionApplication Application,
        ApplicationReview Review,
        ActaOutcome Outcome,
        string? Observation);

    /// <summary>
    /// Decisión de CP de cada postulación dentro de la sesión, con su resultado FINAL:
    /// un rechazo puede cambiar por apelación (aprobado/rechazado por CA) o seguir pendiente.
    /// </summary>
    private async Task<List<SessionDecision>> LoadSessionDecisionsAsync(
        ReviewSession session, CancellationToken cancellationToken)
    {
        var applications = await db.Applications
            .Include(a => a.Teacher)
            .Include(a => a.Reviews)
            .Include(a => a.Appeal)
            .Where(a => a.ProcessId == session.ProcessId
                        && a.Reviews.Any(r => r.ReviewSessionId == session.Id && r.Stage == ReviewStages.Cp))
            .ToListAsync(cancellationToken);

        var utcNow = DateTime.UtcNow;
        var ecuadorTz = TimeZoneInfo.FindSystemTimeZoneById("America/Guayaquil");
        string Date(DateTime utc) =>
            TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(utc, DateTimeKind.Utc), ecuadorTz).ToString("dd/MM/yyyy");
        static string Join(string head, string? feedback) =>
            string.IsNullOrWhiteSpace(feedback) ? head : $"{head}. {feedback}";

        return [.. applications.Select(a =>
        {
            var cpReview = a.Reviews
                .Where(r => r.ReviewSessionId == session.Id && r.Stage == ReviewStages.Cp)
                .OrderByDescending(r => r.CreatedAt)
                .First();

            if (cpReview.Decision == ReviewDecisions.Approved)
            {
                return new SessionDecision(a, cpReview, ActaOutcome.Promoted, cpReview.Feedback);
            }

            var caReview = a.Reviews
                .Where(r => r.Stage == ReviewStages.Ca)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefault();

            var effective = ApplicationStateMachine.GetEffectiveStatus(a.Status, a.CpDecisionAt, null, utcNow);
            var caDate = Date(caReview?.CreatedAt ?? a.DecidedAt ?? utcNow);

            return effective switch
            {
                ApplicationStatus.Approved => new SessionDecision(a, cpReview, ActaOutcome.Promoted,
                    Join($"Aprobado en apelación por la Comisión de Apelaciones ({caDate})", caReview?.Feedback)),

                ApplicationStatus.Rejected when a.Appeal is not null => new SessionDecision(a, cpReview, ActaOutcome.NotPromoted,
                    Join($"Rechazado en apelación por la Comisión de Apelaciones ({caDate})", caReview?.Feedback)),

                ApplicationStatus.Rejected => new SessionDecision(a, cpReview, ActaOutcome.NotPromoted,
                    Join("No presentó apelación dentro del plazo", cpReview.Feedback)),

                ApplicationStatus.Appealed => new SessionDecision(a, cpReview, ActaOutcome.PendingAppeal,
                    Join("Apelación en trámite ante la Comisión de Apelaciones", cpReview.Feedback)),

                ApplicationStatus.CpRejected => new SessionDecision(a, cpReview, ActaOutcome.PendingAppeal,
                    Join($"En plazo de apelación hasta el {Date(ApplicationStateMachine.GetAppealDeadline(effective, a.CpDecisionAt, null) ?? utcNow)}", cpReview.Feedback)),

                _ => new SessionDecision(a, cpReview, ActaOutcome.NotPromoted, cpReview.Feedback)
            };
        })];
    }
}
