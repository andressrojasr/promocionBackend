using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Abstractions.External;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.Teachers;
using PromocionBackend.Domain.Services;

namespace PromocionBackend.Application.Services;

/// <summary>
/// Expone la hoja de vida del docente almacenada en su snapshot, para la página
/// de perfil y la selección de documentos al postular.
/// </summary>
public class TeacherProfileService(IAppDbContext db)
{
    public async Task<TeacherProfileDto> GetMyProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var snapshot = await db.TeacherSnapshots
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken)
            ?? throw AppException.NotFound("No se encontró la hoja de vida del docente. Inicie sesión nuevamente para sincronizarla.");

        var details = JsonSerializer.Deserialize<HrTeacherDetails>(snapshot.SnapshotJson, AppJson.Options)
            ?? throw AppException.UpstreamUnavailable("No fue posible leer la hoja de vida almacenada.");

        var nextPosition = PositionLadder.GetNextPosition(snapshot.CurrentPosition);

        return new TeacherProfileDto(
            snapshot.CapturedAt,
            snapshot.CurrentPosition,
            PositionLadder.Label(snapshot.CurrentPosition),
            nextPosition,
            nextPosition is null ? null : PositionLadder.Label(nextPosition),
            details);
    }
}
