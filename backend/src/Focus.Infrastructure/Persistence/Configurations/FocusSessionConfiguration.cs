using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Focus.Infrastructure.Persistence.Configurations;

public class FocusSessionConfiguration : IEntityTypeConfiguration<FocusSession>
{
    public void Configure(EntityTypeBuilder<FocusSession> builder)
    {
        builder.ToTable("focus_sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Kind)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(s => s.ThemeId)
            .HasMaxLength(100);

        builder.Property(s => s.MixPresetId)
            .HasMaxLength(100);

        builder.Property(s => s.ConcurrencyStamp)
            .IsConcurrencyToken();

        builder.HasIndex(s => s.UserId);

        // Kullanici basina yalnizca bir aktif (Running/Paused) seans
        builder.HasIndex(s => s.UserId, "IX_focus_sessions_UserId_Active")
            .IsUnique()
            .HasFilter("\"Status\" IN ('Running', 'Paused')");
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.StartedAt);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Room)
            .WithMany()
            .HasForeignKey(s => s.RoomId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
