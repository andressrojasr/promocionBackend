namespace PromocionBackend.Application.Abstractions;

/// <summary>
/// Usuario autenticado de la petición actual, extraído de los claims del JWT propio.
/// </summary>
public interface ICurrentUserService
{
    Guid UserId { get; }
    string Email { get; }
    string Role { get; }
    string? TeacherId { get; }
}
