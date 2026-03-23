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
