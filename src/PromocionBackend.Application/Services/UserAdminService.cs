using Microsoft.EntityFrameworkCore;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Application.Common;
using PromocionBackend.Application.DTOs.Users;
using PromocionBackend.Domain.Constants;

namespace PromocionBackend.Application.Services;

/// <summary>
/// Gestión de usuarios para el rol administrador: listado, cambio de rol y
/// activación/desactivación de cuentas.
/// </summary>
public class UserAdminService(IAppDbContext db)
{
    public async Task<IReadOnlyList<UserDto>> ListAsync(string? search, string? role, CancellationToken cancellationToken = default)
    {
        var query = db.Users.Include(u => u.Snapshot).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => u.Email.Contains(search) || u.FullName.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => u.Role == role);
        }

        var users = await query
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken);

        return [.. users.Select(ToDto)];
    }

    public async Task<UserDto> ChangeRoleAsync(Guid userId, string newRole, CancellationToken cancellationToken = default)
    {
        if (!Roles.IsValid(newRole))
        {
            throw AppException.BadRequest($"Rol inválido. Los roles permitidos son: {string.Join(", ", Roles.All)}.");
        }

        var user = await db.Users
            .Include(u => u.Snapshot)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw AppException.NotFound("Usuario no encontrado.");

        if (user.Role == Roles.Admin && newRole != Roles.Admin)
        {
            await EnsureNotLastActiveAdminAsync(user.Id, cancellationToken);
        }

        user.Role = newRole;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(user);
    }

    public async Task<UserDto> SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .Include(u => u.Snapshot)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw AppException.NotFound("Usuario no encontrado.");

        if (!isActive && user.Role == Roles.Admin)
        {
            await EnsureNotLastActiveAdminAsync(user.Id, cancellationToken);
        }

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(user);
    }

    private async Task EnsureNotLastActiveAdminAsync(Guid userId, CancellationToken cancellationToken)
    {
        var otherActiveAdmins = await db.Users
            .CountAsync(u => u.Id != userId && u.Role == Roles.Admin && u.IsActive, cancellationToken);

        if (otherActiveAdmins == 0)
        {
            throw AppException.Conflict("No se puede modificar al último administrador activo del sistema.");
        }
    }

    private static UserDto ToDto(Domain.Entities.User user) => new(
        user.Id,
        user.Email,
        user.FullName,
        user.Role,
        user.IsActive,
        user.Identification,
        user.TeacherId,
        user.Snapshot?.CurrentPosition,
        user.LastLoginAt,
        user.CreatedAt);
}
