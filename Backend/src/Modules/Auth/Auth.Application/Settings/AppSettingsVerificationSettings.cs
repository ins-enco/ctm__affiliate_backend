using CopyTradeMarketApi.Shared.Verification;
using Microsoft.Extensions.Configuration;

namespace Auth.Application.Settings;

public class AppSettingsVerificationSettings(IConfiguration configuration) : IVerificationSettings
{
    public TimeSpan TokenExpiry
    {
        get
        {
            var hours = configuration.GetValue<int>("EmailVerification:TokenExpiryHours", 24);
            return TimeSpan.FromHours(hours);
        }
    }
}
