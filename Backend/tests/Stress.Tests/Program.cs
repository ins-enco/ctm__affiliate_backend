using Stress.Tests;

await using var factory = new StressWebFactory();
var client = factory.CreateClient();

Console.WriteLine("[Stress] API started in-process → MySQL copytrade_stress_db");

// ── Resolve affiliate code ────────────────────────────────────────────────────
var affiliateCode = Environment.GetEnvironmentVariable("STRESS_AFFILIATE_CODE");

if (string.IsNullOrWhiteSpace(affiliateCode))
{
    Console.WriteLine("[Stress] Registering a dedicated stress affiliate...");

    var email = $"stress_{Guid.NewGuid():N}@test.com";

    var register = await client.PostAsJsonAsync("/api/auth/register", new
    {
        name     = "Stress Tester",
        email,
        password = "StressPass1!"
    });

    if (!register.IsSuccessStatusCode)
    {
        Console.Error.WriteLine($"[Stress] Registration failed: {register.StatusCode}");
        return 1;
    }

    var auth = await register.Content.ReadFromJsonAsync<AuthResult>();
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.Token);

    var dash      = await client.GetFromJsonAsync<DashboardResult>("/api/affiliate/dashboard");
    affiliateCode = dash!.UniqueCode;

    Console.WriteLine($"[Stress] Registered: {affiliateCode}  (email: {email})");
}
else
{
    Console.WriteLine($"[Stress] Using supplied affiliate code: {affiliateCode}");
}

// ── Start resource monitor ────────────────────────────────────────────────────
await using var monitor = new ResourceMonitor(StressWebFactory.StressConnectionString);
Console.WriteLine("[Stress] Resource monitor started (CPU / Memory / Threads / DB connections)");

// ── Run scenarios ─────────────────────────────────────────────────────────────
var scenarios = new[]
{
    ClickScenarios.RandomMix(affiliateCode),
};

var results = new List<ScenarioResult>();

foreach (var spec in scenarios)
{
    monitor.MarkScenario(spec.Name);
    var result = await StressRunner.RunAsync(spec, client);
    results.Add(result);
    PrintResult(result);
}

// ── Save HTML report ──────────────────────────────────────────────────────────
var reportPath = HtmlReportWriter.Write(results, monitor, "stress-report");
Console.WriteLine($"\n[Stress] Report saved → {reportPath}");
return 0;

// ── Helpers ───────────────────────────────────────────────────────────────────
static void PrintResult(ScenarioResult r)
{
    Console.WriteLine($"""

    ┌─ {r.Name} ──────────────────────────────
    │  Total    : {r.Total,8:N0}
    │  200 OK   : {r.Success,8:N0}   ({r.Success * 100.0 / r.Total:F1} %)
    │  404      : {r.NotFound,8:N0}   ({r.NotFound * 100.0 / r.Total:F1} %)  ← non-existent affiliate
    │  Errors   : {r.Failed,8:N0}   ({r.Failed * 100.0 / r.Total:F1} %)
    │  RPS      : {r.Throughput,8:F1}
    │  Latency  : Min={r.Min}ms  P50={r.P50}ms  P95={r.P95}ms  P99={r.P99}ms  Max={r.Max}ms
    └──────────────────────────────────────────
    """);
}

internal sealed record AuthResult(string Token, int AffiliateId);
internal sealed record DashboardResult(string UniqueCode, string AffiliateName,
                                       int TotalClicks, int UniqueClicks, int Last7DayClicks);
