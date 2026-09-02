namespace PromocionBackend.Application.DTOs.Notifications;

public record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    bool IsRead,
    string CreatedAt);

public record NotificationListDto(IReadOnlyList<NotificationDto> Items, int UnreadCount);
