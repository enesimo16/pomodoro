using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Focus.Infrastructure.Persistence.Configurations;

public class PixelRoomConfiguration : IEntityTypeConfiguration<PixelRoom>
{
    public void Configure(EntityTypeBuilder<PixelRoom> builder)
    {
        builder.ToTable("pixel_rooms");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.WallPaperId)
            .HasMaxLength(100);

        builder.Property(r => r.FloorId)
            .HasMaxLength(100);

        builder.Property(r => r.InviteCode)
            .HasMaxLength(32);

        builder.HasIndex(r => r.InviteCode);

        builder.HasMany(r => r.Items)
            .WithOne(i => i.Room)
            .HasForeignKey(i => i.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
