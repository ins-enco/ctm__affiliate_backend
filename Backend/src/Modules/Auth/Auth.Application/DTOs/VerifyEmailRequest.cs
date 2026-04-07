namespace Auth.Application.DTOs;

public record VerifyEmailRequest([Required] string Token);
