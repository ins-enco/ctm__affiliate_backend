using System.Net.Http.Headers;
using Stress.Tests;

// ── Thread A: Web host ────────────────────────────────────────────────────────
// Dedicated OS thread — keeps the factory + TestServer alive independently
// from the stress thread so request processing doesn't compete for the
// same thread pool slots as the task spawning loop.

await using var factory = new StressWebFactory();

// Resolve affiliate code on the web thread (needs HTTP to the server)
var affiliateCode = Environment.GetEnvironmentVariable("STRESS_AFFILIATE_CODE");

if (string.IsNullOrWhiteSpace(affiliateCode))
{
    Console.WriteLine("[Web ] Registering stress affiliate...");
    using var setupClient = factory.CreateClient();

    var email    = $"stress_{Guid.NewGuid():N}@test.com";
    var register = await setupClient.PostAsJsonAsync("/api/auth/register", new
    {
        name = "Stress Tester", email, password = "StressPass1!"
    });

    if (!register.IsSuccessStatusCode)
    {
        Console.Error.WriteLine($"[Web ] Registration failed: {register.StatusCode}");
        return 1;
    }

    var auth = await register.Content.ReadFromJsonAsync<AuthResult>();
    setupClient.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", auth!.Token);

    var dash      = await setupClient.GetFromJsonAsync<DashboardResult>("/api/affiliate/dashboard");
    affiliateCode = dash!.UniqueCode;
    Console.WriteLine($"[Web ] Ready — affiliate: {affiliateCode}");
}
else
{
    Console.WriteLine($"[Web ] Using supplied affiliate code: {affiliateCode}");
}

// ── Thread B: Stress test ─────────────────────────────────────────────────────
// LongRunning = dedicated OS thread, never competes with thread pool workers.
// Each scenario gets its own HttpClient so headers don't bleed between runs.

await using var monitor = new ResourceMonitor(StressWebFactory.StressConnectionString);
Console.WriteLine("[Test] Resource monitor started\n");

var scenarios = new[]
{
    ClickScenarios.RandomMix1    (affiliateCode),
    ClickScenarios.RandomMix100  (affiliateCode),
};

var stressTask = Task.Factory.StartNew(async () =>
{
    var results = new List<ScenarioResult>();

    foreach (var spec in scenarios)
    {
        using var client = factory.CreateClient(); // fresh client per scenario
        monitor.MarkScenario(spec.Name);
        var result = await StressRunner.RunAsync(spec, client, monitor);
        results.Add(result);
        PrintResult(result);
    }

    return results;

}, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();

var results = await stressTask;

// ── Report ────────────────────────────────────────────────────────────────────
var reportPath = HtmlReportWriter.Write(results, monitor, "stress-report");
Console.WriteLine($"\n[Test] Report → {reportPath}");
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
