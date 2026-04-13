namespace Auth.Application.Services;

public class AuthService(
    AuthDbContext db,
    IAffiliateLookupService affiliateLookup,
    JwtSettings jwtSettings,
    IEventPublisher eventPublisher,
    IVerificationService verificationService) : IAuthService
{
    public async Task<RegisterResult> RegisterAsync(RegisterRequest request)
    {
        if (await db.Users.Apply(new UserByEmailSpecification(request.UserInformation.Email)).AnyAsync())
            throw new ConflictException("Email already registered.");

        var user = new User
        {
            Email        = request.UserInformation.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Information  = new UserInformation
            {
                FirstName   = request.UserInformation.FirstName,
                LastName    = request.UserInformation.LastName,
                PhoneCode   = request.UserInformation.PhoneCode,
                PhoneNumber = request.UserInformation.PhoneNumber,
                Language    = request.UserInformation.Language
            }
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var fullName = $"{request.UserInformation.FirstName} {request.UserInformation.LastName}";
        await affiliateLookup.CreateAffiliateAsync(user.Id, fullName);

        await eventPublisher.PublishAsync(new UserRegisteredEvent(user.Id, request.SessionId));

        return new RegisterResult(user.Id, user.Email);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var user = await db.Users.Apply(new UserByEmailSpecification(request.Email)).FirstOrDefaultAsync()
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var affiliateId = await affiliateLookup.GetAffiliateIdByUserIdAsync(user.Id)
            ?? throw new InvalidOperationException("Affiliate profile not found.");

        return BuildToken(user.Id, affiliateId);
    }

    public Task VerifyEmailAsync(string token)
        => verificationService.VerifyAsync(token);

    public Task ResendVerificationAsync(string email)
        => verificationService.ResendAsync(email);

    private AuthResult BuildToken(int userId, int affiliateId)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(jwtSettings.ExpiryMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim("affiliateId", affiliateId.ToString())
            ],
            expires: expiresAt,
            signingCredentials: creds);

        return new AuthResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt, affiliateId);
    }
}
