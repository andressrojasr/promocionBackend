using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.Auth;
using PromocionBackend.Application.Services;

namespace PromocionBackend.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(SessionService sessionService, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>
    /// Intercambio de sesión: recibe el token externo de la UTA, sincroniza el usuario
    /// y su hoja de vida, y devuelve el JWT propio de la aplicación.
    /// </summary>
    [HttpPost("session")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<SessionResponse>>> ExchangeSession(
        [FromBody] SessionRequest request, CancellationToken cancellationToken)
    {
        var session = await sessionService.ExchangeAsync(request.ExternalAccessToken, cancellationToken);
        return Ok(ApiResponse<SessionResponse>.Ok(session, "Sesión iniciada correctamente"));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<SessionUserDto>>> GetMe(CancellationToken cancellationToken)
    {
        var user = await sessionService.GetMeAsync(currentUser.UserId, cancellationToken);
        return Ok(ApiResponse<SessionUserDto>.Ok(user));
    }
}
