using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PromocionBackend.Domain.Entities;

namespace PromocionBackend.Infrastructure.Persistence.Configurations;

public class CommissionConfiguration : IEntityTypeConfiguration<Commission>
{
    public void Configure(EntityTypeBuilder<Commission> builder)
    {
        builder.ToTable("Commissions");

        builder.HasKey(c => c.Id);
        builder.HasIndex(c => new { c.ProcessId, c.Type, c.Date });

        builder.Property(c => c.Type).HasMaxLength(10).IsRequired();

        builder.HasOne(c => c.Process)
            .WithMany()
            .HasForeignKey(c => c.ProcessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.CreatedBy)
            .WithMany()
            .HasForeignKey(c => c.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Members)
            .WithOne(m => m.Commission)
            .HasForeignKey(m => m.CommissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CommissionMemberConfiguration : IEntityTypeConfiguration<CommissionMember>
{
    public void Configure(EntityTypeBuilder<CommissionMember> builder)
    {
        builder.ToTable("CommissionMembers");

        builder.HasKey(m => m.Id);
        builder.HasIndex(m => m.CommissionId);

        builder.Property(m => m.CargoLabel).HasMaxLength(500).IsRequired();
        builder.Property(m => m.TeacherIdentification).HasMaxLength(20).IsRequired();
        builder.Property(m => m.TeacherFullName).HasMaxLength(500).IsRequired();
        builder.Property(m => m.TeacherExternalId).HasMaxLength(30);
    }
}
