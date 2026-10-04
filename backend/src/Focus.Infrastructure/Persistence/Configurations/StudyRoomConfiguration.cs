using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Focus.Infrastructure.Persistence.Configurations;

public class StudyRoomConfiguration : IEntityTypeConfiguration<StudyRoom>
{
    public void Configure(EntityTypeBuilder<StudyRoom> builder)
    {
        builder.ToTable("study_rooms");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code)
            .IsRequired()
            .HasMaxLength(32);

        builder.HasIndex(r => r.Code)
            .IsUnique();

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(r => r.Description)
            .HasMaxLength(512);

        builder.Property(r => r.AccessCode)
            .HasMaxLength(64);

        builder.Property(r => r.ThemeId)
            .HasMaxLength(64);

        builder.Property(r => r.MusicTrackId)
            .HasMaxLength(128);

        builder.HasOne(r => r.OwnerUser)
            .WithMany()
            .HasForeignKey(r => r.OwnerUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(r => r.Members)
            .WithOne(m => m.Room)
            .HasForeignKey(m => m.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Invitations)
            .WithOne(i => i.Room)
            .HasForeignKey(i => i.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
