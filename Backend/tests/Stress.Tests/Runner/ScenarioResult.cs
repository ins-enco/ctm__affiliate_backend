namespace Stress.Tests.Runner;

public sealed record ScenarioResult(
    string   Name,
    long[]   SortedLatenciesMs,
    int      Success,
    int      NotFound,   // expected 404 — non-existent affiliate codes
    int      Failed,     // unexpected errors (5xx, timeouts, etc.)
    TimeSpan Duration)
{
    public int    Total      => Success + NotFound + Failed;
    public double Throughput => Total / Duration.TotalSeconds;
    public long   Min => SortedLatenciesMs.Length > 0 ? SortedLatenciesMs[0]  : 0;
    public long   Max => SortedLatenciesMs.Length > 0 ? SortedLatenciesMs[^1] : 0;
    public long   P50 => Percentile(50);
    public long   P95 => Percentile(95);
    public long   P99 => Percentile(99);

    private long Percentile(int p)
    {
        if (SortedLatenciesMs.Length == 0) return 0;
        var idx = (int)Math.Ceiling(p / 100.0 * SortedLatenciesMs.Length) - 1;
        return SortedLatenciesMs[Math.Clamp(idx, 0, SortedLatenciesMs.Length - 1)];
    }
}
