using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Focus.Infrastructure.Persistence.Configurations;

public class UserAvatarConfiguration : IEntityTypeConfiguration<UserAvatar>
{
    public void Configure(EntityTypeBuilder<UserAvatar> builder)
    {
        builder.ToTable("user_avatars");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.SkinTone)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.HairStyle)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.HairColor)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.TopItemCatalogId)
            .HasMaxLength(100);

        builder.Property(a => a.BottomItemCatalogId)
            .HasMaxLength(100);

        builder.Property(a => a.HatItemCatalogId)
            .HasMaxLength(100);

        builder.Property(a => a.GlassesItemCatalogId)
            .HasMaxLength(100);

        builder.Property(a => a.ShoesItemCatalogId)
            .HasMaxLength(100);
    }
}
