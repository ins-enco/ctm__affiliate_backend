using CopyTradeMarketApi.Shared.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CopyTradeMarketApi.Shared.Events;

public class EventPublisher(IServiceProvider serviceProvider) : IEventPublisher
{
    public async Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        var handlers = serviceProvider.GetServices<IEventHandler<TEvent>>();
        foreach (var handler in handlers)
            await handler.HandleAsync(domainEvent);
    }
}
