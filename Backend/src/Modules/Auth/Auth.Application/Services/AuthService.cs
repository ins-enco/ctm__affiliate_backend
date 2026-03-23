namespace Auth.Application.Services;

public class AuthService(
    AuthDbContext db,
    IAffiliateLookupService affiliateLookup,
    JwtSettings jwtSettings,
    IEventPublisher eventPublisher) : IAuthService
{
    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        if (await db.Users.AnyAsync(u => u.Email == request.Email))
            throw new ConflictException("Email already registered.");

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var (affiliateId, _) = await affiliateLookup.CreateAffiliateAsync(user.Id, request.Name);

        await eventPublisher.PublishAsync(new UserRegisteredEvent(user.Id, request.SessionId));

        return BuildToken(user.Id, affiliateId);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var affiliateId = await affiliateLookup.GetAffiliateIdByUserIdAsync(user.Id)
            ?? throw new InvalidOperationException("Affiliate profile not found.");

        return BuildToken(user.Id, affiliateId);
    }

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
