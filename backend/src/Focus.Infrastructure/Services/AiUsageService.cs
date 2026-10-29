using Focus.Application.Common.Exceptions;
using Focus.Application.Common.Interfaces;
using Focus.Domain.Entities;
using Focus.Infrastructure.Common;
using Focus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Focus.Infrastructure.Services;

public class AiUsageService : IAiUsageService
{
    private const int MaxAttempts = 5;

    private readonly ApplicationDbContext _context;
    private readonly AiSettings _settings;
    private readonly TimeProvider _timeProvider;

    public AiUsageService(ApplicationDbContext context, IOptions<AiSettings> settings, TimeProvider timeProvider)
    {
        _context = context;
        _settings = settings.Value;
        _timeProvider = timeProvider;
    }

    public async Task ConsumeOrThrowAsync(Guid userId, int tokens, CancellationToken cancellationToken = default)
    {
        if (!await TryConsumeAsync(userId, tokens, cancellationToken))
        {
            throw new AiBudgetExceededException();
        }
    }

    public async Task<bool> TryConsumeAsync(Guid userId, int tokens, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var usage = await _context.AiTokenUsages
                .FirstOrDefaultAsync(u => u.UserId == userId && u.UsageDate == today, cancellationToken);

            var isNew = usage == null;
            usage ??= AiTokenUsage.Create(userId, today);

            if (!usage.TryConsume(tokens, _settings.DailyTokenBudgetPerUser))
            {
                return false;
            }

            if (isNew)
            {
                _context.AiTokenUsages.Add(usage);
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Eszamanli bir istek ayni satiri guncelledi; guncel degerle yeniden dene
                foreach (var entry in ex.Entries)
                {
                    await entry.ReloadAsync(cancellationToken);
                }
            }
            catch (DbUpdateException) when (isNew)
            {
                // Ayni gun icin satir eszamanli olarak olusturuldu; eklenen nesneyi birak ve yeniden oku
                _context.Entry(usage).State = EntityState.Detached;
            }
        }

        return false;
    }
}
