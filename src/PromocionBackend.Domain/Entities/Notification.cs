namespace PromocionBackend.Domain.Entities;

/// <summary>
/// Notificación in-app dirigida a un usuario (por ejemplo, el resultado de una revisión).
/// </summary>
public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
