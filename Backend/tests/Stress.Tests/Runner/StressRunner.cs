using System.Net;

namespace Stress.Tests.Runner;

public sealed record ScenarioSpec(
    string Name,
    Func<int, HttpRequestMessage> RequestFactory,
    int Concurrency,   // max in-flight requests at any time
    TimeSpan Warmup,
    TimeSpan Duration);

public static class StressRunner
{
    public static async Task<ScenarioResult> RunAsync(
        ScenarioSpec spec, HttpClient client, ResourceMonitor? monitor = null)
    {
        Console.WriteLine($"\n[{spec.Name}] Warming up {spec.Warmup.TotalSeconds}s  concurrency={spec.Concurrency:N0}");
        await FireAsync(spec, client, spec.Warmup, discard: true, monitor: null);

        Console.WriteLine($"[{spec.Name}] Running {spec.Duration.TotalSeconds}s  concurrency={spec.Concurrency:N0}");
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();

        var liveTop = Console.CursorTop - 6;

        var (latencies, success, notFound, failed) =
            await FireAsync(spec, client, spec.Duration, discard: false, monitor, liveTop, spec.Name);

        Console.SetCursorPosition(0, liveTop + 6);
        Array.Sort(latencies);

        return new ScenarioResult(spec.Name, latencies, success, notFound, failed, spec.Duration);
    }

    private const int MaxSamples = 100_000;

    private static async Task<(long[] Latencies, int Success, int NotFound, int Failed)> FireAsync(
        ScenarioSpec spec, HttpClient client, TimeSpan duration, bool discard,
        ResourceMonitor? monitor, int liveTop = 0, string scenarioName = "")
    {
        var latencies   = new long[MaxSamples];
        var sampleCount = 0;
        var success     = 0;
        var notFound    = 0;
        var failed      = 0;
        var counter     = 0;
        var sw          = Stopwatch.StartNew();
        var deadline    = duration;
        var sem         = new SemaphoreSlim(spec.Concurrency, spec.Concurrency);

        // ── Live reporter (dedicated OS thread) ───────────────────────────────
        using var reporterCts = new CancellationTokenSource();
        var reporterTask = Task.Factory.StartNew(() =>
        {
            if (discard)
            {
                while (!reporterCts.Token.IsCancellationRequested)
                {
                    Thread.Sleep(1000);
                    if (reporterCts.Token.IsCancellationRequested) break;
                    var wsMb     = Process.GetCurrentProcess().WorkingSet64 / 1_048_576;
                    var inFlight = spec.Concurrency - sem.CurrentCount;
                    Console.Write($"\r  warming up {sw.Elapsed:mm\\:ss} / {duration:mm\\:ss}" +
                                  $"  WS={wsMb}MB  InFlight={inFlight:N0}          ");
                }
                Console.WriteLine();
                return;
            }

            while (!reporterCts.Token.IsCancellationRequested)
            {
                Thread.Sleep(1000);
                if (reporterCts.Token.IsCancellationRequested) break;

                var elapsed  = sw.Elapsed;
                var ok       = Volatile.Read(ref success);
                var nf       = Volatile.Read(ref notFound);
                var err      = Volatile.Read(ref failed);
                var total    = ok + nf + err;
                var rps      = elapsed.TotalSeconds > 0 ? total / elapsed.TotalSeconds : 0;
                var inFlight = spec.Concurrency - sem.CurrentCount;

                var sample  = monitor?.Samples.Count > 0 ? monitor.Samples[^1] : null;
                var wsMb    = sample?.WorkingSetMb           ?? 0;
                var gcMb    = sample?.GcHeapMb               ?? 0;
                var osThd   = sample?.ThreadPoolThreadCount  ?? 0;
                var pending = sample?.ThreadPoolPendingItems ?? 0;
                var dbQps   = sample?.DbQueriesPerSec        ?? 0;
                var dbIps   = sample?.DbInsertsPerSec        ?? 0;
                var dbRun   = sample?.DbThreadsRunning       ?? 0;

                Console.SetCursorPosition(0, liveTop);
                Console.WriteLine($"┌─ {scenarioName}  {elapsed:mm\\:ss} / {duration:mm\\:ss}");
                Console.WriteLine($"│  Total    : {total,10:N0}   RPS: {rps,8:N1}   InFlight: {inFlight}");
                Console.WriteLine($"│  200 OK   : {ok,10:N0}   ({(total > 0 ? ok * 100.0 / total : 0):F1} %)");
                Console.WriteLine($"│  404      : {nf,10:N0}   ({(total > 0 ? nf * 100.0 / total : 0):F1} %)  ← non-existent affiliate");
                Console.WriteLine($"│  Errors   : {err,10:N0}   ({(total > 0 ? err * 100.0 / total : 0):F1} %)");
                Console.Write    ($"│  System   : WS={wsMb}MB  GC={gcMb}MB  Threads={osThd}  Pending={pending}  DB QPS={dbQps:N0}  Inserts={dbIps:N0}  Running={dbRun}");
            }
        }, reporterCts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        // ── Fire loop ─────────────────────────────────────────────────────────
        while (sw.Elapsed < deadline)
        {
            await sem.WaitAsync();

            if (sw.Elapsed >= deadline) { sem.Release(); break; }

            var idx = Interlocked.Increment(ref counter);
            _ = Task.Run(async () =>
            {
                try
                {
                    using var req = spec.RequestFactory(idx);
                    var t0        = Stopwatch.GetTimestamp();
                    var response  = await client.SendAsync(req);
                    var ms        = Stopwatch.GetElapsedTime(t0).Milliseconds;

                    if (discard) return;

                    var n = Interlocked.Increment(ref sampleCount);
                    if (n <= MaxSamples)
                        latencies[n - 1] = ms;
                    else
                    {
                        var j = Random.Shared.Next(n);
                        if (j < MaxSamples) latencies[j] = ms;
                    }

                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK:       Interlocked.Increment(ref success);  break;
                        case HttpStatusCode.NotFound: Interlocked.Increment(ref notFound); break;
                        default:                      Interlocked.Increment(ref failed);   break;
                    }
                }
                finally { sem.Release(); }
            });
        }

        // Drain: wait for all in-flight requests to finish
        for (var i = 0; i < spec.Concurrency; i++)
            await sem.WaitAsync();

        await reporterCts.CancelAsync();
        await reporterTask;

        var filled = Math.Min(sampleCount, MaxSamples);
        return (latencies[..filled], success, notFound, failed);
    }
}
