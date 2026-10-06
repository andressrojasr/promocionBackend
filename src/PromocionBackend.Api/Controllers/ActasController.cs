using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.Actas;
using PromocionBackend.Application.Services;
using PromocionBackend.Domain.Constants;

namespace PromocionBackend.Api.Controllers;

[ApiController]
[Route("api/v1/actas")]
[Authorize(Roles = Roles.Cp)]
public class ActasController(ActaService actaService) : ControllerBase
{
    /// <summary>Categorías (transiciones) con decisiones en una sesión cerrada; se genera un acta por categoría.</summary>
    [HttpGet("cp/by-session/categories")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ActaCategoryDto>>>> GetCategories(
        [FromQuery] Guid reviewSessionId,
        CancellationToken cancellationToken)
    {
        var categories = await actaService.GetCpActaCategoriesBySessionAsync(reviewSessionId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ActaCategoryDto>>.Ok(categories));
    }

    /// <summary>Genera el acta (PDF) de una sesión de revisión cerrada para una categoría.</summary>
    [HttpGet("cp/by-session")]
    public async Task<IActionResult> GenerateCpActaBySession(
        [FromQuery] Guid reviewSessionId,
        [FromQuery] string? fromPosition,
        [FromQuery] string? toPosition,
        CancellationToken cancellationToken)
    {
        var (pdfBytes, _, isProvisional) = await actaService.GenerateCpActaBySessionAsync(reviewSessionId, fromPosition, toPosition, cancellationToken);
        var suffix = isProvisional ? "-provisional" : string.Empty;
        return File(pdfBytes, "application/pdf", $"acta-promocion{suffix}-{DateTime.UtcNow:yyyyMMdd-HHmm}.pdf");
    }
}
