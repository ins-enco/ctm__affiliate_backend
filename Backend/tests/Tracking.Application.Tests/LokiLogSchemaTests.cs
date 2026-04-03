namespace Tracking.Application.Tests;

/// <summary>
/// Verifies the structured log contract for Grafana/Loki:
/// - PII fields (IpAddress, UserAgent, Email) are absent from log messages
/// - Required business properties (EventType, AffiliateCode, SessionId) are present
/// </summary>
public class LokiLogSchemaTests
{
    private static (TrackingService service, List<(LogLevel level, string message)> logEntries) BuildService()
    {
        var db = new TrackingDbContext(
            new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<TrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        var affiliateLookup = new Mock<IAffiliateLookupService>();
        affiliateLookup
            .Setup(x => x.FindByCodeAsync("TEST0001"))
            .ReturnsAsync((1, "TEST0001"));

        var cache = new Mock<ICacheService>();
        cache
            .Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<(int, string)?>>>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync((1, "TEST0001"));

        var logEntries = new List<(LogLevel, string)>();
        var logger = new CapturingLogger<TrackingService>(logEntries);

        var service = new TrackingService(db, affiliateLookup.Object, cache.Object, logger);
        return (service, logEntries);
    }

    [Fact]
    public async Task RecordClickAsync_LogEvent_DoesNotContainIpAddress()
    {
        var (service, log) = BuildService();

        await service.RecordClickAsync("TEST0001", "192.168.1.1", "Mozilla/5.0", null);

        var infoMessages = log.Where(e => e.level == LogLevel.Information).Select(e => e.message).ToList();
        Assert.NotEmpty(infoMessages);
        Assert.DoesNotContain(infoMessages, m => m.Contains("192.168.1.1"));
        Assert.DoesNotContain(infoMessages, m => m.Contains("IpAddress"));
    }

    [Fact]
    public async Task RecordClickAsync_LogEvent_DoesNotContainUserAgent()
    {
        var (service, log) = BuildService();

        await service.RecordClickAsync("TEST0001", "192.168.1.1", "Mozilla/5.0", null);

        var infoMessages = log.Where(e => e.level == LogLevel.Information).Select(e => e.message).ToList();
        Assert.NotEmpty(infoMessages);
        Assert.DoesNotContain(infoMessages, m => m.Contains("Mozilla"));
        Assert.DoesNotContain(infoMessages, m => m.Contains("UserAgent"));
    }

    [Fact]
    public async Task RecordClickAsync_LogEvent_DoesNotContainEmail()
    {
        var (service, log) = BuildService();

        await service.RecordClickAsync("TEST0001", "192.168.1.1", "Mozilla/5.0", null);

        var allMessages = log.Select(e => e.message).ToList();
        Assert.DoesNotContain(allMessages, m => m.Contains("Email") || m.Contains("@"));
    }

    [Fact]
    public async Task RecordClickAsync_LogEvent_ContainsRequiredBusinessProperties()
    {
        var (service, log) = BuildService();

        await service.RecordClickAsync("TEST0001", "192.168.1.1", "Mozilla/5.0", null);

        var infoMessages = log.Where(e => e.level == LogLevel.Information).Select(e => e.message).ToList();
        Assert.NotEmpty(infoMessages);

        var clickLog = infoMessages.FirstOrDefault(m => m.Contains("ClickRecorded"));
        Assert.NotNull(clickLog);
        Assert.Contains("TEST0001", clickLog);  // AffiliateCode
        Assert.Contains("EventType", clickLog);
        Assert.Contains("AffiliateCode", clickLog);
        Assert.Contains("SessionId", clickLog);
    }
}

/// <summary>
/// Minimal ILogger implementation that captures log messages for test assertions.
/// </summary>
file sealed class CapturingLogger<T>(List<(LogLevel level, string message)> entries) : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        entries.Add((logLevel, formatter(state, exception)));
    }
}
