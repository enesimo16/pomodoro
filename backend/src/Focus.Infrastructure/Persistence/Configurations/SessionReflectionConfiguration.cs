using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Focus.Infrastructure.Persistence.Configurations;

public class SessionReflectionConfiguration : IEntityTypeConfiguration<SessionReflection>
{
    public void Configure(EntityTypeBuilder<SessionReflection> builder)
    {
        builder.ToTable("SessionReflections");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.SessionId)
            .IsRequired();

        builder.Property(r => r.UserId)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(r => r.FocusQuality)
            .IsRequired();

        builder.Property(r => r.MoodAfter)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(r => r.DistractionNote)
            .HasMaxLength(350)
            .IsRequired(false);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.HasIndex(r => r.SessionId);
        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => r.CreatedAt);
    }
}
