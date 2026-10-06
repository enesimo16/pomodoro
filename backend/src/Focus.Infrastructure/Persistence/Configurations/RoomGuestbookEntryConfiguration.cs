using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Focus.Infrastructure.Persistence.Configurations;

public class RoomGuestbookEntryConfiguration : IEntityTypeConfiguration<RoomGuestbookEntry>
{
    public void Configure(EntityTypeBuilder<RoomGuestbookEntry> builder)
    {
        builder.ToTable("RoomGuestbookEntries");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.StudyRoomId)
            .IsRequired();

        builder.Property(g => g.SenderUserId)
            .IsRequired();

        builder.Property(g => g.SenderDisplayName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(g => g.Message)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(g => g.GiftType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(g => g.CreatedAt)
            .IsRequired();

        builder.HasOne(g => g.StudyRoom)
            .WithMany()
            .HasForeignKey(g => g.StudyRoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(g => g.SenderUser)
            .WithMany()
            .HasForeignKey(g => g.SenderUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(g => g.StudyRoomId);
        builder.HasIndex(g => g.CreatedAt);
    }
}
