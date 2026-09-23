using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.Faculties;
using PromocionBackend.Application.Services;

namespace PromocionBackend.Api.Controllers;

[ApiController]
[Route("api/v1/faculties")]
[Authorize]
public class FacultiesController(FacultyService facultyService) : ControllerBase
{
    /// <summary>Catálogo de facultades, usado para filtrar postulaciones e integrar comisiones.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FacultyDto>>>> List(
        [FromHeader(Name = "X-External-Token")] string externalAccessToken,
        CancellationToken cancellationToken)
    {
        var faculties = await facultyService.ListAsync(externalAccessToken, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<FacultyDto>>.Ok(faculties));
    }
}
