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
    private const string InvalidSessionMessage = "Oturum geçersiz.";

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

        if (existingToken == null)
        {
            throw new UnauthorizedAccessException(InvalidSessionMessage);
        }

        if (existingToken.IsRevoked)
        {
            // Yalnizca rotasyonla degistirilmis belirtecin tekrar sunulmasi replay sayilir; cikis yapilmis belirtec duz reddedilir
            if (existingToken.WasRotated)
            {
                await RevokeAllActiveTokensAsync(existingToken.UserId, request.IpAddress, cancellationToken);
            }

            throw new UnauthorizedAccessException(InvalidSessionMessage);
        }

        if (existingToken.IsExpired)
        {
            throw new UnauthorizedAccessException(InvalidSessionMessage);
        }

        var user = existingToken.User;
        var (newAccessToken, newRefreshToken, expiresAt) = _jwtTokenService.GenerateTokens(user);
        var newHash = _jwtTokenService.HashToken(newRefreshToken);

        existingToken.Revoke(request.IpAddress, newHash);
        var newRefreshTokenRecord = new RefreshToken(user.Id, newHash, DateTime.UtcNow.Add(_jwtTokenService.RefreshTokenLifetime), request.IpAddress);
        _context.RefreshTokens.Add(newRefreshTokenRecord);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Ayni belirec es zamanli baska bir istekte tuketildi
            throw new UnauthorizedAccessException(InvalidSessionMessage);
        }

        return new AuthResponseDto(
            UserDto.FromEntity(user),
            user.Avatar != null ? AvatarDto.FromEntity(user.Avatar) : null,
            user.Room != null ? PixelRoomDto.FromEntity(user.Room) : null,
            newAccessToken,
            newRefreshToken,
            expiresAt);
    }

    private async Task RevokeAllActiveTokensAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(ipAddress);
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Baska bir istek bu belirteclerden bazilarini zaten iptal etti
        }
    }
}
