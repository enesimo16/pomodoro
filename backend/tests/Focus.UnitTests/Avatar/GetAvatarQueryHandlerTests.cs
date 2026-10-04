using FluentAssertions;
using Focus.Application.Features.Avatar.Queries;
using Focus.Domain.Entities;
using Focus.UnitTests.Common;
using Xunit;

namespace Focus.UnitTests.Avatar;

public class GetAvatarQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnAvatarDto_WhenAvatarExists()
    {
        using var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var avatar = UserAvatar.CreateDefault(userId);
        context.UserAvatars.Add(avatar);
        await context.SaveChangesAsync();

        var handler = new GetAvatarQueryHandler(context);
        var result = await handler.Handle(new GetAvatarQuery(userId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.SkinTone.Should().Be("tone_1");
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenAvatarDoesNotExist()
    {
        using var context = TestDbContextFactory.Create();
        var handler = new GetAvatarQueryHandler(context);

        var result = await handler.Handle(new GetAvatarQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }
}
