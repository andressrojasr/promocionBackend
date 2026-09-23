using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.Teachers;
using PromocionBackend.Application.Services;
using PromocionBackend.Domain.Constants;

namespace PromocionBackend.Api.Controllers;

[ApiController]
[Route("api/v1/teachers")]
[Authorize]
public class TeachersController(
    TeacherDirectoryService teacherDirectoryService,
    TeacherProfileService teacherProfileService,
    ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Busca docentes/autoridades por nombre o cédula, para integrar comisiones.</summary>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Cp},{Roles.Ca},{Roles.Admin}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TeacherSummaryDto>>>> Search(
        [FromQuery] string? query,
        [FromHeader(Name = "X-External-Token")] string externalAccessToken,
        CancellationToken cancellationToken)
    {
        var teachers = await teacherDirectoryService.SearchAsync(query, externalAccessToken, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TeacherSummaryDto>>.Ok(teachers));
    }

    /// <summary>Hoja de vida del docente autenticado (datos frescos de RRHH), para el perfil y la postulación.</summary>
    [HttpGet("me/profile")]
    public async Task<ActionResult<ApiResponse<TeacherProfileDto>>> GetMyProfile(
        [FromHeader(Name = "X-External-Token")] string externalAccessToken,
        CancellationToken cancellationToken)
    {
        var profile = await teacherProfileService.GetMyProfileAsync(currentUser.UserId, externalAccessToken, cancellationToken);
        return Ok(ApiResponse<TeacherProfileDto>.Ok(profile));
    }
}
