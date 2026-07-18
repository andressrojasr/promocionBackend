using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Domain.Entities;

namespace PromocionBackend.Infrastructure.Auth;

/// <summary>
/// Emite el JWT propio de la aplicación (HS256) con los claims del usuario y su rol.
/// </summary>
public class AppJwtIssuer(IOptions<JwtOptions> options) : IAppJwtIssuer
{
    private readonly JwtOptions _options = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public string IssueToken(User user)
    {
        var claims = new Dictionary<string, object>
        {
            ["sub"] = user.Id.ToString(),
            ["email"] = user.Email,
            ["name"] = user.FullName,
            ["role"] = user.Role
        };

        if (!string.IsNullOrEmpty(user.TeacherId))
        {
            claims["teacherId"] = user.TeacherId;
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes),
            Claims = claims,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)),
                SecurityAlgorithms.HmacSha256)
        };

        return _handler.CreateToken(descriptor);
    }
}
