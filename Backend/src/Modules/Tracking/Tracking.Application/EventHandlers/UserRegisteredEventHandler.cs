using CopyTradeMarketApi.Shared.Abstractions;
using CopyTradeMarketApi.Shared.Events;
using Tracking.Application.DTOs;
using Tracking.Application.Services;

namespace Tracking.Application.EventHandlers;

public class UserRegisteredEventHandler(ITrackingService trackingService) : IEventHandler<UserRegisteredEvent>
{
    public async Task HandleAsync(UserRegisteredEvent domainEvent)
    {
        if (domainEvent.SessionId is null)
            return;

        await trackingService.RecordConversionAsync(new ConversionRequest(
            domainEvent.SessionId,
            "Registration",
            domainEvent.UserId));
    }
}
