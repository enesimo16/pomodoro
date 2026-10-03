using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Auth.DTOs;
using Focus.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Auth.Commands;

public record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress = null) : IRequest<AuthResponseDto>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        IJwtTokenService jwtTokenService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _jwtTokenService.HashToken(request.RefreshToken);

        var existingToken = await _context.RefreshTokens
            .Include(t => t.User)
                .ThenInclude(u => u.Avatar)
            .Include(t => t.User)
                .ThenInclude(u => u.Room)
                    .ThenInclude(r => r!.Items)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (existingToken == null || !existingToken.IsActive)
        {
            throw new UnauthorizedAccessException("Geçersiz veya süresi dolmuş yenileme belirteci.");
        }

        var user = existingToken.User;
        var (newAccessToken, newRefreshToken, expiresAt) = _jwtTokenService.GenerateTokens(user);
        var newHash = _jwtTokenService.HashToken(newRefreshToken);

        existingToken.Revoke(request.IpAddress, newHash);
        var newRefreshTokenRecord = new RefreshToken(user.Id, newHash, DateTime.UtcNow.AddDays(30), request.IpAddress);
        _context.RefreshTokens.Add(newRefreshTokenRecord);

        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(
            UserDto.FromEntity(user),
            user.Avatar != null ? AvatarDto.FromEntity(user.Avatar) : null,
            user.Room != null ? PixelRoomDto.FromEntity(user.Room) : null,
            newAccessToken,
            newRefreshToken,
            expiresAt);
    }
}
