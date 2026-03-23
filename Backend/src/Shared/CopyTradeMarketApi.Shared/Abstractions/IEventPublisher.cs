namespace CopyTradeMarketApi.Shared.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent;
}
