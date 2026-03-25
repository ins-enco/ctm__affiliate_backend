namespace Tracking.API.Controllers;

[ApiController]
[Route("api/tracking")]
public class TrackingController(ITrackingService trackingService, IConfiguration configuration, IWebHostEnvironment env) : ControllerBase
{
    [HttpGet("click")]
    [ProducesResponseType(typeof(ClickResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Click([FromQuery] string affiliateCode)
    {
        var cookieName = configuration["ClickTracking:CookieName"] ?? "aff_sid";
        var cookieDays = int.TryParse(configuration["ClickTracking:CookieLifetimeDays"], out var d) ? d : 1;

        Request.Cookies.TryGetValue(cookieName, out var existingSessionId);

        // In Development, allow X-Forwarded-For override so the Mock FE can simulate different IPs.
        var ipAddress = env.IsDevelopment()
            ? Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString()
            : HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await trackingService.RecordClickAsync(affiliateCode, ipAddress, userAgent, existingSessionId);

        // Set the aff_sid cookie if it's a new session
        if (existingSessionId is null)
        {
            Response.Cookies.Append(cookieName, result.IsUnique
                ? CopyTradeMarketApi.Shared.Helpers.HashHelper.Sha256($"{ipAddress}{userAgent}{affiliateCode}")
                : existingSessionId ?? string.Empty,
                new CookieOptions
                {
                    HttpOnly = !env.IsDevelopment(),
                    Expires = DateTimeOffset.UtcNow.AddDays(cookieDays),
                    SameSite = SameSiteMode.Lax
                });
        }

        return Ok(result);
    }

    [HttpPost("convert")]
    [ProducesResponseType(typeof(ConversionResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Convert([FromBody] ConversionRequest request)
    {
        var result = await trackingService.RecordConversionAsync(request);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
