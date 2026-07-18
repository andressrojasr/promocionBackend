using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PromocionBackend.Domain.Entities;

namespace PromocionBackend.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.Identification).HasMaxLength(20);
        builder.Property(u => u.TeacherId).HasMaxLength(20);
        builder.Property(u => u.FullName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Role).HasMaxLength(20).IsRequired();

        builder.HasOne(u => u.Snapshot)
            .WithOne(s => s.User)
            .HasForeignKey<TeacherSnapshot>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TeacherSnapshotConfiguration : IEntityTypeConfiguration<TeacherSnapshot>
{
    public void Configure(EntityTypeBuilder<TeacherSnapshot> builder)
    {
        builder.ToTable("TeacherSnapshots");

        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.UserId).IsUnique();

        builder.Property(s => s.CurrentPosition).HasMaxLength(30).IsRequired();
        builder.Property(s => s.SnapshotJson).IsRequired();
    }
}
