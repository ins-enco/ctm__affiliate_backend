namespace Stress.Tests.Runner;

public sealed record ResourceSample(
    double ElapsedSeconds,
    double CpuPercent,
    long   WorkingSetMb,
    long   GcHeapMb,
    // Thread pool
    int    ThreadPoolThreadCount,    // actual OS threads spawned in the pool
    long   ThreadPoolPendingItems,   // queued work items waiting for a thread
    // MySQL server-side
    int    DbThreadsConnected,       // total open connections (incl. idle pool)
    int    DbThreadsRunning,         // actively executing queries right now
    long   DbQueriesPerSec,          // delta of Questions counter → actual QPS
    long   DbInsertsPerSec);         // delta of Innodb_rows_inserted → inserts/sec
