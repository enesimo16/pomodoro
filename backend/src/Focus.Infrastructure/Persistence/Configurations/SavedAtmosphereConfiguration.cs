namespace Focus.Infrastructure.Persistence.Configurations;

using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SavedAtmosphereConfiguration : IEntityTypeConfiguration<SavedAtmosphere>
{
    public void Configure(EntityTypeBuilder<SavedAtmosphere> builder)
    {
        builder.ToTable("SavedAtmospheres");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(a => a.Description)
            .HasMaxLength(500);

        builder.Property(a => a.Aesthetic)
            .HasMaxLength(50);

        builder.Property(a => a.WallColor)
            .HasMaxLength(20);

        builder.Property(a => a.FloorColor)
            .HasMaxLength(20);

        builder.Property(a => a.AccentLightColor)
            .HasMaxLength(20);

        builder.Property(a => a.WallPaperId)
            .HasMaxLength(100);

        builder.Property(a => a.FloorId)
            .HasMaxLength(100);

        builder.Property(a => a.WeatherEffect)
            .HasMaxLength(50);

        builder.Property(a => a.WindowVideoQuery)
            .HasMaxLength(100);

        builder.Property(a => a.MusicGenre)
            .HasMaxLength(100);

        builder.Property(a => a.MusicSearchQuery)
            .HasMaxLength(150);

        builder.Property(a => a.AmbienceType)
            .HasMaxLength(50);

        builder.Property(a => a.NoiseType)
            .HasMaxLength(50);

        builder.Property(a => a.TextureType)
            .HasMaxLength(50);

        builder.Property(a => a.FlowShieldLevel)
            .HasMaxLength(30);

        builder.Property(a => a.PromptUsed)
            .HasMaxLength(1000);

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.CreatedAt);
    }
}
