namespace Auth.Application.DTOs;

public record ResendVerificationRequest([Required][EmailAddress] string Email);
