using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.Commissions;
using PromocionBackend.Application.Services;
using PromocionBackend.Domain.Constants;

namespace PromocionBackend.Api.Controllers;

[ApiController]
[Route("api/v1/commissions")]
[Authorize(Roles = $"{Roles.Cp},{Roles.Ca},{Roles.Admin}")]
public class CommissionsController(CommissionService commissionService, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Lista las comisiones (principal primero) de un proceso para el tipo indicado (cp/ca).</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CommissionDto>>>> List(
        [FromQuery] Guid processId, [FromQuery] string type, CancellationToken cancellationToken)
    {
        var commissions = await commissionService.ListAsync(processId, type, currentUser, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CommissionDto>>.Ok(commissions));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CommissionDto>>> GetDetail(Guid id, CancellationToken cancellationToken)
    {
        var commission = await commissionService.GetDetailAsync(id, cancellationToken);
        return Ok(ApiResponse<CommissionDto>.Ok(commission));
    }

    /// <summary>
    /// Crea una comisión para un día concreto (con delegados) o marca una nueva comisión
    /// principal. Se usa para dejar constancia de quién aprobó/rechazó cada día.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CommissionDto>>> Create(
        [FromBody] CreateCommissionRequest request, CancellationToken cancellationToken)
    {
        var commission = await commissionService.CreateAsync(request, currentUser, cancellationToken);
        return CreatedAtAction(
            nameof(GetDetail),
            new { id = commission.Id },
            ApiResponse<CommissionDto>.Ok(commission, "Comisión registrada correctamente"));
    }
}
