using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Focus.Infrastructure.Persistence.Configurations;

public class UserStreakConfiguration : IEntityTypeConfiguration<UserStreak>
{
    public void Configure(EntityTypeBuilder<UserStreak> builder)
    {
        builder.ToTable("user_streaks");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.CurrentStreak).IsRequired().HasDefaultValue(0);
        builder.Property(s => s.LongestStreak).IsRequired().HasDefaultValue(0);
        builder.Property(s => s.FreezesAvailable).IsRequired().HasDefaultValue(1);

        builder.HasOne(s => s.User)
            .WithOne(u => u.Streak)
            .HasForeignKey<UserStreak>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.UserId).IsUnique();
    }
}
