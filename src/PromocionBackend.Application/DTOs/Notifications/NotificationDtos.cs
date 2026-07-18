namespace PromocionBackend.Application.DTOs.Notifications;

public record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    bool IsRead,
    DateTime CreatedAt);

public record NotificationListDto(IReadOnlyList<NotificationDto> Items, int UnreadCount);
