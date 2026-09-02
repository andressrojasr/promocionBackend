using Microsoft.EntityFrameworkCore;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.Notifications;
using PromocionBackend.Domain.Entities;

namespace PromocionBackend.Application.Services;

/// <summary>
/// Notificaciones in-app. Los métodos de creación solo agregan entidades al contexto;
/// el servicio que orquesta la operación es responsable de guardar los cambios, de modo
/// que la notificación quede en la misma transacción que la acción que la origina.
/// </summary>
public class NotificationService(IAppDbContext db)
{
    public async Task<NotificationListDto> ListAsync(Guid userId, bool unreadOnly, CancellationToken cancellationToken = default)
    {
        var query = db.Notifications.Where(n => n.UserId == userId);

        var unreadCount = await query.CountAsync(n => !n.IsRead, cancellationToken);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        var items = notifications
            .Select(n => new NotificationDto(n.Id, n.Title, n.Message, n.IsRead, TimeHelper.FormatToEcuadorString(n.CreatedAt)))
            .ToList();

        return new NotificationListDto(items, unreadCount);
    }

    public async Task MarkReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken)
            ?? throw AppException.NotFound("Notificación no encontrada.");

        notification.IsRead = true;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);
    }

    /// <summary>Agrega una notificación para un usuario (sin guardar cambios).</summary>
    public void Notify(Guid userId, string title, string message)
    {
        db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
    }

    /// <summary>Agrega una notificación para todos los usuarios activos de un rol (sin guardar cambios).</summary>
    public async Task NotifyRoleAsync(string role, string title, string message, CancellationToken cancellationToken = default)
    {
        var userIds = await db.Users
            .Where(u => u.Role == role && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        foreach (var userId in userIds)
        {
            Notify(userId, title, message);
        }
    }
}
