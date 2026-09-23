using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromocionBackend.Application.Services;
using PromocionBackend.Domain.Constants;

namespace PromocionBackend.Api.Controllers;

[ApiController]
[Route("api/v1/actas")]
[Authorize(Roles = Roles.Cp)]
public class ActasController(ActaService actaService) : ControllerBase
{
    /// <summary>
    /// Genera el acta de promoción (PDF) de una comisión de CP para una facultad determinada,
    /// con los docentes aprobados y no aprobados en esa sesión.
    /// </summary>
    [HttpGet("cp")]
    public async Task<IActionResult> GenerateCpActa(
        [FromQuery] Guid commissionId,
        [FromQuery] string facultyId,
        [FromQuery] string facultyName,
        CancellationToken cancellationToken)
    {
        var pdfBytes = await actaService.GenerateCpActaAsync(commissionId, facultyId, facultyName, cancellationToken);
        return File(pdfBytes, "application/pdf", $"acta-promocion-{DateTime.UtcNow:yyyyMMdd-HHmm}.pdf");
    }

    /// <summary>Genera el acta a partir de una sesión de revisión ya registrada.</summary>
    [HttpGet("cp/by-session")]
    public async Task<IActionResult> GenerateCpActaBySession(
        [FromQuery] Guid reviewSessionId,
        CancellationToken cancellationToken)
    {
        var pdfBytes = await actaService.GenerateCpActaBySessionAsync(reviewSessionId, cancellationToken);
        return File(pdfBytes, "application/pdf", $"acta-promocion-{DateTime.UtcNow:yyyyMMdd-HHmm}.pdf");
    }
}
