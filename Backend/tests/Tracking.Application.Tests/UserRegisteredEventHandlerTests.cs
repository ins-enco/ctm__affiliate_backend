namespace Tracking.Application.Tests;

public class UserRegisteredEventHandlerTests
{
    // ── HandleAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WithValidSessionId_CallsRecordConversionWithRegistration()
    {
        // Arrange
        var mockTracking = new Mock<ITrackingService>();
        mockTracking
            .Setup(s => s.RecordConversionAsync(It.IsAny<ConversionRequest>()))
            .ReturnsAsync(new ConversionResult(true, "AFF00001", "Registration", "Conversion recorded and attributed."));

        var handler = new UserRegisteredEventHandler(mockTracking.Object);
        var evt = new UserRegisteredEvent(UserId: 42, SessionId: "SESSION-XYZ");

        // Act
        await handler.HandleAsync(evt);

        // Assert
        mockTracking.Verify(s => s.RecordConversionAsync(
            It.Is<ConversionRequest>(r =>
                r.SessionId == "SESSION-XYZ" &&
                r.ConversionType == "Registration" &&
                r.UserId == 42)),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNullSessionId_DoesNotCallTrackingService()
    {
        // Arrange
        var mockTracking = new Mock<ITrackingService>();
        var handler = new UserRegisteredEventHandler(mockTracking.Object);
        var evt = new UserRegisteredEvent(UserId: 1, SessionId: null);

        // Act
        await handler.HandleAsync(evt);

        // Assert — no conversion attempt when there is no session to attribute to
        mockTracking.Verify(s => s.RecordConversionAsync(It.IsAny<ConversionRequest>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenConversionAlreadyExists_PropagatesConflictException()
    {
        // Arrange
        var mockTracking = new Mock<ITrackingService>();
        mockTracking
            .Setup(s => s.RecordConversionAsync(It.IsAny<ConversionRequest>()))
            .ThrowsAsync(new ConflictException("A Registration conversion has already been recorded for this session."));

        var handler = new UserRegisteredEventHandler(mockTracking.Object);
        var evt = new UserRegisteredEvent(UserId: 5, SessionId: "SESSION-DUP");

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => handler.HandleAsync(evt));
    }
}
