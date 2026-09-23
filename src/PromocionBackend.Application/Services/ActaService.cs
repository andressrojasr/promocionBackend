using Microsoft.EntityFrameworkCore;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.Actas;
using PromocionBackend.Domain.Constants;
using PromocionBackend.Domain.Services;

namespace PromocionBackend.Application.Services;

/// <summary>
/// Genera el acta de promoción (PDF) de una comisión de CP para una facultad
/// determinada, con los docentes aprobados y no aprobados en esa sesión.
/// </summary>
public class ActaService(IAppDbContext db, IActaPdfBuilder pdfBuilder)
{
    private static readonly string[] MonthNames =
    [
        "enero", "febrero", "marzo", "abril", "mayo", "junio",
        "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"
    ];

    public async Task<byte[]> GenerateCpActaAsync(
        Guid commissionId, string facultyId, string facultyName, CancellationToken cancellationToken = default)
    {
        var commission = await db.Commissions
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == commissionId, cancellationToken)
            ?? throw AppException.NotFound("Comisión no encontrada.");

        if (commission.Type != CommissionTypes.Cp)
        {
            throw AppException.Forbidden("El acta de promoción solo está disponible para comisiones de CP.");
        }

        var applications = await db.Applications
            .Include(a => a.Teacher)
            .Include(a => a.Reviews)
            .Where(a => a.ProcessId == commission.ProcessId
                        && a.FacultyId == facultyId
                        && a.Reviews.Any(r => r.CommissionId == commissionId && r.Stage == ReviewStages.Cp))
            .ToListAsync(cancellationToken);

        var approved = new List<ActaApprovedRow>();
        var rejected = new List<ActaRejectedRow>();

        foreach (var application in applications)
        {
            var review = application.Reviews
                .Where(r => r.CommissionId == commissionId && r.Stage == ReviewStages.Cp)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefault();

            if (review is null)
            {
                continue;
            }

            if (review.Decision == ReviewDecisions.Approved)
            {
                approved.Add(new ActaApprovedRow(
                    application.Teacher.Identification,
                    application.TeacherName,
                    PositionLadder.Label(application.ToPosition),
                    review.Feedback));
            }
            else
            {
                rejected.Add(new ActaRejectedRow(
                    application.Teacher.Identification,
                    application.TeacherName,
                    review.Feedback));
            }
        }

        var distinctFrom = applications.Select(a => a.FromPosition).Distinct().ToList();
        var distinctTo = applications.Select(a => a.ToPosition).Distinct().ToList();

        var ecuadorTz = TimeZoneInfo.FindSystemTimeZoneById("America/Guayaquil");
        var timeLocal = TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(commission.CreatedAt, DateTimeKind.Utc), ecuadorTz);

        var reviewedNames = approved.Select(a => a.FullName)
            .Concat(rejected.Select(r => r.FullName))
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        var data = new ActaData(
            commission.Date.Day,
            MonthNames[commission.Date.Month - 1],
            commission.Date.Year,
            timeLocal.ToString("HH:mm"),
            facultyName,
            distinctFrom.Count == 1 ? PositionLadder.Label(distinctFrom[0]) : null,
            distinctTo.Count == 1 ? PositionLadder.Label(distinctTo[0]) : null,
            [.. commission.Members.OrderBy(m => m.OrderIndex).Select(m => new ActaMemberRow(m.CargoLabel, m.TeacherFullName))],
            reviewedNames,
            [.. approved.OrderBy(a => a.FullName)],
            [.. rejected.OrderBy(r => r.FullName)]);

        return pdfBuilder.Build(data);
    }

    /// <summary>Genera el acta a partir de una sesión de revisión ya registrada (evita reconstruir commissionId+facultyId a mano).</summary>
    public async Task<byte[]> GenerateCpActaBySessionAsync(Guid reviewSessionId, CancellationToken cancellationToken = default)
    {
        var session = await db.ReviewSessions
            .FirstOrDefaultAsync(s => s.Id == reviewSessionId, cancellationToken)
            ?? throw AppException.NotFound("Sesión de revisión no encontrada.");

        if (session.Type != CommissionTypes.Cp)
        {
            throw AppException.Forbidden("El acta de promoción solo está disponible para sesiones de CP.");
        }

        return await GenerateCpActaAsync(session.CommissionId, session.FacultyId, session.FacultyName, cancellationToken);
    }
}
