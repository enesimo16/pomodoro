using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Auth.DTOs;
using Focus.Domain.Entities;
using MediatR;

namespace Focus.Application.Features.Auth.Commands;

public record CreateGuestCommand(
    string? DisplayName = null,
    string? Locale = null,
    string? TimeZoneId = null,
    string? IpAddress = null) : IRequest<AuthResponseDto>;

public class CreateGuestCommandHandler : IRequestHandler<CreateGuestCommand, AuthResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;

    public CreateGuestCommandHandler(
        IApplicationDbContext context,
        IJwtTokenService jwtTokenService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponseDto> Handle(CreateGuestCommand request, CancellationToken cancellationToken)
    {
        var user = User.CreateGuest(request.DisplayName, request.Locale, request.TimeZoneId);

        // Başlangıç hoş geldin Focus Coin hediyesi
        var welcomeBonus = new CoinLedgerEntry(user.Id, CoinTransactionReason.InitialBonus, 50);

        var (accessToken, refreshToken, expiresAt) = _jwtTokenService.GenerateTokens(user);
        var tokenHash = _jwtTokenService.HashToken(refreshToken);

        user.AddRefreshToken(tokenHash, DateTime.UtcNow.AddDays(30), request.IpAddress);

        _context.Users.Add(user);
        _context.CoinLedgerEntries.Add(welcomeBonus);

        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(
            UserDto.FromEntity(user),
            user.Avatar != null ? AvatarDto.FromEntity(user.Avatar) : null,
            user.Room != null ? PixelRoomDto.FromEntity(user.Room) : null,
            accessToken,
            refreshToken,
            expiresAt);
    }
}
