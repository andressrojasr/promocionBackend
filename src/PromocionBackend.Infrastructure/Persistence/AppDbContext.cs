using Microsoft.EntityFrameworkCore;
using PromocionBackend.Application.Abstractions;
using PromocionBackend.Domain.Entities;

namespace PromocionBackend.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<TeacherSnapshot> TeacherSnapshots => Set<TeacherSnapshot>();
    public DbSet<PromotionProcess> Processes => Set<PromotionProcess>();
    public DbSet<ProcessRequirement> ProcessRequirements => Set<ProcessRequirement>();
    public DbSet<PromotionApplication> Applications => Set<PromotionApplication>();
    public DbSet<ApplicationItem> ApplicationItems => Set<ApplicationItem>();
    public DbSet<ApplicationReview> ApplicationReviews => Set<ApplicationReview>();
    public DbSet<Appeal> Appeals => Set<Appeal>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
