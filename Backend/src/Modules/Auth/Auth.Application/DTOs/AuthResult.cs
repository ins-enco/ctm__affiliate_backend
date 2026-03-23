namespace Auth.Application.DTOs;

public record AuthResult(string Token, DateTime ExpiresAt, int AffiliateId);
