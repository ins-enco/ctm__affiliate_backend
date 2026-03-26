using MySqlConnector;

namespace Stress.Tests.Runner;

public sealed class ResourceMonitor : IAsyncDisposable
{
    private readonly List<ResourceSample>                _samples = new();
    private readonly List<(double Elapsed, string Name)> _markers = new();
    private readonly CancellationTokenSource             _cts     = new();
    private readonly Stopwatch                           _sw      = Stopwatch.StartNew();
    private readonly Task                                _bgTask;
    private readonly string?                             _connectionString;

    public IReadOnlyList<ResourceSample>                Samples => _samples;
    public IReadOnlyList<(double Elapsed, string Name)> Markers => _markers;

    public ResourceMonitor(string? connectionString = null)
    {
        _connectionString = connectionString;
        _bgTask = SampleLoopAsync(_cts.Token);
    }

    public void MarkScenario(string name) =>
        _markers.Add((_sw.Elapsed.TotalSeconds, name));

    private async Task SampleLoopAsync(CancellationToken ct)
    {
        var process  = Process.GetCurrentProcess();
        var prevCpu  = process.TotalProcessorTime;
        var prevSecs = _sw.Elapsed.TotalSeconds;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                process.Refresh();
                var now    = _sw.Elapsed.TotalSeconds;
                var curCpu = process.TotalProcessorTime;

                var cpuDeltaMs  = (curCpu - prevCpu).TotalMilliseconds;
                var timeDeltaMs = (now - prevSecs) * 1_000;
                var cpuPct      = timeDeltaMs > 0
                    ? Math.Min(cpuDeltaMs / (timeDeltaMs * Environment.ProcessorCount) * 100, 100)
                    : 0;

                var (connected, running) = await QueryDbStatusAsync();

                _samples.Add(new ResourceSample(
                    ElapsedSeconds:        Math.Round(now, 1),
                    CpuPercent:            Math.Round(cpuPct, 1),
                    WorkingSetMb:          process.WorkingSet64 / 1_048_576,
                    GcHeapMb:              GC.GetTotalMemory(false) / 1_048_576,
                    ThreadPoolThreadCount: ThreadPool.ThreadCount,
                    ThreadPoolPendingItems: ThreadPool.PendingWorkItemCount,
                    DbThreadsConnected:    connected,
                    DbThreadsRunning:      running
                ));

                prevCpu  = curCpu;
                prevSecs = now;
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task<(int Connected, int Running)> QueryDbStatusAsync()
    {
        if (_connectionString is null) return (0, 0);
        try
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SHOW STATUS WHERE Variable_name IN ('Threads_connected','Threads_running')";
            await using var reader = await cmd.ExecuteReaderAsync();

            int connected = 0, running = 0;
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(0);
                var val  = reader.GetInt32(1);
                if (name == "Threads_connected") connected = val;
                else if (name == "Threads_running")  running   = val;
            }
            return (connected, running);
        }
        catch { return (0, 0); }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        try { await _bgTask; } catch (OperationCanceledException) { }
        _cts.Dispose();
    }
}
