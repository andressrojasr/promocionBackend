using Microsoft.IdentityModel.JsonWebTokens;
using PromocionBackend.Application.Abstractions;

namespace PromocionBackend.Infrastructure.Auth;

/// <summary>
/// Lee los claims del token externo de la UTA sin validar la firma. La validación
/// real la realiza el sistema externo cuando se consumen sus servicios con el token.
/// </summary>
public class ExternalTokenReader : IExternalTokenReader
{
    private const string MsNameClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";

    private readonly JsonWebTokenHandler _handler = new();

    public string? GetEmail(string externalAccessToken)
    {
        if (string.IsNullOrWhiteSpace(externalAccessToken) || !_handler.CanReadToken(externalAccessToken))
        {
            return null;
        }

        try
        {
            var token = _handler.ReadJsonWebToken(externalAccessToken);

            if (token.TryGetPayloadValue<string>("email", out var email) && !string.IsNullOrWhiteSpace(email))
            {
                return email;
            }

            // El servicio de la UTA también emite el correo en el claim de nombre del esquema de Microsoft.
            if (token.TryGetPayloadValue<string>(MsNameClaim, out var name) && name.Contains('@'))
            {
                return name;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
