using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Focus.Infrastructure.Persistence.Configurations;

public class CatalogItemConfiguration : IEntityTypeConfiguration<CatalogItem>
{
    public void Configure(EntityTypeBuilder<CatalogItem> builder)
    {
        builder.ToTable("catalog_items");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasMaxLength(64);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(128);
        builder.Property(c => c.Description).IsRequired().HasMaxLength(512);
        builder.Property(c => c.Category).IsRequired();
        builder.Property(c => c.Tier).IsRequired();
        builder.Property(c => c.RequiredLevel).IsRequired().HasDefaultValue(1);
        builder.Property(c => c.CoinPrice).IsRequired().HasDefaultValue(0);
        builder.Property(c => c.SpriteUrl).IsRequired().HasMaxLength(256);
        builder.Property(c => c.IsInteractive).IsRequired().HasDefaultValue(false);
        builder.Property(c => c.GridWidth).IsRequired().HasDefaultValue(1);
        builder.Property(c => c.GridHeight).IsRequired().HasDefaultValue(1);
        builder.Property(c => c.CreatedAt).IsRequired();
    }
}
