using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.Dashboard;
using PromocionBackend.Application.Services;

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
}
