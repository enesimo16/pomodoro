namespace Focus.Application.Common.Interfaces;

public interface IAiUsageService
{
    // Gunluk jeton butcesinden atomik olarak rezerve eder; butce asilirsa false doner
    Task<bool> TryConsumeAsync(Guid userId, int tokens, CancellationToken cancellationToken = default);

    // Rezervasyon yapar, butce asilirsa AiBudgetExceededException firlatir
    Task ConsumeOrThrowAsync(Guid userId, int tokens, CancellationToken cancellationToken = default);
}
