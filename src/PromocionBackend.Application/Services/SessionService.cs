using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.Auth;
using PromocionBackend.Application.Mapping;
using PromocionBackend.Domain.Constants;
using PromocionBackend.Domain.Entities;

namespace PromocionBackend.Application.Services;

/// <summary>
/// Intercambio de sesión: recibe el token externo de la UTA, obtiene la cédula y la
/// hoja de vida del docente desde RRHH, sincroniza el usuario y su snapshot en la
/// base de datos, y emite el JWT propio de la aplicación.
/// </summary>
public class SessionService(
    IAppDbContext db,
    IHrApiClient hrApi,
    IExternalTokenReader tokenReader,
    IAppJwtIssuer jwtIssuer,
    IOptions<RoleSeedOptions> roleSeeds)
{
    public async Task<SessionResponse> ExchangeAsync(string externalAccessToken, CancellationToken cancellationToken = default)
    {
        var email = tokenReader.GetEmail(externalAccessToken)
            ?? throw AppException.Unauthorized("El token externo no es válido o no contiene el correo del usuario.");

        // La llamada a RRHH con el token externo delega su validación al sistema de la UTA:
        // un token inválido o expirado produce 401 y el intercambio se rechaza.
        // Si el email no existe en RRHH, usa StaticCedula como fallback.
        var identification = await hrApi.GetIdentificationByEmailAsync(email, externalAccessToken, cancellationToken);

        // Obtener datos completos del docente, incluyendo nombre real
        // Si falla (ej: token sin autorización), usa nombre derivado del email
        string fullName = DisplayNameFromEmail(email);
        try
        {
            var teacherDetails = await hrApi.GetTeacherDetailsAsync(identification, externalAccessToken, cancellationToken);
            if (teacherDetails?.FullName != null)
            {
                fullName = teacherDetails.FullName;
            }
        }
        catch
        {
            // Si no puede obtener datos del docente, continúa con nombre derivado del email
        }

        var utcNow = DateTime.UtcNow;
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Role = ResolveInitialRole(email),
                FullName = fullName,
                IsActive = true,
                CreatedAt = utcNow
            };
            db.Users.Add(user);
        }
        else
        {
            // Actualizar nombre si el usuario ya existe
            user.FullName = fullName;
        }

        if (!user.IsActive)
        {
            throw AppException.Forbidden("Su cuenta está desactivada. Contacte al administrador del sistema.");
        }

        user.Identification = identification;
        user.LastLoginAt = utcNow;
        user.UpdatedAt = utcNow;

        await db.SaveChangesAsync(cancellationToken);

        var accessToken = jwtIssuer.IssueToken(user);

        return new SessionResponse(accessToken, BuildUserDto(user));
    }

    public async Task<SessionUserDto> GetMeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw AppException.NotFound("Usuario no encontrado.");

        return BuildUserDto(user);
    }


    private string ResolveInitialRole(string email) =>
        roleSeeds.Value.Emails.TryGetValue(email, out var seededRole) && Roles.IsValid(seededRole)
            ? seededRole
            : Roles.Teacher;

    private static SessionUserDto BuildUserDto(User user) => new(
        user.Id,
        user.Email,
        user.FullName,
        user.Role,
        user.TeacherId,
        user.Identification,
        null);

    /// <summary>Nombre legible derivado del correo, usado para usuarios sin hoja de vida (personal administrativo).</summary>
    private static string DisplayNameFromEmail(string email)
    {
        var localPart = email.Split('@')[0];
        var parts = localPart.Split(['.', '_', '-'], StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts.Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
    }
}
