using Microsoft.EntityFrameworkCore;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.DTOs.Dashboard;
using PromocionBackend.Domain.Constants;
using PromocionBackend.Domain.Services;

namespace PromocionBackend.Application.Services;

/// <summary>
/// Contadores del dashboard por rol.
/// </summary>
public class DashboardService(IAppDbContext db)
{
    public async Task<DashboardStatsDto> GetStatsAsync(ICurrentUserService currentUser, CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var counters = new Dictionary<string, int>();

        counters["unreadNotifications"] = await db.Notifications
            .CountAsync(n => n.UserId == currentUser.UserId && !n.IsRead, cancellationToken);

        switch (currentUser.Role)
        {
            case Roles.Teacher:
                counters["openProcesses"] = await db.Processes
                    .CountAsync(p => p.StartDate <= utcNow && p.EndDate >= utcNow, cancellationToken);
                counters["myApplications"] = await db.Applications
                    .CountAsync(a => a.TeacherUserId == currentUser.UserId, cancellationToken);
                counters["myApplicationsInProgress"] = await db.Applications
                    .Where(a => a.TeacherUserId == currentUser.UserId)
                    .InProgress()
                    .CountAsync(cancellationToken);
                counters["myApplicationsApproved"] = await db.Applications
                    .Where(a => a.TeacherUserId == currentUser.UserId)
                    .Approved()
                    .CountAsync(cancellationToken);
                break;

            case Roles.Th:
                counters["pendingReview"] = await db.Applications
                    .PendingTh()
                    .CountAsync(cancellationToken);
                counters["totalApplications"] = await db.Applications.CountAsync(cancellationToken);
                break;

            case Roles.Cp:
                counters["pendingReview"] = await db.Applications
                    .PendingCp()
                    .CountAsync(cancellationToken);
                counters["openProcesses"] = await db.Processes
                    .CountAsync(p => p.StartDate <= utcNow && p.EndDate >= utcNow, cancellationToken);
                counters["totalProcesses"] = await db.Processes.CountAsync(cancellationToken);
                counters["approvedApplications"] = await db.Applications
                    .Approved()
                    .CountAsync(cancellationToken);
                break;

            case Roles.Ca:
                counters["pendingAppeals"] = await db.Applications
                    .PendingCa()
                    .CountAsync(cancellationToken);
                counters["resolvedAppeals"] = await db.Appeals
                    .CountAsync(ap => ap.Application.Status != ApplicationStatus.Appealed, cancellationToken);
                break;

            case Roles.Admin:
                counters["totalUsers"] = await db.Users.CountAsync(cancellationToken);
                counters["activeUsers"] = await db.Users.CountAsync(u => u.IsActive, cancellationToken);
                counters["teachers"] = await db.Users.CountAsync(u => u.Role == Roles.Teacher, cancellationToken);
                counters["totalProcesses"] = await db.Processes.CountAsync(cancellationToken);
                counters["totalApplications"] = await db.Applications.CountAsync(cancellationToken);
                break;
        }

        return new DashboardStatsDto(currentUser.Role, counters);
    }

    public async Task<CpDashboardDataDto> GetCpDashboardAsync(
        string? status = null,
        string? processId = null,
        string? teacherId = null,
        CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        var query = db.Applications
            .Include(a => a.Process)
            .Include(a => a.Teacher)
            .Include(a => a.Reviews.Where(r => r.Stage == ReviewStages.Cp))
                .ThenInclude(r => r.Reviewer)
            .AsQueryable();

        // Aplicar filtros
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (ApplicationStatusExtensions.TryFromStringValue(status, out var parsedStatus))
            {
                query = query.Where(a => a.Status == parsedStatus);
            }
            else
            {
                // Status inválido, retorna vacío
                return new CpDashboardDataDto(
                    new CpDashboardStatsDto(0, 0, 0, 0, 0, 0),
                    [],
                    [],
                    []);
            }
        }

        if (!string.IsNullOrWhiteSpace(processId) && Guid.TryParse(processId, out var parsedProcessId))
        {
            query = query.Where(a => a.ProcessId == parsedProcessId);
        }

        if (!string.IsNullOrWhiteSpace(teacherId))
        {
            query = query.Where(a => a.Teacher.Identification.Contains(teacherId));
        }

        var applications = await query.ToListAsync(cancellationToken);

        var totalApplications = applications.Count;
        var pendingReview = applications.Count(a => a.Status == ApplicationStatus.ThApproved);
        var approvedByCP = applications.Count(a => a.Status == ApplicationStatus.Approved);
        var rejectedByCP = applications.Count(a => a.Status == ApplicationStatus.CpRejected);

        var approvalRate = totalApplications > 0
            ? Math.Round((decimal)approvedByCP / totalApplications * 100, 1)
            : 0m;

        var daysToDecision = applications
            .Where(a => a.Status.IsFinal() && a.DecidedAt.HasValue)
            .Select(a => (a.DecidedAt.Value - a.SubmittedAt).Days)
            .DefaultIfEmpty(0)
            .Average();

        var stats = new CpDashboardStatsDto(
            totalApplications,
            pendingReview,
            approvedByCP,
            rejectedByCP,
            approvalRate,
            daysToDecision);

        var reportData = applications
            .OrderByDescending(a => a.SubmittedAt)
            .Select(a => new CpApplicationReportDto(
                a.Id,
                a.Process.Name,
                a.TeacherName,
                a.Teacher.Identification,
                a.FromPosition,
                a.ToPosition,
                ApplicationStateMachine.GetEffectiveStatus(a.Status, a.CpDecisionAt, null, utcNow).ToStringValue(),
                a.SubmittedAt,
                a.DecidedAt,
                a.DecidedAt.HasValue ? (int?)(a.DecidedAt.Value - a.SubmittedAt).Days : null,
                a.ScorePct,
                a.Reviews.FirstOrDefault()?.Reviewer?.FullName))
            .ToList();

        var availableProcesses = applications
            .Select(a => a.Process.Name)
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        var availableStatuses = ApplicationStatusExtensions.GetAllValues().ToList();

        return new CpDashboardDataDto(stats, reportData, availableProcesses, availableStatuses);
    }
}
