using System.Collections.Concurrent;
using System.Net;

namespace Stress.Tests.Runner;

public sealed record ScenarioSpec(
    string Name,
    Func<int, HttpRequestMessage> RequestFactory,
    int RatePerSecond,
    TimeSpan Warmup,
    TimeSpan Duration);

public static class StressRunner
{
    public static async Task<ScenarioResult> RunAsync(ScenarioSpec spec, HttpClient client)
    {
        Console.WriteLine($"\n[{spec.Name}] Warming up for {spec.Warmup.TotalSeconds}s ...");
        await FireAsync(spec, client, spec.Warmup, discard: true);

        Console.WriteLine($"[{spec.Name}] Running for {spec.Duration.TotalSeconds}s at {spec.RatePerSecond:N0} req/s ...");
        var (latencies, success, notFound, failed) = await FireAsync(spec, client, spec.Duration, discard: false);

        var sorted = latencies.ToArray();
        Array.Sort(sorted);

        return new ScenarioResult(spec.Name, sorted, success, notFound, failed, spec.Duration);
    }

    private static async Task<(ConcurrentBag<long> Latencies, int Success, int NotFound, int Failed)> FireAsync(
        ScenarioSpec spec, HttpClient client, TimeSpan duration, bool discard)
    {
        var latencies = new ConcurrentBag<long>();
        var success   = 0;
        var notFound  = 0;
        var failed    = 0;
        var counter   = 0;

        using var cts   = new CancellationTokenSource(duration);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        try
        {
            while (await timer.WaitForNextTickAsync(cts.Token))
            {
                var tasks = Enumerable.Range(0, spec.RatePerSecond).Select(_ =>
                {
                    var idx = Interlocked.Increment(ref counter);
                    return Task.Run(async () =>
                    {
                        using var req = spec.RequestFactory(idx);
                        var sw        = Stopwatch.GetTimestamp();
                        var response  = await client.SendAsync(req);
                        var ms        = Stopwatch.GetElapsedTime(sw).Milliseconds;

                        if (discard) return;

                        latencies.Add(ms);
                        switch (response.StatusCode)
                        {
                            case HttpStatusCode.OK:
                                Interlocked.Increment(ref success);
                                break;
                            case HttpStatusCode.NotFound:
                                Interlocked.Increment(ref notFound);
                                break;
                            default:
                                Interlocked.Increment(ref failed);
                                break;
                        }
                    });
                });

                await Task.WhenAll(tasks);
            }
        }
        catch (OperationCanceledException) { /* duration elapsed — normal exit */ }

        return (latencies, success, notFound, failed);
    }
}
