namespace Stress.Tests.Scenarios;

public static class ClickScenarios
{
    public static ScenarioSpec RandomMix1      (string affiliateCode) => RandomMix(affiliateCode, 1);
    public static ScenarioSpec RandomMix100    (string affiliateCode) => RandomMix(affiliateCode, 100);
    public static ScenarioSpec RandomMix1K     (string affiliateCode) => RandomMix(affiliateCode, 1_000);
    public static ScenarioSpec RandomMix10K    (string affiliateCode) => RandomMix(affiliateCode, 10_000);
    public static ScenarioSpec RandomMix100K   (string affiliateCode) => RandomMix(affiliateCode, 100_000);

    private static ScenarioSpec RandomMix(string affiliateCode, int concurrency) =>
        new(
            Name:           $"random_mix_c{concurrency:N0}",
            RequestFactory: idx =>
            {
                var variant = Random.Shared.Next(4);
                var ip      = variant is 1 or 3 ? "77.77.77.77" : UniqueIp(idx);
                var agent   = variant is 1 or 2 ? "FixedBot/1.0" : $"Browser-{idx}";
                var code    = Random.Shared.Next(10) < 7
                    ? affiliateCode
                    : $"FAKE{idx % 100:D4}";

                var req = new HttpRequestMessage(HttpMethod.Get,
                    $"/api/tracking/click?affiliateCode={code}");
                req.Headers.Add("X-Forwarded-For", ip);
                req.Headers.TryAddWithoutValidation("User-Agent", agent);
                return req;
            },
            Concurrency: concurrency,
            Warmup:      TimeSpan.FromSeconds(5),
            Duration:    TimeSpan.FromSeconds(30));

    private static string UniqueIp(int idx) =>
        $"10.{idx / 65025 % 255}.{idx / 255 % 255}.{idx % 255}";
}
