using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Focus.Infrastructure.Persistence.Configurations;

public class AgentMemoryConfiguration : IEntityTypeConfiguration<AgentMemory>
{
    public void Configure(EntityTypeBuilder<AgentMemory> builder)
    {
        builder.ToTable("AgentMemories");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.UserId)
            .IsRequired(false);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(m => m.Category)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.Content)
            .HasMaxLength(1500)
            .IsRequired();

        builder.Property(m => m.Importance)
            .IsRequired();

        builder.Property(m => m.UsageCount)
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.Property(m => m.LastAccessedAt)
            .IsRequired(false);

        builder.Ignore(m => m.IsGlobal);

        builder.HasIndex(m => m.UserId);
        builder.HasIndex(m => m.Category);
    }
}
