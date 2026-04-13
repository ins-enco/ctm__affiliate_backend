namespace SubscriptionHistory.Application.DTOs;

public record SubscriptionHistoryItem(
    DateTime Timestamp,
    string ClientName,
    string AccountNumber,
    string StrategyName,
    decimal EquityConnect,
    decimal? EquityDisconnect,
    string ActionType
);
