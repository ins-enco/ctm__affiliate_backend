namespace CopyTradeMarketApi.Shared.Abstractions;

public interface IClickStatsReader
{
    Task<ClickStats> GetAsync(int affiliateId);
}

public record ClickStats(int TotalClicks, int UniqueClicks, int Last7DayClicks, int ConvertedClicks);
