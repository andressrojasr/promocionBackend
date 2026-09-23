using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PromocionBackend.Domain.Entities;

namespace PromocionBackend.Infrastructure.Persistence.Configurations;

public class ReviewSessionConfiguration : IEntityTypeConfiguration<ReviewSession>
{
    public void Configure(EntityTypeBuilder<ReviewSession> builder)
    {
        builder.ToTable("ReviewSessions");

        builder.HasKey(s => s.Id);
        builder.HasIndex(s => new { s.ProcessId, s.Type, s.FacultyId, s.CreatedAt });

        builder.Property(s => s.ProcessName).HasMaxLength(300).IsRequired();
        builder.Property(s => s.Type).HasMaxLength(10).IsRequired();
        builder.Property(s => s.FacultyId).HasMaxLength(50).IsRequired();
        builder.Property(s => s.FacultyName).HasMaxLength(300).IsRequired();

        builder.HasOne(s => s.Process)
            .WithMany()
            .HasForeignKey(s => s.ProcessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Commission)
            .WithMany()
            .HasForeignKey(s => s.CommissionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.CreatedBy)
            .WithMany()
            .HasForeignKey(s => s.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
