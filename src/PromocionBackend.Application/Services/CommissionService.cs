using Microsoft.EntityFrameworkCore;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.Commissions;
using PromocionBackend.Domain.Constants;
using PromocionBackend.Domain.Entities;

namespace PromocionBackend.Application.Services;

/// <summary>
/// Comisiones de Promoción (CP) y de Apelaciones (CA): cada proceso tiene una comisión
/// principal por tipo, y puede tener comisiones adicionales para días concretos en los
/// que algún integrante fue reemplazado por su delegado. Toda decisión de CP/CA queda
/// ligada a la comisión que la tomó ese día.
/// </summary>
public class CommissionService(IAppDbContext db)
{
    /// <summary>
    /// Crea una comisión para un día concreto (con delegados). La comisión principal de un
    /// proceso solo se crea una vez, junto con el proceso (ver ProcessService); por eso aquí
    /// se ignora cualquier valor de IsPrincipal que envíe el cliente: la nueva comisión solo
    /// queda como principal si el proceso aún no tiene ninguna (caso excepcional de datos
    /// heredados), y en cualquier otro caso siempre se crea como comisión de delegados.
    /// </summary>
    public async Task<CommissionDto> CreateAsync(CreateCommissionRequest request, ICurrentUserService currentUser, CancellationToken cancellationToken = default)
    {
        EnsureRoleMatchesType(currentUser.Role, request.Type);

        var hasPrincipal = await db.Commissions.AnyAsync(
            c => c.ProcessId == request.ProcessId && c.Type == request.Type && c.IsPrincipal,
            cancellationToken);

        request.IsPrincipal = !hasPrincipal;

        return await CreateInternalAsync(request, currentUser.UserId, cancellationToken);
    }

    /// <summary>Crea una comisión sin verificar rol; usado al crear un proceso (ya protegido por [Authorize(Roles=Cp)]).</summary>
    public async Task<CommissionDto> CreateInternalAsync(CreateCommissionRequest request, Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        if (!CommissionTypes.IsValid(request.Type))
        {
            throw AppException.BadRequest("Tipo de comisión inválido.");
        }

        var processExists = await db.Processes.AnyAsync(p => p.Id == request.ProcessId, cancellationToken);
        if (!processExists)
        {
            throw AppException.NotFound("Proceso de promoción no encontrado.");
        }

        if (request.Members.Count != 6)
        {
            throw AppException.BadRequest("La comisión debe tener exactamente 6 integrantes.");
        }

        if (request.IsPrincipal)
        {
            var existingPrincipal = await db.Commissions
                .FirstOrDefaultAsync(c => c.ProcessId == request.ProcessId && c.Type == request.Type && c.IsPrincipal, cancellationToken);

            if (existingPrincipal is not null)
            {
                existingPrincipal.IsPrincipal = false;
            }
        }

        var commission = new Commission
        {
            Id = Guid.NewGuid(),
            ProcessId = request.ProcessId,
            Type = request.Type,
            IsPrincipal = request.IsPrincipal,
            Date = request.Date.Date,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            Members = [.. request.Members.Select((m, index) => new CommissionMember
            {
                Id = Guid.NewGuid(),
                OrderIndex = index + 1,
                CargoLabel = m.CargoLabel.Trim(),
                TeacherIdentification = m.TeacherIdentification.Trim(),
                TeacherFullName = m.TeacherFullName.Trim(),
                TeacherExternalId = string.IsNullOrWhiteSpace(m.TeacherExternalId) ? null : m.TeacherExternalId.Trim()
            })]
        };

        db.Commissions.Add(commission);
        await db.SaveChangesAsync(cancellationToken);

        return await GetDetailAsync(commission.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<CommissionDto>> ListAsync(Guid processId, string type, ICurrentUserService currentUser, CancellationToken cancellationToken = default)
    {
        EnsureRoleMatchesType(currentUser.Role, type);

        var commissions = await db.Commissions
            .Include(c => c.CreatedBy)
            .Include(c => c.Members)
            .Where(c => c.ProcessId == processId && c.Type == type)
            .OrderByDescending(c => c.IsPrincipal)
            .ThenByDescending(c => c.Date)
            .ToListAsync(cancellationToken);

        return [.. commissions.Select(ToDto)];
    }

    public async Task<CommissionDto> GetDetailAsync(Guid commissionId, CancellationToken cancellationToken = default)
    {
        var commission = await db.Commissions
            .Include(c => c.CreatedBy)
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == commissionId, cancellationToken)
            ?? throw AppException.NotFound("Comisión no encontrada.");

        return ToDto(commission);
    }

    private static void EnsureRoleMatchesType(string role, string type)
    {
        if (!CommissionTypes.RoleMatchesType(role, type))
        {
            throw AppException.Forbidden("Su rol no puede administrar comisiones de este tipo.");
        }
    }

    private static CommissionDto ToDto(Commission commission) => new(
        commission.Id,
        commission.ProcessId,
        commission.Type,
        commission.IsPrincipal,
        commission.Date,
        commission.CreatedAt,
        commission.CreatedBy.FullName,
        [.. commission.Members
            .OrderBy(m => m.OrderIndex)
            .Select(m => new CommissionMemberDto(m.OrderIndex, m.CargoLabel, m.TeacherIdentification, m.TeacherFullName, m.TeacherExternalId))]);
}
