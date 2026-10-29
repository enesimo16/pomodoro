using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Auth.DTOs;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Auth.Commands;

// GuestUserIdToClaim yalnizca dogrulanmis oturumdaki (JWT) misafir kimliginden gelmelidir; istemci govdesine guvenilmez
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

        if (!googleInfo.EmailVerified || string.IsNullOrWhiteSpace(googleInfo.Email) || string.IsNullOrWhiteSpace(googleInfo.Subject))
        {
            throw new UnauthorizedAccessException("Google hesabının e-posta adresi doğrulanmamış.");
        }

        try
        {
            return await SignInAsync(request, googleInfo, cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Es zamanli ilk giriste benzersizlik ihlali olabilir; bekleyen degisiklikler atilip kayit yeniden okunur
            _context.DiscardPendingChanges();
            return await SignInAsync(request, googleInfo, cancellationToken);
        }
    }

    private async Task<AuthResponseDto> SignInAsync(GoogleLoginCommand request, GoogleUserInfo googleInfo, CancellationToken cancellationToken)
    {
        var normalizedEmail = googleInfo.Email.ToLowerInvariant().Trim();

        var user = await FindUserAsync(u => u.ExternalLogins.Any(l => l.Provider == AuthProvider.Google && l.ProviderKey == googleInfo.Subject), cancellationToken);

        if (user == null)
        {
            user = await FindUserAsync(u => u.Email == normalizedEmail, cancellationToken);

            if (user != null)
            {
                if (user.ExternalLogins.Any(l => l.Provider == AuthProvider.Google))
                {
                    throw new UnauthorizedAccessException("Bu e-posta başka bir Google hesabına bağlı.");
                }

                _context.UserExternalLogins.Add(new UserExternalLogin(user.Id, AuthProvider.Google, googleInfo.Subject, normalizedEmail));
            }
        }

        var guestUser = request.GuestUserIdToClaim.HasValue && request.GuestUserIdToClaim.Value != user?.Id
            ? await FindUserAsync(u => u.Id == request.GuestUserIdToClaim.Value && u.IsGuest, cancellationToken)
            : null;

        User finalUser;

        if (user != null)
        {
            finalUser = user;

            if (guestUser != null)
            {
                await MergeGuestAsync(guestUser, finalUser, request.IpAddress, cancellationToken);
            }
        }
        else if (guestUser != null)
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

        var (accessToken, refreshToken, expiresAt) = _jwtTokenService.GenerateTokens(finalUser);
        var tokenHash = _jwtTokenService.HashToken(refreshToken);

        _context.RefreshTokens.Add(new RefreshToken(finalUser.Id, tokenHash, DateTime.UtcNow.Add(_jwtTokenService.RefreshTokenLifetime), request.IpAddress));

        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(
            UserDto.FromEntity(finalUser),
            finalUser.Avatar != null ? AvatarDto.FromEntity(finalUser.Avatar) : null,
            finalUser.Room != null ? PixelRoomDto.FromEntity(finalUser.Room) : null,
            accessToken,
            refreshToken,
            expiresAt);
    }

    private Task<User?> FindUserAsync(System.Linq.Expressions.Expression<Func<User, bool>> predicate, CancellationToken cancellationToken)
    {
        return _context.Users
            .Include(u => u.Avatar)
            .Include(u => u.Room)
                .ThenInclude(r => r!.Items)
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(predicate, cancellationToken);
    }

    // Yalnizca kazanilmis XP ve pozitif coin aktarilir; hos geldin bonusu aktarilmaz (misafir hesap acarak coin cikarmayi onler)
    private async Task MergeGuestAsync(User guestUser, User targetUser, string? ipAddress, CancellationToken cancellationToken)
    {
        if (guestUser.CurrentXp > 0)
        {
            targetUser.AddXp(guestUser.CurrentXp);
        }

        var earnedCoins = await _context.CoinLedgerEntries
            .Where(c => c.UserId == guestUser.Id && c.Reason != CoinTransactionReason.InitialBonus && c.Coins > 0)
            .ToListAsync(cancellationToken);

        foreach (var coin in earnedCoins)
        {
            _context.CoinLedgerEntries.Add(new CoinLedgerEntry(targetUser.Id, coin.Reason, coin.Coins, coin.SessionId));
        }

        var guestTokens = await _context.RefreshTokens
            .Where(t => t.UserId == guestUser.Id && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in guestTokens)
        {
            token.Revoke(ipAddress);
        }

        guestUser.SoftDelete();
    }
}
