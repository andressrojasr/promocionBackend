using Microsoft.EntityFrameworkCore;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.DTOs.Dashboard;
using PromocionBackend.Domain.Constants;

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
                    .CountAsync(a => a.TeacherUserId == currentUser.UserId &&
                        (a.Status == ApplicationStatuses.Submitted ||
                         a.Status == ApplicationStatuses.ThApproved ||
                         a.Status == ApplicationStatuses.Appealed ||
                         a.Status == ApplicationStatuses.CpRejected), cancellationToken);
                counters["myApplicationsApproved"] = await db.Applications
                    .CountAsync(a => a.TeacherUserId == currentUser.UserId && a.Status == ApplicationStatuses.Approved, cancellationToken);
                break;

            case Roles.Th:
                counters["pendingReview"] = await db.Applications
                    .CountAsync(a => a.Status == ApplicationStatuses.Submitted, cancellationToken);
                counters["totalApplications"] = await db.Applications.CountAsync(cancellationToken);
                break;

            case Roles.Cp:
                counters["pendingReview"] = await db.Applications
                    .CountAsync(a => a.Status == ApplicationStatuses.ThApproved, cancellationToken);
                counters["openProcesses"] = await db.Processes
                    .CountAsync(p => p.StartDate <= utcNow && p.EndDate >= utcNow, cancellationToken);
                counters["totalProcesses"] = await db.Processes.CountAsync(cancellationToken);
                counters["approvedApplications"] = await db.Applications
                    .CountAsync(a => a.Status == ApplicationStatuses.Approved, cancellationToken);
                break;

            case Roles.Ca:
                counters["pendingAppeals"] = await db.Applications
                    .CountAsync(a => a.Status == ApplicationStatuses.Appealed, cancellationToken);
                counters["resolvedAppeals"] = await db.Appeals
                    .CountAsync(ap => ap.Application.Status != ApplicationStatuses.Appealed, cancellationToken);
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
}
