using CopyTradeMarketApi.Shared.Validation;

namespace Auth.Application.DTOs;

public record LoginRequest
{
    [Required][StrictEmailField] public string Email { get; init; } = null!;
    [Required]                   public string Password { get; init; } = null!;
}
