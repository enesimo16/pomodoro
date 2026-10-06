using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Focus.Infrastructure.Persistence.Configurations;

public class UserDeviceSessionConfiguration : IEntityTypeConfiguration<UserDeviceSession>
{
    public void Configure(EntityTypeBuilder<UserDeviceSession> builder)
    {
        builder.ToTable("UserDeviceSessions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.IpAddress)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.DeviceType)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.OperatingSystem)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.Browser)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(1000);

        builder.Property(x => x.LastPath)
            .HasMaxLength(256);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.IpAddress);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.LastSeenAt);
    }
}
