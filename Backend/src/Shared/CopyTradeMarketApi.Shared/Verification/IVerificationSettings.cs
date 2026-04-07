namespace CopyTradeMarketApi.Shared.Verification;

/// <summary>
/// Expiry configuration for email verification tokens.
/// Implementations may read from appsettings.json or a database-backed settings store.
/// </summary>
public interface IVerificationSettings
{
    TimeSpan TokenExpiry { get; }
}
