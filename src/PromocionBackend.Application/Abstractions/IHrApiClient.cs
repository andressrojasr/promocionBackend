using PromocionBackend.Application.Abstractions.External;

namespace PromocionBackend.Application.Abstractions;

/// <summary>
/// Cliente de los servicios de RRHH de la UTA (backend simulado). Todas las
/// operaciones usan el token externo del usuario como Bearer, de modo que la
/// validez del token la verifica el propio sistema externo.
/// </summary>
public interface IHrApiClient
{
    /// <summary>Obtiene la cédula/identificación del empleado a partir de su correo institucional.</summary>
    Task<string> GetIdentificationByEmailAsync(string email, string externalAccessToken, CancellationToken cancellationToken = default);

    /// <summary>Obtiene la hoja de vida completa del docente a partir de su identificación.</summary>
    Task<HrTeacherDetails> GetTeacherDetailsAsync(string identification, string externalAccessToken, CancellationToken cancellationToken = default);
}
