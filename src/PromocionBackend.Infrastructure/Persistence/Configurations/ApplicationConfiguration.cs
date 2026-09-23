using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PromocionBackend.Domain.Constants;
using PromocionBackend.Domain.Entities;

namespace PromocionBackend.Infrastructure.Persistence.Configurations;

public class PromotionApplicationConfiguration : IEntityTypeConfiguration<PromotionApplication>
{
    public void Configure(EntityTypeBuilder<PromotionApplication> builder)
    {
        builder.ToTable("Applications");

        builder.HasKey(a => a.Id);
        builder.HasIndex(a => new { a.ProcessId, a.TeacherUserId }).IsUnique();
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.TeacherUserId);

        builder.Property(a => a.FromPosition).HasMaxLength(30).IsRequired();
        builder.Property(a => a.ToPosition).HasMaxLength(30).IsRequired();
        builder.Property(a => a.Status)
            .HasConversion(
                status => status.ToStringValue(),
                statusString => ApplicationStatusExtensions.FromStringValue(statusString))
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(a => a.TeacherId).HasMaxLength(255).IsRequired();
        builder.Property(a => a.TeacherName).HasMaxLength(500);
        builder.Property(a => a.CurrentPosition).HasMaxLength(255);
        builder.Property(a => a.ScorePct).HasPrecision(5, 2);
        builder.Property(a => a.ReviewLockedBy).IsRequired(false);
        builder.Property(a => a.ReviewLockedAt).IsRequired(false);
        builder.Property(a => a.ReviewLockExpiresAt).IsRequired(false);

        builder.HasIndex(a => new { a.ReviewLockedBy, a.ReviewLockExpiresAt });

        builder.HasOne(a => a.Process)
            .WithMany(p => p.Applications)
            .HasForeignKey(a => a.ProcessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Teacher)
            .WithMany()
            .HasForeignKey(a => a.TeacherUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.ReviewLocker)
            .WithMany()
            .HasForeignKey(a => a.ReviewLockedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(a => a.Items)
            .WithOne(i => i.Application)
            .HasForeignKey(i => i.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Reviews)
            .WithOne(r => r.Application)
            .HasForeignKey(r => r.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Appeal)
            .WithOne(ap => ap.Application)
            .HasForeignKey<Appeal>(ap => ap.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ApplicationItemConfiguration : IEntityTypeConfiguration<ApplicationItem>
{
    public void Configure(EntityTypeBuilder<ApplicationItem> builder)
    {
        builder.ToTable("ApplicationItems");

        builder.HasKey(i => i.Id);
        builder.HasIndex(i => new { i.ApplicationId, i.ItemType, i.ExternalItemId }).IsUnique();

        builder.Property(i => i.ItemType).HasMaxLength(30).IsRequired();
        builder.Property(i => i.ExternalItemId).HasMaxLength(40).IsRequired();
        builder.Property(i => i.Title).HasMaxLength(300).IsRequired();
        builder.Property(i => i.DocumentUrl).HasMaxLength(500);
    }
}

public class ApplicationReviewConfiguration : IEntityTypeConfiguration<ApplicationReview>
{
    public void Configure(EntityTypeBuilder<ApplicationReview> builder)
    {
        builder.ToTable("ApplicationReviews");

        builder.HasKey(r => r.Id);
        builder.HasIndex(r => r.ApplicationId);

        builder.Property(r => r.Stage).HasMaxLength(10).IsRequired();
        builder.Property(r => r.ReviewerRole).HasMaxLength(20).IsRequired();
        builder.Property(r => r.Decision).HasMaxLength(20).IsRequired();
        builder.Property(r => r.Feedback).HasMaxLength(2000);

        builder.HasOne(r => r.Reviewer)
            .WithMany()
            .HasForeignKey(r => r.ReviewerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Commission)
            .WithMany()
            .HasForeignKey(r => r.CommissionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.ReviewSession)
            .WithMany(s => s.Reviews)
            .HasForeignKey(r => r.ReviewSessionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AppealConfiguration : IEntityTypeConfiguration<Appeal>
{
    public void Configure(EntityTypeBuilder<Appeal> builder)
    {
        builder.ToTable("Appeals");

        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.ApplicationId).IsUnique();

        builder.Property(a => a.Justification).HasMaxLength(2000).IsRequired();
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);
        builder.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });

        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(1000).IsRequired();

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
