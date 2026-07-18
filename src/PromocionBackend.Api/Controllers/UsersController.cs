using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.Users;
using PromocionBackend.Application.Services;
using PromocionBackend.Domain.Constants;

namespace PromocionBackend.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = Roles.Admin)]
public class UsersController(UserAdminService userAdminService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserDto>>>> List(
        [FromQuery] string? search, [FromQuery] string? role, CancellationToken cancellationToken)
    {
        var users = await userAdminService.ListAsync(search, role, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<UserDto>>.Ok(users));
    }

    [HttpPatch("{id:guid}/role")]
    public async Task<ActionResult<ApiResponse<UserDto>>> ChangeRole(
        Guid id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var user = await userAdminService.ChangeRoleAsync(id, request.Role, cancellationToken);
        return Ok(ApiResponse<UserDto>.Ok(user, "Rol actualizado correctamente"));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<UserDto>>> ChangeStatus(
        Guid id, [FromBody] UpdateStatusRequest request, CancellationToken cancellationToken)
    {
        var user = await userAdminService.SetActiveAsync(id, request.IsActive, cancellationToken);
        var message = request.IsActive ? "Usuario activado correctamente" : "Usuario desactivado correctamente";
        return Ok(ApiResponse<UserDto>.Ok(user, message));
    }
}
