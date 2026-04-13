namespace Auth.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService, IConfiguration configuration) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var cookieName = configuration["ClickTracking:CookieName"] ?? "aff_sid";
        Request.Cookies.TryGetValue(cookieName, out var sessionId);

        var result = await authService.RegisterAsync(request with { SessionId = sessionId });
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        return Ok(result);
    }

    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        await authService.VerifyEmailAsync(request.Token);
        return Ok(new { message = "Email verified successfully." });
    }

    [HttpPost("resend-verification")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        await authService.ResendVerificationAsync(request.Email);
        return Ok(new { message = "Verification email sent." });
    }
}
