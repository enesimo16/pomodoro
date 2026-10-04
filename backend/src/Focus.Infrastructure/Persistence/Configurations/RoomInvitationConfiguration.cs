using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Focus.Infrastructure.Persistence.Configurations;

public class RoomInvitationConfiguration : IEntityTypeConfiguration<RoomInvitation>
{
    public void Configure(EntityTypeBuilder<RoomInvitation> builder)
    {
        builder.ToTable("room_invitations");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.InviteCode)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(i => i.InviteCode)
            .IsUnique();

        builder.Property(i => i.InviteeUsername)
            .HasMaxLength(64);

        builder.HasOne(i => i.Inviter)
            .WithMany()
            .HasForeignKey(i => i.InviterUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Invitee)
            .WithMany()
            .HasForeignKey(i => i.InviteeUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
