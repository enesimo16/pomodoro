using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Focus.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Email)
            .HasMaxLength(256);

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL AND \"Email\" IS NOT NULL");

        builder.Property(u => u.Locale)
            .HasMaxLength(20)
            .HasDefaultValue("tr-TR");

        builder.Property(u => u.TimeZoneId)
            .HasMaxLength(50)
            .HasDefaultValue("Europe/Istanbul");

        builder.HasQueryFilter(u => u.DeletedAt == null);

        builder.HasOne(u => u.Avatar)
            .WithOne(a => a.User)
            .HasForeignKey<UserAvatar>(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.Room)
            .WithOne(r => r.OwnerUser)
            .HasForeignKey<PixelRoom>(r => r.OwnerUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.Preferences)
            .WithOne(p => p.User)
            .HasForeignKey<UserPreferences>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.ExternalLogins)
            .WithOne(l => l.User)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
