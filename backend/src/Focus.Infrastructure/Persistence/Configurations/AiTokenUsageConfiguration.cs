using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Focus.Infrastructure.Persistence.Configurations;

public class AiTokenUsageConfiguration : IEntityTypeConfiguration<AiTokenUsage>
{
    public void Configure(EntityTypeBuilder<AiTokenUsage> builder)
    {
        builder.ToTable("AiTokenUsages");

        builder.HasKey(u => u.Id);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(u => u.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(u => u.ConcurrencyStamp)
            .IsConcurrencyToken();

        builder.HasIndex(u => new { u.UserId, u.UsageDate })
            .IsUnique();
    }
}
