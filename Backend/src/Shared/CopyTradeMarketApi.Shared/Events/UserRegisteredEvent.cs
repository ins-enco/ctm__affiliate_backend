using CopyTradeMarketApi.Shared.Abstractions;

namespace CopyTradeMarketApi.Shared.Events;

public record UserRegisteredEvent(int UserId, string? SessionId) : IDomainEvent;
