using System.ComponentModel.DataAnnotations;
using PromocionBackend.Application.DTOs.Eligibility;

namespace PromocionBackend.Application.DTOs.Applications;

public class ApplicationItemRequest
{
    [Required]
    public string ItemType { get; set; } = string.Empty;

    [Required]
    public string ExternalItemId { get; set; } = string.Empty;
}

public class SubmitApplicationRequest
{
    [Required]
    public Guid ProcessId { get; set; }

    public List<ApplicationItemRequest> Items { get; set; } = [];
}

public class ReviewRequest
{
    [Required(ErrorMessage = "La decisión es obligatoria.")]
    [RegularExpression("approved|rejected", ErrorMessage = "La decisión debe ser 'approved' o 'rejected'.")]
    public string Decision { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Feedback { get; set; }
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
    string? DocumentUrl);

public record ReviewDto(
    string Stage,
    string ReviewerName,
    string ReviewerRole,
    string Decision,
    string? Feedback,
    DateTime CreatedAt);

public record AppealDto(string Justification, DateTime SubmittedAt);

public record ApplicationSummaryDto(
    Guid Id,
    Guid ProcessId,
    string ProcessName,
    Guid TeacherUserId,
    string TeacherName,
    string? TeacherId,
    string FromPosition,
    string ToPosition,
    string FromLabel,
    string ToLabel,
    string Status,
    DateTime SubmittedAt,
    DateTime? AppealDeadline);

public record ApplicationDetailDto(
    ApplicationSummaryDto Summary,
    IReadOnlyList<ApplicationItemDto> Items,
    IReadOnlyList<ReviewDto> Reviews,
    AppealDto? Appeal,
    EligibilityDto? Eligibility,
    bool CanAppeal);
