using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Shop.DTOs;
using Focus.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Shop.Commands;

public record BuyStreakFreezeCommand(Guid UserId) : IRequest<BuyFreezeResultDto>;

public class BuyStreakFreezeCommandHandler : IRequestHandler<BuyStreakFreezeCommand, BuyFreezeResultDto>
{
    private const int FreezePrice = 100;
    private readonly IApplicationDbContext _context;

    public BuyStreakFreezeCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BuyFreezeResultDto> Handle(BuyStreakFreezeCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null)
        {
            return new BuyFreezeResultDto(false, "Kullanıcı bulunamadı.", 0, 0);
        }

        var streak = await _context.UserStreaks.FirstOrDefaultAsync(s => s.UserId == request.UserId, cancellationToken);
        if (streak == null)
        {
            streak = UserStreak.CreateDefault(request.UserId);
            _context.UserStreaks.Add(streak);
        }

        var currentCoins = await _context.CoinLedgerEntries
            .Where(c => c.UserId == request.UserId)
            .SumAsync(c => (int?)c.Coins, cancellationToken) ?? 0;

        if (streak.FreezesAvailable >= 2)
        {
            return new BuyFreezeResultDto(
                false,
                "Zaten maksimum sayıda (2) Seri Dondurucuya sahipsiniz.",
                currentCoins,
                streak.FreezesAvailable);
        }

        if (currentCoins < FreezePrice)
        {
            return new BuyFreezeResultDto(
                false,
                $"Seri Dondurucu için {FreezePrice} Focus Coin gerekiyor. Mevcut bakiyeniz: {currentCoins}.",
                currentCoins,
                streak.FreezesAvailable);
        }

        var coinEntry = new CoinLedgerEntry(request.UserId, CoinTransactionReason.StreakFreezePurchase, -FreezePrice);
        _context.CoinLedgerEntries.Add(coinEntry);

        streak.AddFreeze(1);

        await _context.SaveChangesAsync(cancellationToken);

        var newBalance = currentCoins - FreezePrice;
        return new BuyFreezeResultDto(
            true,
            "1x Seri Dondurucu (Streak Freeze) başarıyla satın alındı! Bir gün çalışmayı kaçırırsanız seriniz sıfırlanmayacak.",
            newBalance,
            streak.FreezesAvailable);
    }
}
