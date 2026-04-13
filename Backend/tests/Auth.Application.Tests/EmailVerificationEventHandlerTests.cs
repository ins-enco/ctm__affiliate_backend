using Auth.Application.EventHandlers;
using Auth.Application.Templates;
using Auth.Domain.Entities;
using CopyTradeMarketApi.Shared.Events;
using CopyTradeMarketApi.Shared.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Auth.Application.Tests;

public class EmailVerificationEventHandlerTests
{
    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AuthDbContext(options);
    }

    private static IConfiguration CreateConfiguration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BaseUrl"]                              = "https://test.example.com",
                ["EmailVerification:TokenExpiryHours"]  = "24"
            })
            .Build();

    private static async Task<User> SeedUserAsync(AuthDbContext db)
    {
        var user = new User
        {
            Email        = "test@example.com",
            PasswordHash = "hash",
            Information  = new UserInformation { FirstName = "John", LastName = "Doe" }
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task HandleAsync_HappyPath_CreatesTokenAndSendsEmail()
    {
        await using var db       = CreateDbContext();
        var user                 = await SeedUserAsync(db);
        var mockVerification     = new Mock<IVerificationService>();
        var mockResolver         = new Mock<ITemplateResolver>();
        var mockMail             = new Mock<IMailService>();

        mockVerification
            .Setup(v => v.CreateTokenAsync(user.Id, user.Email))
            .ReturnsAsync("test-token");

        mockResolver
            .Setup(r => r.ResolveAsync("email-verification"))
            .ReturnsAsync(new EmailTemplate("email-verification", "Hello {{RecipientName}}", "<p>{{VerificationLink}}</p>"));

        var handler = new EmailVerificationEventHandler(
            db, mockVerification.Object, mockResolver.Object, mockMail.Object,
            CreateConfiguration(), NullLogger<EmailVerificationEventHandler>.Instance);

        await handler.HandleAsync(new UserRegisteredEvent(user.Id, null));

        mockVerification.Verify(v => v.CreateTokenAsync(user.Id, user.Email), Times.Once);
        mockMail.Verify(m => m.SendAsync(It.Is<CopyTradeMarketApi.Shared.Mail.MailMessage>(
            msg => msg.To == user.Email && msg.Subject.Contains("John"))), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_MailServiceThrows_DoesNotRethrow()
    {
        await using var db  = CreateDbContext();
        var user            = await SeedUserAsync(db);
        var mockVerification = new Mock<IVerificationService>();
        var mockResolver    = new Mock<ITemplateResolver>();
        var mockMail        = new Mock<IMailService>();

        mockVerification
            .Setup(v => v.CreateTokenAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync("test-token");

        mockResolver
            .Setup(r => r.ResolveAsync(It.IsAny<string>()))
            .ReturnsAsync(new EmailTemplate("email-verification", "Subject", "Body"));

        mockMail
            .Setup(m => m.SendAsync(It.IsAny<CopyTradeMarketApi.Shared.Mail.MailMessage>()))
            .ThrowsAsync(new InvalidOperationException("SMTP unavailable"));

        var handler = new EmailVerificationEventHandler(
            db, mockVerification.Object, mockResolver.Object, mockMail.Object,
            CreateConfiguration(), NullLogger<EmailVerificationEventHandler>.Instance);

        // Must NOT throw
        await handler.HandleAsync(new UserRegisteredEvent(user.Id, null));
    }

    [Fact]
    public async Task HandleAsync_TemplateNotFound_DoesNotRethrow()
    {
        await using var db   = CreateDbContext();
        var user             = await SeedUserAsync(db);
        var mockVerification = new Mock<IVerificationService>();
        var mockResolver     = new Mock<ITemplateResolver>();
        var mockMail         = new Mock<IMailService>();

        mockVerification
            .Setup(v => v.CreateTokenAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync("test-token");

        mockResolver
            .Setup(r => r.ResolveAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Template not found"));

        var handler = new EmailVerificationEventHandler(
            db, mockVerification.Object, mockResolver.Object, mockMail.Object,
            CreateConfiguration(), NullLogger<EmailVerificationEventHandler>.Instance);

        // Must NOT throw
        await handler.HandleAsync(new UserRegisteredEvent(user.Id, null));

        mockMail.Verify(m => m.SendAsync(It.IsAny<CopyTradeMarketApi.Shared.Mail.MailMessage>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_SkipsDispatch()
    {
        await using var db   = CreateDbContext();
        var mockVerification = new Mock<IVerificationService>();
        var mockResolver     = new Mock<ITemplateResolver>();
        var mockMail         = new Mock<IMailService>();

        var handler = new EmailVerificationEventHandler(
            db, mockVerification.Object, mockResolver.Object, mockMail.Object,
            CreateConfiguration(), NullLogger<EmailVerificationEventHandler>.Instance);

        await handler.HandleAsync(new UserRegisteredEvent(99999, null));

        mockVerification.Verify(v => v.CreateTokenAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        mockMail.Verify(m => m.SendAsync(It.IsAny<CopyTradeMarketApi.Shared.Mail.MailMessage>()), Times.Never);
    }
}
