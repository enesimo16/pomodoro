using Focus.Application.Common.Options;
using Focus.Application.Features.Session.Commands;
using Focus.Application.Features.Session.Services;
using Focus.Infrastructure.Persistence;

namespace Focus.UnitTests.Common;

public sealed class TestClock : TimeProvider
{
    private DateTimeOffset _now;

    public TestClock(DateTime? startUtc = null)
    {
        _now = new DateTimeOffset(startUtc ?? new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);
    }

    public DateTime UtcNow => _now.UtcDateTime;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now = _now.Add(by);
}

// Handler'lari gercek ayarlar ve kontrol edilebilir saatle olusturur
public sealed class SessionTestKit
{
    public SessionTestKit(ApplicationDbContext context, TestClock? clock = null, AlgorithmOptions? options = null)
    {
        Context = context;
        Clock = clock ?? new TestClock();
        Options = options ?? new AlgorithmOptions();
        Settlement = new SessionSettlementService(context, Microsoft.Extensions.Options.Options.Create(Options));
    }

    public ApplicationDbContext Context { get; }
    public TestClock Clock { get; }
    public AlgorithmOptions Options { get; }
    public SessionSettlementService Settlement { get; }

    public StartSessionCommandHandler StartHandler => new(Context, Settlement, Clock);
    public PauseSessionCommandHandler PauseHandler => new(Context, Settlement, Clock);
    public ResumeSessionCommandHandler ResumeHandler => new(Context, Settlement, Clock);
    public CompleteSessionCommandHandler CompleteHandler => new(Context, Settlement, Clock);
    public AbandonSessionCommandHandler AbandonHandler => new(Context, Settlement, Clock);
    public ExtendSessionCommandHandler ExtendHandler => new(Context, Microsoft.Extensions.Options.Options.Create(Options), Clock);
}
