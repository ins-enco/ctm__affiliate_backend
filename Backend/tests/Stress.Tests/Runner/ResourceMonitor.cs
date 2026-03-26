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
        VerifyDbConnection();

        // Dedicated OS thread — not thread pool — so starvation from
        // Task.WhenAll floods never prevents the monitor from sampling.
        _bgTask = Task.Factory.StartNew(
            () => SampleLoop(_cts.Token),
            _cts.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    private void VerifyDbConnection()
    {
        if (_connectionString is null)
        {
            Console.WriteLine("[Monitor] DB monitoring disabled — no connection string.");
            return;
        }
        try
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SHOW GLOBAL STATUS WHERE Variable_name IN " +
                "('Questions','Threads_connected','Innodb_rows_inserted')";
            using var reader = cmd.ExecuteReader();
            Console.WriteLine($"[Monitor] DB connected ✓  {_connectionString[.._connectionString.IndexOf(';')]}");
            while (reader.Read())
                Console.WriteLine($"[Monitor]   {reader.GetString(0)} = {reader.GetString(1)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Monitor] DB connection FAILED — DB metrics will show 0.");
            Console.WriteLine($"          {ex.Message}");
        }
    }

    public void MarkScenario(string name) =>
        _markers.Add((_sw.Elapsed.TotalSeconds, name));

    // Runs on a dedicated OS thread (LongRunning) — immune to thread pool starvation.
    // Uses Thread.Sleep instead of PeriodicTimer so it never yields back to the pool.
    private void SampleLoop(CancellationToken ct)
    {
        var process   = Process.GetCurrentProcess();
        var prevCpu   = process.TotalProcessorTime;
        var prevSecs  = _sw.Elapsed.TotalSeconds;
        var prevQuestions = QuerySingleLong("Questions");
        var prevInserts   = QueryClickCount();

        while (!ct.IsCancellationRequested)
        {
            Thread.Sleep(1000);
            if (ct.IsCancellationRequested) break;

            process.Refresh();
            var now    = _sw.Elapsed.TotalSeconds;
            var curCpu = process.TotalProcessorTime;

            var cpuDeltaMs  = (curCpu - prevCpu).TotalMilliseconds;
            var timeDeltaMs = (now - prevSecs) * 1_000;
            var cpuPct      = timeDeltaMs > 0
                ? Math.Min(cpuDeltaMs / (timeDeltaMs * Environment.ProcessorCount) * 100, 100)
                : 0;

            var (connected, running, questions) = QueryDbStatus();
            var clicks  = QueryClickCount();
            var qps = Math.Max(0, questions - prevQuestions);
            var ips = Math.Max(0, clicks    - prevInserts);

            _samples.Add(new ResourceSample(
                ElapsedSeconds:         Math.Round(now, 1),
                CpuPercent:             Math.Round(cpuPct, 1),
                WorkingSetMb:           process.WorkingSet64 / 1_048_576,
                GcHeapMb:               GC.GetTotalMemory(false) / 1_048_576,
                ThreadPoolThreadCount:  ThreadPool.ThreadCount,
                ThreadPoolPendingItems: ThreadPool.PendingWorkItemCount,
                DbThreadsConnected:     connected,
                DbThreadsRunning:       running,
                DbQueriesPerSec:        qps,
                DbInsertsPerSec:        ips
            ));

            prevCpu       = curCpu;
            prevSecs      = now;
            prevQuestions = questions;
            prevInserts   = clicks;
        }
    }

    private (int Connected, int Running, long Questions) QueryDbStatus()
    {
        if (_connectionString is null) return (0, 0, 0);
        try
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SHOW GLOBAL STATUS WHERE Variable_name IN " +
                "('Threads_connected','Threads_running','Questions')";
            using var reader = cmd.ExecuteReader();

            int connected = 0, running = 0;
            long questions = 0;
            while (reader.Read())
            {
                switch (reader.GetString(0))
                {
                    case "Threads_connected": connected = int.Parse(reader.GetString(1));  break;
                    case "Threads_running":   running   = int.Parse(reader.GetString(1));  break;
                    case "Questions":         questions = long.Parse(reader.GetString(1)); break;
                }
            }
            return (connected, running, questions);
        }
        catch { return (0, 0, 0); }
    }

    private long QueryClickCount()
    {
        if (_connectionString is null) return 0;
        try
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM click_events";
            return Convert.ToInt64(cmd.ExecuteScalar());
        }
        catch { return 0; }
    }

    private long QuerySingleLong(string variable)
    {
        if (_connectionString is null) return 0;
        try
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SHOW GLOBAL STATUS WHERE Variable_name = '{variable}'";
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? long.Parse(reader.GetString(1)) : 0;
        }
        catch { return 0; }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        try { await _bgTask; } catch (OperationCanceledException) { }
        _cts.Dispose();
    }
}
