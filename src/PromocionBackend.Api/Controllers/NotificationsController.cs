using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.Notifications;
using PromocionBackend.Application.Services;

namespace PromocionBackend.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController(
    NotificationService notificationService,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<NotificationListDto>>> List(
        [FromQuery] bool unreadOnly, CancellationToken cancellationToken)
    {
        var notifications = await notificationService.ListAsync(currentUser.UserId, unreadOnly, cancellationToken);
        return Ok(ApiResponse<NotificationListDto>.Ok(notifications));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await notificationService.MarkReadAsync(id, currentUser.UserId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Notificación marcada como leída"));
    }

    [HttpPost("read-all")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllRead(CancellationToken cancellationToken)
    {
        await notificationService.MarkAllReadAsync(currentUser.UserId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Todas las notificaciones fueron marcadas como leídas"));
    }
}
