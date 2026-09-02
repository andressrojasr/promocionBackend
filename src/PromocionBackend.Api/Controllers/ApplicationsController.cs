using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromocionBackend.Api.Services;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.Applications;
using PromocionBackend.Application.Services;
using PromocionBackend.Domain.Constants;

namespace PromocionBackend.Api.Controllers;

[ApiController]
[Route("api/v1/applications")]
[Authorize]
public class ApplicationsController(
    ApplicationService applicationService,
    ICurrentUserService currentUser,
    ApplicationNotificationService notificationService) : ControllerBase
{
    /// <summary>
    /// Envía una postulación. El servidor vuelve a validar la elegibilidad y congela
    /// la hoja de vida y el resultado del dashboard al momento del envío.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Roles.Teacher)]
    public async Task<ActionResult<ApiResponse<ApplicationDetailDto>>> Submit(
        [FromBody] SubmitApplicationRequest request,
        [FromHeader(Name = "X-External-Token")] string externalAccessToken,
        CancellationToken cancellationToken)
    {
        var application = await applicationService.SubmitAsync(request, currentUser, externalAccessToken, cancellationToken);
        await notificationService.NotifyApplicationUpdatedAsync(application.Summary);
        return CreatedAtAction(
            nameof(GetDetail),
            new { id = application.Summary.Id },
            ApiResponse<ApplicationDetailDto>.Ok(application, "Postulación enviada correctamente"));
    }

    /// <summary>Listado de postulaciones según el rol: el docente ve las suyas; TH/CP/Admin todas; CA las apeladas.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ApplicationSummaryDto>>>> List(
        [FromQuery] string? status, [FromQuery] Guid? processId, [FromQuery] string? teacherId, CancellationToken cancellationToken)
    {
        // Validar estado si se proporciona
        if (!string.IsNullOrWhiteSpace(status) && !ApplicationStatusValidator.IsValidStatus(status))
        {
            return BadRequest(ApiResponse<IReadOnlyList<ApplicationSummaryDto>>.Fail(
                ApplicationStatusValidator.GetValidationErrorMessage(status)));
        }

        var applications = await applicationService.ListAsync(currentUser, status, processId, teacherId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ApplicationSummaryDto>>.Ok(applications));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ApplicationDetailDto>>> GetDetail(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var application = await applicationService.GetDetailAsync(id, currentUser, cancellationToken);
        return Ok(ApiResponse<ApplicationDetailDto>.Ok(application));
    }

    /// <summary>
    /// Registra la decisión de revisión de la etapa correspondiente al rol del usuario:
    /// TH sobre postulaciones enviadas, CP sobre aprobadas por TH y CA sobre apelaciones.
    /// </summary>
    [HttpPost("{id:guid}/review")]
    [Authorize(Roles = $"{Roles.Th},{Roles.Cp},{Roles.Ca}")]
    public async Task<ActionResult<ApiResponse<ApplicationDetailDto>>> Review(
        Guid id, [FromBody] ReviewRequest request, CancellationToken cancellationToken)
    {
        var application = await applicationService.ReviewAsync(id, request, currentUser, cancellationToken);
        await notificationService.NotifyApplicationUpdatedAsync(application.Summary);
        return Ok(ApiResponse<ApplicationDetailDto>.Ok(application, "Revisión registrada correctamente"));
    }

    /// <summary>Apela un rechazo de la Comisión de Promoción dentro del plazo de 3 días.</summary>
    [HttpPost("{id:guid}/appeal")]
    [Authorize(Roles = Roles.Teacher)]
    public async Task<ActionResult<ApiResponse<ApplicationDetailDto>>> Appeal(
        Guid id, [FromBody] AppealRequest request, CancellationToken cancellationToken)
    {
        var application = await applicationService.AppealAsync(id, request, currentUser, cancellationToken);
        return Ok(ApiResponse<ApplicationDetailDto>.Ok(application, "Apelación presentada correctamente"));
    }
}
