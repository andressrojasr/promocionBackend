using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.Eligibility;
using PromocionBackend.Application.DTOs.Processes;
using PromocionBackend.Application.Services;
using PromocionBackend.Domain.Constants;

namespace PromocionBackend.Api.Controllers;

[ApiController]
[Route("api/v1/processes")]
[Authorize]
public class ProcessesController(
    ProcessService processService,
    EligibilityService eligibilityService,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProcessSummaryDto>>>> List(CancellationToken cancellationToken)
    {
        var processes = await processService.ListAsync(currentUser, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ProcessSummaryDto>>.Ok(processes));
    }

    /// <summary>Valores por defecto del reglamento para pre-llenar el formulario de creación.</summary>
    [HttpGet("requirement-defaults")]
    [Authorize(Roles = Roles.Cp)]
    public ActionResult<ApiResponse<IReadOnlyList<RequirementConfigDto>>> GetDefaults()
    {
        return Ok(ApiResponse<IReadOnlyList<RequirementConfigDto>>.Ok(processService.GetDefaults()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProcessDetailDto>>> GetDetail(Guid id, CancellationToken cancellationToken)
    {
        var process = await processService.GetDetailAsync(id, currentUser, cancellationToken);
        return Ok(ApiResponse<ProcessDetailDto>.Ok(process));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Cp)]
    public async Task<ActionResult<ApiResponse<ProcessDetailDto>>> Create(
        [FromBody] CreateProcessRequest request, CancellationToken cancellationToken)
    {
        var process = await processService.CreateAsync(request, currentUser.UserId, cancellationToken);
        return CreatedAtAction(
            nameof(GetDetail),
            new { id = process.Summary.Id },
            ApiResponse<ProcessDetailDto>.Ok(process, "Proceso de promoción creado correctamente"));
    }

    /// <summary>
    /// Dashboard de elegibilidad del docente autenticado frente a este proceso:
    /// requisitos exigidos, valores alcanzados y si puede postular.
    /// </summary>
    [HttpGet("{id:guid}/eligibility")]
    [Authorize(Roles = Roles.Teacher)]
    public async Task<ActionResult<ApiResponse<EligibilityDto>>> GetEligibility(
        Guid id,
        [FromHeader(Name = "X-External-Token")] string externalAccessToken,
        CancellationToken cancellationToken)
    {
        var eligibility = await eligibilityService.EvaluateForProcessAsync(id, currentUser.UserId, externalAccessToken, cancellationToken);
        return Ok(ApiResponse<EligibilityDto>.Ok(eligibility));
    }
}
