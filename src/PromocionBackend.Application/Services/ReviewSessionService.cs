using Microsoft.EntityFrameworkCore;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.ReviewSessions;
using PromocionBackend.Domain.Constants;
using PromocionBackend.Domain.Entities;

namespace PromocionBackend.Application.Services;

/// <summary>
/// Sesiones de revisión: registran que un usuario CP/CA inició trabajo sobre un
/// proceso + comisión + facultad determinados, para que las decisiones tomadas durante
/// esa sesión queden ligadas automáticamente sin reelegir la comisión en cada una, y
/// para poder buscar/reanudar sesiones pasadas (y descargar su acta).
/// </summary>
public class ReviewSessionService(IAppDbContext db)
{
    public async Task<ReviewSessionDto> CreateAsync(CreateReviewSessionRequest request, ICurrentUserService currentUser, CancellationToken cancellationToken = default)
    {
        if (!CommissionTypes.RoleMatchesType(currentUser.Role, request.Type))
        {
            throw AppException.Forbidden("Su rol no puede iniciar sesiones de revisión de este tipo.");
        }

        var process = await db.Processes
            .FirstOrDefaultAsync(p => p.Id == request.ProcessId, cancellationToken)
            ?? throw AppException.NotFound("Proceso de promoción no encontrado.");

        var commission = await db.Commissions
            .FirstOrDefaultAsync(c => c.Id == request.CommissionId && c.ProcessId == request.ProcessId && c.Type == request.Type, cancellationToken)
            ?? throw AppException.NotFound("La comisión seleccionada no corresponde a este proceso y tipo.");

        var session = new ReviewSession
        {
            Id = Guid.NewGuid(),
            ProcessId = process.Id,
            ProcessName = process.Name,
            Type = request.Type,
            CommissionId = commission.Id,
            CommissionDate = commission.Date,
            CommissionIsPrincipal = commission.IsPrincipal,
            FacultyId = request.FacultyId.Trim(),
            FacultyName = request.FacultyName.Trim(),
            CreatedByUserId = currentUser.UserId,
            CreatedAt = DateTime.UtcNow
        };

        db.ReviewSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        return await GetDetailAsync(session.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<ReviewSessionDto>> ListAsync(
        ICurrentUserService currentUser,
        Guid? processId,
        string? type,
        string? facultyId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        bool? closed = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveType = type ?? (currentUser.Role is Roles.Cp or Roles.Ca ? currentUser.Role : null);

        if (effectiveType is not null && !CommissionTypes.RoleMatchesType(currentUser.Role, effectiveType))
        {
            throw AppException.Forbidden("Su rol no puede consultar sesiones de este tipo.");
        }

        var query = db.ReviewSessions.Include(s => s.CreatedBy).AsQueryable();

        if (effectiveType is not null)
        {
            query = query.Where(s => s.Type == effectiveType);
        }

        if (processId is { } pid)
        {
            query = query.Where(s => s.ProcessId == pid);
        }

        if (!string.IsNullOrWhiteSpace(facultyId))
        {
            query = query.Where(s => s.FacultyId == facultyId);
        }

        if (dateFrom is { } from)
        {
            var fromUtc = from.ToDateTime(TimeOnly.MinValue);
            query = query.Where(s => s.CreatedAt >= fromUtc);
        }

        if (dateTo is { } to)
        {
            var toUtc = to.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(s => s.CreatedAt <= toUtc);
        }

        if (closed is { } closedFilter)
        {
            query = closedFilter ? query.Where(s => s.ClosedAt != null) : query.Where(s => s.ClosedAt == null);
        }

        var sessions = await query.OrderByDescending(s => s.CreatedAt).ToListAsync(cancellationToken);
        return [.. sessions.Select(ToDto)];
    }

    public async Task<ReviewSessionDto> GetDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var session = await db.ReviewSessions
            .Include(s => s.CreatedBy)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw AppException.NotFound("Sesión de revisión no encontrada.");

        return ToDto(session);
    }

    /// <summary>Cierra una sesión: ya no podrá usarse para decidir postulaciones ni reanudarse.</summary>
    public async Task<ReviewSessionDto> CloseAsync(Guid id, ICurrentUserService currentUser, CancellationToken cancellationToken = default)
    {
        var session = await db.ReviewSessions
            .Include(s => s.CreatedBy)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw AppException.NotFound("Sesión de revisión no encontrada.");

        if (!CommissionTypes.RoleMatchesType(currentUser.Role, session.Type))
        {
            throw AppException.Forbidden("Su rol no puede cerrar sesiones de este tipo.");
        }

        if (session.ClosedAt is null)
        {
            session.ClosedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        return ToDto(session);
    }

    private static ReviewSessionDto ToDto(ReviewSession s) => new(
        s.Id,
        s.ProcessId,
        s.ProcessName,
        s.Type,
        s.CommissionId,
        s.CommissionDate,
        s.CommissionIsPrincipal,
        s.FacultyId,
        s.FacultyName,
        s.CreatedByUserId,
        s.CreatedBy.FullName,
        s.CreatedAt,
        s.ClosedAt);
}
