using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Common;

namespace PromocionBackend.Api.Services;

/// <summary>
/// Extrae la identidad del usuario autenticado desde los claims del JWT propio.
/// </summary>
public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid UserId =>
        Guid.TryParse(FindClaim("sub"), out var id)
            ? id
            : throw AppException.Unauthorized("El token no contiene un identificador de usuario válido.");

    public string Email => FindClaim("email") ?? string.Empty;

    public string Role => FindClaim("role") ?? string.Empty;

    public string? TeacherId => FindClaim("teacherId");

    private string? FindClaim(string claimType) =>
        httpContextAccessor.HttpContext?.User.FindFirst(claimType)?.Value;
}
