using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Focus.Infrastructure.Persistence.Configurations;

public class RoomMemberConfiguration : IEntityTypeConfiguration<RoomMember>
{
    public void Configure(EntityTypeBuilder<RoomMember> builder)
    {
        builder.ToTable("room_members");

        builder.HasKey(m => m.Id);

        builder.HasIndex(m => new { m.RoomId, m.UserId })
            .IsUnique();

        // Bir masada ayni anda yalnizca bir uye oturabilir
        builder.HasIndex(m => new { m.RoomId, m.SeatIndex }, "IX_room_members_RoomId_SeatIndex")
            .IsUnique()
            .HasFilter("\"SeatIndex\" IS NOT NULL");

        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
