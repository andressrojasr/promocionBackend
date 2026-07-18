using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PromocionBackend.Domain.Entities;

namespace PromocionBackend.Infrastructure.Persistence.Configurations;

public class PromotionProcessConfiguration : IEntityTypeConfiguration<PromotionProcess>
{
    public void Configure(EntityTypeBuilder<PromotionProcess> builder)
    {
        builder.ToTable("PromotionProcesses");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(1000);

        builder.HasOne(p => p.CreatedBy)
            .WithMany()
            .HasForeignKey(p => p.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Requirements)
            .WithOne(r => r.Process)
            .HasForeignKey(r => r.ProcessId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProcessRequirementConfiguration : IEntityTypeConfiguration<ProcessRequirement>
{
    public void Configure(EntityTypeBuilder<ProcessRequirement> builder)
    {
        builder.ToTable("ProcessRequirements");

        builder.HasKey(r => r.Id);
        builder.HasIndex(r => new { r.ProcessId, r.FromPosition }).IsUnique();

        builder.Property(r => r.FromPosition).HasMaxLength(30).IsRequired();
        builder.Property(r => r.ToPosition).HasMaxLength(30).IsRequired();
        builder.Property(r => r.MinEvaluationScorePct).HasPrecision(5, 2);
        builder.Property(r => r.MinPedagogicalTrainingPct).HasPrecision(5, 2);
        builder.Property(r => r.ProjectRoleScope).HasMaxLength(20).IsRequired();
        builder.Property(r => r.RequiredLanguageLevel).HasMaxLength(5);
        builder.Property(r => r.Notes).HasMaxLength(1000);
    }
}
