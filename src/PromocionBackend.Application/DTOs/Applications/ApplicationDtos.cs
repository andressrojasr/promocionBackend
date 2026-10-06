using System.ComponentModel.DataAnnotations;
using PromocionBackend.Application.DTOs.Eligibility;

namespace PromocionBackend.Application.DTOs.Applications;

public class ApplicationItemRequest
{
    [Required]
    public string ItemType { get; set; } = string.Empty;

    [Required]
    public string ExternalItemId { get; set; } = string.Empty;

    public DateTime? DocumentDateOriginal { get; set; }
}

public class SubmitApplicationRequest
{
    [Required]
    public Guid ProcessId { get; set; }

    public List<ApplicationItemRequest> Items { get; set; } = [];

    /// <summary>El docente confirmó haber leído y aceptado las consideraciones previas al envío.</summary>
    public bool AcceptedTerms { get; set; }
}

public class ReviewRequest
{
    [Required(ErrorMessage = "La decisión es obligatoria.")]
    [RegularExpression("approved|rejected", ErrorMessage = "La decisión debe ser 'approved' o 'rejected'.")]
    public string Decision { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Feedback { get; set; }

    /// <summary>Sesión de revisión (proceso+comisión+facultad) bajo la que se decide. Obligatoria para CP/CA; no aplica a TH.</summary>
    public Guid? ReviewSessionId { get; set; }
}

public class AppealRequest
{
    [Required(ErrorMessage = "La justificación de la apelación es obligatoria.")]
    [MaxLength(2000)]
    public string Justification { get; set; } = string.Empty;
}

public record ApplicationItemDto(
    string ItemType,
    string ExternalItemId,
    string Title,
    string? DocumentUrl,
    DateTime? DocumentDateOriginal);

public record ReviewDto(
    string Stage,
    string ReviewerName,
    string ReviewerRole,
    string Decision,
    string? Feedback,
    string CreatedAt);

public record AppealDto(string Justification, string SubmittedAt);

public record ApplicationSummaryDto(
    Guid Id,
    Guid ProcessId,
    string ProcessName,
    Guid TeacherUserId,
    string TeacherName,
    string? TeacherIdentification,
    string FromPosition,
    string ToPosition,
    string FromLabel,
    string ToLabel,
    string Status,
    string SubmittedAt,
    string? AppealDeadline,
    decimal? ScorePct,
    int? DaysToDecision,
    string? CurrentReviewerName,
    string? FacultyId,
    string? FacultyName,
    /// <summary>Sesión de revisión (proceso+comisión+facultad) de la decisión CP/CA más reciente, si existe.</summary>
    Guid? ReviewSessionId);

public record ReviewLockInfoDto(
    string? LockedByName,
    DateTime? LockedAt,
    DateTime? ExpiresAt);

public record ApplicationDetailDto(
    ApplicationSummaryDto Summary,
    IReadOnlyList<ApplicationItemDto> Items,
    IReadOnlyList<ReviewDto> Reviews,
    AppealDto? Appeal,
    EligibilityDto? Eligibility,
    bool CanAppeal,
    ReviewLockInfoDto? ReviewLock);
