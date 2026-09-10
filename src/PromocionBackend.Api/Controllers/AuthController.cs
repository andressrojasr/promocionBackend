using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.Auth;
using PromocionBackend.Application.Services;
using PromocionBackend.Infrastructure.Configuration;

namespace PromocionBackend.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(SessionService sessionService, ICurrentUserService currentUser, IOptions<DataSourceSettings> dataSourceOptions) : ControllerBase
{
    private readonly DataSourceSettings _dataSourceSettings = dataSourceOptions.Value;

    /// <summary>
    /// Configuración de autenticación: indica dónde debe el frontend hacer login.
    /// </summary>
    [HttpGet("config")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<object>> GetAuthConfig()
    {
        var authApiUrl = _dataSourceSettings.UseRealServices
            ? "https://serviciospruebas.uta.edu.ec/WsSeguUta"
            : "http://localhost:5031";

        return Ok(ApiResponse<object>.Ok(new { authApiUrl }, "Configuración de autenticación"));
    }

    /// <summary>
    /// Metadata: información sobre la estructura de URLs de autenticación.
    /// </summary>
    [HttpGet("login-endpoint")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<object>> GetLoginEndpoint()
    {
        var loginEndpoint = _dataSourceSettings.UseRealServices
            ? "api/auth/login"
            : "api/v1/auth/login";

        return Ok(ApiResponse<object>.Ok(new { loginEndpoint }, "Estructura de endpoint de login"));
    }

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
