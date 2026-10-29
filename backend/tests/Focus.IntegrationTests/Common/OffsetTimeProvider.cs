namespace Focus.IntegrationTests.Common;

// Gercek saate eklenen ofset ile zaman ilerletilir; testler dakikalarca beklemeden seans surelerini simule eder
public sealed class OffsetTimeProvider : TimeProvider
{
    private long _offsetTicks;

    public override DateTimeOffset GetUtcNow() => System.GetUtcNow().AddTicks(Interlocked.Read(ref _offsetTicks));

    public void Advance(TimeSpan by) => Interlocked.Add(ref _offsetTicks, by.Ticks);
}
