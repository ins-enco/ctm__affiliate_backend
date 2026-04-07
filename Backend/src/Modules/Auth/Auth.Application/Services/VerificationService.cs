using System.Security.Cryptography;
using CopyTradeMarketApi.Shared.Exceptions;
using CopyTradeMarketApi.Shared.Verification;

namespace Auth.Application.Services;

public class VerificationService(
    AuthDbContext db,
    IVerificationSettings settings) : IVerificationService
{
    private static readonly TimeSpan ResendRateLimit = TimeSpan.FromMinutes(2);

    public async Task<string> CreateTokenAsync(int userId, string email)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
                          .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        var verificationToken = new EmailVerificationToken
        {
            UserId    = userId,
            Email     = email,
            Token     = token,
            ExpiresAt = DateTime.UtcNow.Add(settings.TokenExpiry)
        };

        db.EmailVerificationTokens.Add(verificationToken);
        await db.SaveChangesAsync();
        return token;
    }

    public async Task VerifyAsync(string token)
    {
        var record = await db.EmailVerificationTokens
            .Apply(new UserByVerificationTokenSpecification(token, includeUser: true))
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Verification token is invalid, expired, or has already been used.");

        if (record.ConsumedAt.HasValue || record.ExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("Verification token is invalid, expired, or has already been used.");

        if (record.User.IsEmailVerified)
            throw new ConflictException("Email address is already verified.");

        record.ConsumedAt         = DateTime.UtcNow;
        record.UpdatedAt          = DateTime.UtcNow;
        record.User.IsEmailVerified = true;
        record.User.UpdatedAt     = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task<string> ResendAsync(string email)
    {
        var user = await db.Users
            .Apply(new UserByEmailSpecification(email))
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("No account found with that email address.");

        if (user.IsEmailVerified)
            throw new ConflictException("Email address is already verified.");

        var recent = await db.EmailVerificationTokens
            .Where(t => t.UserId == user.Id)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        if (recent is not null && DateTime.UtcNow - recent.CreatedAt < ResendRateLimit)
            throw new TooManyRequestsException("A verification email was recently sent. Please wait before requesting another.");

        // Invalidate all outstanding tokens
        var activeTokens = await db.EmailVerificationTokens
            .Where(t => t.UserId == user.Id && t.ConsumedAt == null)
            .ToListAsync();

        foreach (var t in activeTokens)
        {
            t.ConsumedAt = DateTime.UtcNow;
            t.UpdatedAt  = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return await CreateTokenAsync(user.Id, email);
    }
}
