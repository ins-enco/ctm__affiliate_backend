using CopyTradeMarketApi.Shared.Validation;

namespace Auth.Application.DTOs;

public record RegisterRequest
{
    [Required][MaxLength(100)]   public string Name { get; init; } = null!;
    [Required][StrictEmailField] public string Email { get; init; } = null!;
    [Required][PasswordField]    public string Password { get; init; } = null!;
    public string? SessionId { get; init; }
}
