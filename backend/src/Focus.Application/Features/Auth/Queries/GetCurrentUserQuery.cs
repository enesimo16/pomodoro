using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Auth.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Auth.Queries;

public record CurrentUserDto(
    UserDto User,
    AvatarDto? Avatar,
    PixelRoomDto? Room,
    int TotalCoins);

public record GetCurrentUserQuery(Guid UserId) : IRequest<CurrentUserDto?>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserDto?>
{
    private readonly IApplicationDbContext _context;

    public GetCurrentUserQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CurrentUserDto?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.Avatar)
            .Include(u => u.Room)
                .ThenInclude(r => r!.Items)
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.DeletedAt == null, cancellationToken);

        if (user == null)
        {
            return null;
        }

        var totalCoins = await _context.CoinLedgerEntries
            .Where(c => c.UserId == user.Id)
            .SumAsync(c => (int?)c.Coins, cancellationToken) ?? 0;

        return new CurrentUserDto(
            UserDto.FromEntity(user),
            user.Avatar != null ? AvatarDto.FromEntity(user.Avatar) : null,
            user.Room != null ? PixelRoomDto.FromEntity(user.Room) : null,
            totalCoins);
    }
}
