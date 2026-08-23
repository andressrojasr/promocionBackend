using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Abstractions.External;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.Teachers;
using PromocionBackend.Application.Mapping;
using PromocionBackend.Domain.Services;

namespace PromocionBackend.Application.Services;

/// <summary>
/// Expone la hoja de vida del docente almacenada en su snapshot, para la página
/// de perfil y la selección de documentos al postular.
/// </summary>
public class TeacherProfileService(
    IAppDbContext db,
    IHrApiClient hrApi)
{
    public async Task<TeacherProfileDto> GetMyProfileAsync(Guid userId, string externalAccessToken, CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw AppException.NotFound("Usuario no encontrado.");

        var identification = user.Identification
            ?? throw AppException.Unauthorized("El usuario no tiene identificación registrada.");

        var details = await hrApi.GetTeacherDetailsAsync(identification, externalAccessToken, cancellationToken);

        var positionStart = TeacherProfileMapper.ParseDate(details.CurrentPositionStartDate)
            ?? throw AppException.UpstreamUnavailable("La hoja de vida recibida no tiene una fecha de inicio de cargo válida.");

        var nextPosition = PositionLadder.GetNextPosition(details.CurrentPosition);
        var utcNow = DateTime.UtcNow;

        return new TeacherProfileDto(
            utcNow,
            details.CurrentPosition,
            PositionLadder.Label(details.CurrentPosition),
            nextPosition,
            nextPosition is null ? null : PositionLadder.Label(nextPosition),
            details);
    }

}
