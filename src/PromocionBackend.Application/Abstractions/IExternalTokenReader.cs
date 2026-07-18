namespace PromocionBackend.Application.Abstractions;

/// <summary>
/// Lector de claims del token externo emitido por el sistema de autenticación de la UTA.
/// No valida la firma: la validación se delega al propio sistema externo al consumir
/// sus servicios con ese token (si el token es inválido, el sistema externo responde 401).
/// </summary>
public interface IExternalTokenReader
{
    /// <summary>Extrae el correo del token, o null si el token no es legible.</summary>
    string? GetEmail(string externalAccessToken);
}
