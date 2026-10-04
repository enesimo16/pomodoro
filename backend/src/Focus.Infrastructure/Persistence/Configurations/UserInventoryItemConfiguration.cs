using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Focus.Infrastructure.Persistence.Configurations;

public class UserInventoryItemConfiguration : IEntityTypeConfiguration<UserInventoryItem>
{
    public void Configure(EntityTypeBuilder<UserInventoryItem> builder)
    {
        builder.ToTable("user_inventory_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.UserId).IsRequired();
        builder.Property(i => i.CatalogItemId).IsRequired().HasMaxLength(64);
        builder.Property(i => i.AcquiredAt).IsRequired();
        builder.Property(i => i.IsEquipped).IsRequired().HasDefaultValue(false);

        builder.HasOne(i => i.User)
            .WithMany(u => u.InventoryItems)
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.CatalogItem)
            .WithMany()
            .HasForeignKey(i => i.CatalogItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.UserId, i.CatalogItemId });
    }
}
