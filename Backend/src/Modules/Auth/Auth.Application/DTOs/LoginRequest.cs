namespace Auth.Application.DTOs;

public record LoginRequest
{
    [Required][EmailAddress] public string Email { get; init; } = null!;
    [Required]               public string Password { get; init; } = null!;
}
