using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.Dashboard;
using PromocionBackend.Application.Services;
using PromocionBackend.Domain.Constants;

namespace PromocionBackend.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public class DashboardController(
    DashboardService dashboardService,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetStats(CancellationToken cancellationToken)
    {
        var stats = await dashboardService.GetStatsAsync(currentUser, cancellationToken);
        return Ok(ApiResponse<DashboardStatsDto>.Ok(stats));
    }

    [HttpGet("cp/data")]
    [Authorize(Roles = Roles.Cp)]
    public async Task<ActionResult<ApiResponse<CpDashboardDataDto>>> GetCpDashboard(
        [FromQuery] string? status = null,
        [FromQuery] string? processId = null,
        [FromQuery] string? teacherId = null,
        CancellationToken cancellationToken = default)
    {
        // Validar estado si se proporciona
        if (!string.IsNullOrWhiteSpace(status) && !ApplicationStatusValidator.IsValidStatus(status))
        {
            return BadRequest(ApiResponse<CpDashboardDataDto>.Fail(
                ApplicationStatusValidator.GetValidationErrorMessage(status)));
        }

        var data = await dashboardService.GetCpDashboardAsync(status, processId, teacherId, cancellationToken);
        return Ok(ApiResponse<CpDashboardDataDto>.Ok(data));
    }
}
