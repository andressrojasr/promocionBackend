using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.ReviewSessions;
using PromocionBackend.Application.Services;
using PromocionBackend.Domain.Constants;

namespace PromocionBackend.Api.Controllers;

[ApiController]
[Route("api/v1/review-sessions")]
[Authorize(Roles = $"{Roles.Cp},{Roles.Ca},{Roles.Admin}")]
public class ReviewSessionsController(ReviewSessionService reviewSessionService, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Inicia una sesión de revisión: proceso + comisión + facultad.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ReviewSessionDto>>> Create(
        [FromBody] CreateReviewSessionRequest request, CancellationToken cancellationToken)
    {
        var session = await reviewSessionService.CreateAsync(request, currentUser, cancellationToken);
        return CreatedAtAction(
            nameof(GetDetail),
            new { id = session.Id },
            ApiResponse<ReviewSessionDto>.Ok(session, "Sesión de revisión iniciada correctamente"));
    }

    /// <summary>Busca sesiones de revisión pasadas (proceso, tipo, facultad, rango de fechas, activas/cerradas).</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReviewSessionDto>>>> List(
        [FromQuery] Guid? processId,
        [FromQuery] string? type,
        [FromQuery] string? facultyId,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        [FromQuery] bool? closed,
        CancellationToken cancellationToken)
    {
        var sessions = await reviewSessionService.ListAsync(currentUser, processId, type, facultyId, dateFrom, dateTo, closed, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ReviewSessionDto>>.Ok(sessions));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ReviewSessionDto>>> GetDetail(Guid id, CancellationToken cancellationToken)
    {
        var session = await reviewSessionService.GetDetailAsync(id, cancellationToken);
        return Ok(ApiResponse<ReviewSessionDto>.Ok(session));
    }

    /// <summary>Cierra una sesión: ya no podrá usarse para decidir postulaciones ni reanudarse.</summary>
    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<ApiResponse<ReviewSessionDto>>> Close(Guid id, CancellationToken cancellationToken)
    {
        var session = await reviewSessionService.CloseAsync(id, currentUser, cancellationToken);
        return Ok(ApiResponse<ReviewSessionDto>.Ok(session, "Sesión de revisión cerrada"));
    }
}
