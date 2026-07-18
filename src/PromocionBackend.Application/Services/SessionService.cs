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
        var identification = await hrApi.GetIdentificationByEmailAsync(email, externalAccessToken, cancellationToken);

        var utcNow = DateTime.UtcNow;
        var user = await db.Users
            .Include(u => u.Snapshot)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Role = ResolveInitialRole(email),
                FullName = DisplayNameFromEmail(email),
                IsActive = true,
                CreatedAt = utcNow
            };
            db.Users.Add(user);
        }

        if (!user.IsActive)
        {
            throw AppException.Forbidden("Su cuenta está desactivada. Contacte al administrador del sistema.");
        }

        user.Identification = identification;
        user.LastLoginAt = utcNow;
        user.UpdatedAt = utcNow;

        if (user.Role == Roles.Teacher)
        {
            await RefreshTeacherSnapshotAsync(user, identification, externalAccessToken, utcNow, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        var accessToken = jwtIssuer.IssueToken(user);

        return new SessionResponse(accessToken, BuildUserDto(user));
    }

    public async Task<SessionUserDto> GetMeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .Include(u => u.Snapshot)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw AppException.NotFound("Usuario no encontrado.");

        return BuildUserDto(user);
    }

    private async Task RefreshTeacherSnapshotAsync(User user, string identification, string externalAccessToken, DateTime utcNow, CancellationToken cancellationToken)
    {
        var details = await hrApi.GetTeacherDetailsAsync(identification, externalAccessToken, cancellationToken);

        user.TeacherId = details.TeacherId;
        user.FullName = details.FullName;

        var positionStart = TeacherProfileMapper.ParseDate(details.CurrentPositionStartDate)
            ?? throw AppException.UpstreamUnavailable("La hoja de vida recibida no tiene una fecha de inicio de cargo válida.");

        var snapshotJson = JsonSerializer.Serialize(details, AppJson.Options);

        if (user.Snapshot is null)
        {
            user.Snapshot = new TeacherSnapshot
            {
                Id = Guid.NewGuid(),
                UserId = user.Id
            };
            db.TeacherSnapshots.Add(user.Snapshot);
        }

        user.Snapshot.CurrentPosition = details.CurrentPosition;
        user.Snapshot.CurrentPositionStartDate = positionStart;
        user.Snapshot.SnapshotJson = snapshotJson;
        user.Snapshot.CapturedAt = utcNow;
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
        user.Snapshot?.CurrentPosition);

    /// <summary>Nombre legible derivado del correo, usado para usuarios sin hoja de vida (personal administrativo).</summary>
    private static string DisplayNameFromEmail(string email)
    {
        var localPart = email.Split('@')[0];
        var parts = localPart.Split(['.', '_', '-'], StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts.Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
    }
}
