namespace Auth.Application.Services;

public interface IAuthService
{
    Task<RegisterResult> RegisterAsync(RegisterRequest request);
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task VerifyEmailAsync(string token);
    Task ResendVerificationAsync(string email);
}
