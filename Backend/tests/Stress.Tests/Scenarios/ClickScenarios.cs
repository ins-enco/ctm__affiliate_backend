namespace Stress.Tests.Scenarios;

public static class ClickScenarios
{
    // ── Scenario 1: Unique clicks ─────────────────────────────────────────────
    // Diff IP, default UA → unique SHA256(ip+ua+code) per request.
    // All should be IsUnique = true.
    public static ScenarioSpec UniqueClicks(string affiliateCode) =>
        new(
            Name:           "unique_clicks",
            RequestFactory: idx =>
            {
                var req = new HttpRequestMessage(HttpMethod.Get,
                    $"/api/tracking/click?affiliateCode={affiliateCode}");
                req.Headers.Add("X-Forwarded-For", UniqueIp(idx));
                return req;
            },
            RatePerSecond: 50,
            Warmup:        TimeSpan.FromSeconds(5),
            Duration:      TimeSpan.FromSeconds(30));

    // ── Scenario 2: Duplicate flood ───────────────────────────────────────────
    // Same IP, same UA → same session ID every time.
    // Only the first insert succeeds; rest return IsUnique = false (HTTP 200).
    // Tests deduplication throughput and DB index under concurrent load.
    public static ScenarioSpec DuplicateFlood(string affiliateCode) =>
        new(
            Name:           "duplicate_flood",
            RequestFactory: _ =>
            {
                var req = new HttpRequestMessage(HttpMethod.Get,
                    $"/api/tracking/click?affiliateCode={affiliateCode}");
                req.Headers.Add("X-Forwarded-For", "99.99.99.99");
                req.Headers.TryAddWithoutValidation("User-Agent", "StressBot/1.0");
                return req;
            },
            RatePerSecond: 50,
            Warmup:        TimeSpan.FromSeconds(5),
            Duration:      TimeSpan.FromSeconds(30));

    // ── Scenario 3: High volume (10 000 requests) ─────────────────────────────
    // Diff IP, default UA → all unique sessions.
    // 200 req/s × 50 s = 10 000 total requests.
    // Measures max throughput and latency under sustained load.
    public static ScenarioSpec HighVolume(string affiliateCode) =>
        new(
            Name:           "high_volume",
            RequestFactory: idx =>
            {
                var req = new HttpRequestMessage(HttpMethod.Get,
                    $"/api/tracking/click?affiliateCode={affiliateCode}");
                req.Headers.Add("X-Forwarded-For", UniqueIp(idx));
                return req;
            },
            RatePerSecond: 10000,
            Warmup:        TimeSpan.FromSeconds(5),
            Duration:      TimeSpan.FromSeconds(50));

    // ── Scenario 4: Same IP + Same Agent (bot replay) ─────────────────────────
    // SHA256("11.11.11.11" + "BotReplay/1.0" + code) is constant.
    // Every request after the first hits the duplicate key → IsUnique = false.
    public static ScenarioSpec SameIpSameAgent(string affiliateCode) =>
        new(
            Name:           "same_ip_same_agent",
            RequestFactory: _ =>
            {
                var req = new HttpRequestMessage(HttpMethod.Get,
                    $"/api/tracking/click?affiliateCode={affiliateCode}");
                req.Headers.Add("X-Forwarded-For", "11.11.11.11");
                req.Headers.TryAddWithoutValidation("User-Agent", "BotReplay/1.0");
                return req;
            },
            RatePerSecond: 50,
            Warmup:        TimeSpan.FromSeconds(5),
            Duration:      TimeSpan.FromSeconds(30));

    // ── Scenario 5: Diff IP + Same Agent ─────────────────────────────────────
    // SHA256 changes each request (IP differs) even though UA is fixed.
    // All clicks should be IsUnique = true — validates that UA alone doesn't
    // cause false deduplication.
    public static ScenarioSpec DiffIpSameAgent(string affiliateCode) =>
        new(
            Name:           "diff_ip_same_agent",
            RequestFactory: idx =>
            {
                var req = new HttpRequestMessage(HttpMethod.Get,
                    $"/api/tracking/click?affiliateCode={affiliateCode}");
                req.Headers.Add("X-Forwarded-For", UniqueIp(idx));
                req.Headers.TryAddWithoutValidation("User-Agent", "SharedBrowser/1.0");
                return req;
            },
            RatePerSecond: 50,
            Warmup:        TimeSpan.FromSeconds(5),
            Duration:      TimeSpan.FromSeconds(30));

    // ── Scenario 6: Random Mix — 100 000 req/s ───────────────────────────────
    // Each request randomly picks one of 4 variants (25 % each):
    //   0 → diff IP  + diff Agent  → unique session
    //   1 → same IP  + same Agent  → duplicate (dedup path)
    //   2 → diff IP  + same Agent  → unique session
    //   3 → same IP  + diff Agent  → unique session
    // No per-request variant logging — just raw throughput + latency.
    public static ScenarioSpec RandomMix(string affiliateCode) =>
        new(
            Name:           "random_mix_100k",
            RequestFactory: idx =>
            {
                // ── IP / Agent variant (4 combinations) ──────────────────────
                var variant = Random.Shared.Next(4);
                var ip      = variant is 1 or 3 ? "77.77.77.77" : UniqueIp(idx);
                var agent   = variant is 1 or 2 ? "FixedBot/1.0" : $"Browser-{idx}";

                // ── Affiliate code: 70 % real → 200 OK, 30 % fake → 404 ─────
                var code = Random.Shared.Next(10) < 7
                    ? affiliateCode
                    : $"FAKE{idx % 100:D4}";   // pool of 100 non-existent codes

                var req = new HttpRequestMessage(HttpMethod.Get,
                    $"/api/tracking/click?affiliateCode={code}");
                req.Headers.Add("X-Forwarded-For", ip);
                req.Headers.TryAddWithoutValidation("User-Agent", agent);
                return req;
            },
            RatePerSecond: 100_000,
            Warmup:        TimeSpan.FromSeconds(5),
            Duration:      TimeSpan.FromSeconds(30));

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static string UniqueIp(int idx) =>
        $"10.{idx / 65025 % 255}.{idx / 255 % 255}.{idx % 255}";
}
