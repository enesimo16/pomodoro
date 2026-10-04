using Focus.Domain.Common;

namespace Focus.Domain.Entities;

public enum CoinTransactionReason
{
    SessionReward = 1,
    BreakBonus = 2,
    ItemPurchase = 3,
    StreakReward = 4,
    InitialBonus = 5,
    StreakFreezePurchase = 6
}

public class CoinLedgerEntry : BaseEntity<Guid>
{
    public Guid UserId { get; private set; }
    public Guid? SessionId { get; private set; }
    public CoinTransactionReason Reason { get; private set; }
    public int Coins { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

    // EF Core constructor
    private CoinLedgerEntry() { }

    public CoinLedgerEntry(Guid userId, CoinTransactionReason reason, int coins, Guid? sessionId = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Reason = reason;
        Coins = coins;
        SessionId = sessionId;
        CreatedAt = DateTime.UtcNow;
    }
}
