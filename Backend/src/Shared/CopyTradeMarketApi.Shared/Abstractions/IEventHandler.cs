namespace CopyTradeMarketApi.Shared.Abstractions;

public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent);
}
