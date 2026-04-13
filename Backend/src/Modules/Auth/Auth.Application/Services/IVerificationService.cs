namespace Auth.Application.Services;

public interface IVerificationService
{
    /// <summary>Generates a new verification token for the user and persists it.</summary>
    Task<string> CreateTokenAsync(int userId, string email);

    /// <summary>Validates the token, marks it consumed, and sets the user's email as verified.</summary>
    Task VerifyAsync(string token);

    /// <summary>
    /// Invalidates outstanding tokens for the account, generates a new one, and returns it.
    /// Throws <see cref="CopyTradeMarketApi.Shared.Exceptions.TooManyRequestsException"/> if called within the rate-limit window.
    /// </summary>
    Task<string> ResendAsync(string email);
}
