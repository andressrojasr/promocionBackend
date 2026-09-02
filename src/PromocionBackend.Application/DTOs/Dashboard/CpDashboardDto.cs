namespace PromocionBackend.Application.DTOs.Dashboard;

public record CpDashboardStatsDto(
    int TotalApplications,
    int PendingReview,
    int ApprovedByCP,
    int RejectedByCP,
    decimal ApprovalRatePercentage,
    double AverageDaysToDecision);

public record CpApplicationReportDto(
    Guid ApplicationId,
    string ProcessName,
    string TeacherName,
    string TeacherIdentification,
    string FromPosition,
    string ToPosition,
    string Status,
    DateTime SubmittedAt,
    DateTime? DecidedAt,
    int? DaysToDecision,
    decimal? ScorePct,
    string? CurrentReviewerName);

public record CpDashboardDataDto(
    CpDashboardStatsDto Stats,
    IReadOnlyList<CpApplicationReportDto> Applications,
    IReadOnlyList<string> AvailableProcesses,
    IReadOnlyList<string> AvailableStatuses);
