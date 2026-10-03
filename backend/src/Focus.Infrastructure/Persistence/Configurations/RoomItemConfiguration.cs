using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Focus.Infrastructure.Persistence.Configurations;

public class RoomItemConfiguration : IEntityTypeConfiguration<RoomItem>
{
    public void Configure(EntityTypeBuilder<RoomItem> builder)
    {
        builder.ToTable("room_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.CatalogItemId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(i => i.RoomId);
    }
}
