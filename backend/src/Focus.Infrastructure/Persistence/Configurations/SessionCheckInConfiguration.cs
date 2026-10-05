using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Focus.Infrastructure.Persistence.Configurations;

public class SessionCheckInConfiguration : IEntityTypeConfiguration<SessionCheckIn>
{
    public void Configure(EntityTypeBuilder<SessionCheckIn> builder)
    {
        builder.ToTable("SessionCheckIns");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.SessionId)
            .IsRequired();

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(c => c.Mood)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(c => c.EnergyLevel)
            .IsRequired();

        builder.Property(c => c.TargetIntent)
            .HasMaxLength(250)
            .IsRequired(false);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.HasIndex(c => c.SessionId);
        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => c.CreatedAt);
    }
}
