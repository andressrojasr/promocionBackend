using PromocionBackend.Domain.Entities;

namespace PromocionBackend.Application.Abstractions;

/// <summary>
/// Emisor del JWT propio de la aplicación (HS256), con los claims de rol y usuario.
/// </summary>
public interface IAppJwtIssuer
{
    string IssueToken(User user);
}
