using Microsoft.EntityFrameworkCore;
using PromocionBackend.Domain.Entities;

namespace PromocionBackend.Application.Abstractions;

/// <summary>
/// Abstracción de la unidad de persistencia. La implementación concreta
/// (EF Core + SQL Server) vive en la capa de infraestructura.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<TeacherSnapshot> TeacherSnapshots { get; }
    DbSet<PromotionProcess> Processes { get; }
    DbSet<ProcessRequirement> ProcessRequirements { get; }
    DbSet<PromotionApplication> Applications { get; }
    DbSet<ApplicationItem> ApplicationItems { get; }
    DbSet<ApplicationReview> ApplicationReviews { get; }
    DbSet<Appeal> Appeals { get; }
    DbSet<Notification> Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
