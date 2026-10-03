using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Focus.Infrastructure.Persistence.Configurations;

public class UserExternalLoginConfiguration : IEntityTypeConfiguration<UserExternalLogin>
{
    public void Configure(EntityTypeBuilder<UserExternalLogin> builder)
    {
        builder.ToTable("user_external_logins");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.ProviderKey)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(l => l.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(l => new { l.Provider, l.ProviderKey })
            .IsUnique();

        builder.HasIndex(l => l.UserId);
    }
}
