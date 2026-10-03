using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Auth.DTOs;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Auth.Commands;

public record GoogleLoginCommand(
    string IdToken,
    Guid? GuestUserIdToClaim = null,
    string? IpAddress = null) : IRequest<AuthResponseDto>;

public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, AuthResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IJwtTokenService _jwtTokenService;

    public GoogleLoginCommandHandler(
        IApplicationDbContext context,
        IGoogleAuthService googleAuthService,
        IJwtTokenService jwtTokenService)
    {
        _context = context;
        _googleAuthService = googleAuthService;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponseDto> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        var googleInfo = await _googleAuthService.ValidateTokenAsync(request.IdToken, cancellationToken);
        if (googleInfo == null)
        {
            throw new UnauthorizedAccessException("Geçersiz veya süresi dolmuş Google kimlik belirteci.");
        }

        var normalizedEmail = googleInfo.Email.ToLowerInvariant().Trim();

        // Kullanıcı önceden kayıtlı mı kontrol et
        var existingUser = await _context.Users
            .Include(u => u.Avatar)
            .Include(u => u.Room)
                .ThenInclude(r => r!.Items)
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail ||
                                      u.ExternalLogins.Any(l => l.Provider == AuthProvider.Google && l.ProviderKey == googleInfo.Subject),
                                cancellationToken);

        User finalUser;

        if (existingUser != null)
        {
            finalUser = existingUser;

            // Eğer misafir hesabından geçiş yapılıyorsa ve misafir farklıysa, coin ve ilerlemeyi aktar
            if (request.GuestUserIdToClaim.HasValue && request.GuestUserIdToClaim.Value != existingUser.Id)
            {
                var guestUser = await _context.Users
                    .Include(u => u.Room)
                    .FirstOrDefaultAsync(u => u.Id == request.GuestUserIdToClaim.Value && u.IsGuest, cancellationToken);

                if (guestUser != null)
                {
                    // Misafirin XP'sini ekle
                    if (guestUser.CurrentXp > 0)
                    {
                        finalUser.AddXp(guestUser.CurrentXp);
                    }

                    // Misafir coin kayıtlarını bu kullanıcıya bağla
                    var guestCoins = await _context.CoinLedgerEntries
                        .Where(c => c.UserId == guestUser.Id)
                        .ToListAsync(cancellationToken);

                    foreach (var coin in guestCoins)
                    {
                        _context.CoinLedgerEntries.Add(new CoinLedgerEntry(finalUser.Id, coin.Reason, coin.Coins, coin.SessionId));
                    }

                    guestUser.SoftDelete();
                }
            }
        }
        else
        {
            // Yeni kullanıcı veya misafir hesabı kalıcı hesaba dönüştürme
            if (request.GuestUserIdToClaim.HasValue)
            {
                var guestUser = await _context.Users
                    .Include(u => u.Avatar)
                    .Include(u => u.Room)
                        .ThenInclude(r => r!.Items)
                    .Include(u => u.ExternalLogins)
                    .FirstOrDefaultAsync(u => u.Id == request.GuestUserIdToClaim.Value && u.IsGuest, cancellationToken);

                if (guestUser != null)
                {
                    guestUser.ClaimWithGoogle(normalizedEmail, googleInfo.Name);
                    _context.UserExternalLogins.Add(new UserExternalLogin(guestUser.Id, AuthProvider.Google, googleInfo.Subject, normalizedEmail));
                    finalUser = guestUser;
                }
                else
                {
                    finalUser = User.CreateGoogleUser(normalizedEmail, googleInfo.Name, googleInfo.Subject);
                    _context.Users.Add(finalUser);
                    _context.CoinLedgerEntries.Add(new CoinLedgerEntry(finalUser.Id, CoinTransactionReason.InitialBonus, 50));
                }
            }
            else
            {
                finalUser = User.CreateGoogleUser(normalizedEmail, googleInfo.Name, googleInfo.Subject);
                _context.Users.Add(finalUser);
                _context.CoinLedgerEntries.Add(new CoinLedgerEntry(finalUser.Id, CoinTransactionReason.InitialBonus, 50));
            }
        }

        var (accessToken, refreshToken, expiresAt) = _jwtTokenService.GenerateTokens(finalUser);
        var tokenHash = _jwtTokenService.HashToken(refreshToken);

        var refreshTokenRecord = new RefreshToken(finalUser.Id, tokenHash, DateTime.UtcNow.AddDays(30), request.IpAddress);
        _context.RefreshTokens.Add(refreshTokenRecord);

        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(
            UserDto.FromEntity(finalUser),
            finalUser.Avatar != null ? AvatarDto.FromEntity(finalUser.Avatar) : null,
            finalUser.Room != null ? PixelRoomDto.FromEntity(finalUser.Room) : null,
            accessToken,
            refreshToken,
            expiresAt);
    }
}
