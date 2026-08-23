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
[Authorize(Roles = Roles.Teacher)]
public class TeachersController(
    TeacherProfileService teacherProfileService,
    ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Hoja de vida del docente autenticado (datos frescos de RRHH).</summary>
    [HttpGet("me/profile")]
    public async Task<ActionResult<ApiResponse<TeacherProfileDto>>> GetMyProfile(
        [FromHeader(Name = "X-External-Token")] string externalAccessToken,
        CancellationToken cancellationToken)
    {
        var profile = await teacherProfileService.GetMyProfileAsync(currentUser.UserId, externalAccessToken, cancellationToken);
        return Ok(ApiResponse<TeacherProfileDto>.Ok(profile));
    }
}
