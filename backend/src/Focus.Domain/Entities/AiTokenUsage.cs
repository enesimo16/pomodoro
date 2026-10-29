namespace Focus.Domain.Entities;

using Focus.Domain.Common;

public class AiTokenUsage : BaseEntity<Guid>
{
    public Guid UserId { get; private set; }
    public DateOnly UsageDate { get; private set; }
    public int TokensUsed { get; private set; }
    public Guid ConcurrencyStamp { get; private set; }

    private AiTokenUsage() { }

    public static AiTokenUsage Create(Guid userId, DateOnly usageDate)
    {
        return new AiTokenUsage
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UsageDate = usageDate,
            TokensUsed = 0,
            ConcurrencyStamp = Guid.NewGuid()
        };
    }

    public bool TryConsume(int tokens, int dailyBudget)
    {
        if (tokens < 0 || (long)TokensUsed + tokens > dailyBudget)
        {
            return false;
        }

        TokensUsed += tokens;
        ConcurrencyStamp = Guid.NewGuid();
        return true;
    }
}
