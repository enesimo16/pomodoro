using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Focus.Infrastructure.Persistence.Configurations;

public class CoinLedgerEntryConfiguration : IEntityTypeConfiguration<CoinLedgerEntry>
{
    public void Configure(EntityTypeBuilder<CoinLedgerEntry> builder)
    {
        builder.ToTable("coin_ledger_entries");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Coins)
            .IsRequired();

        builder.HasIndex(c => c.UserId);

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
