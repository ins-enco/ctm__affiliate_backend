namespace Mock.API.Controllers;

/// <summary>
/// Serves static in-memory mock data for the Dashboard screen.
/// Available in the Development environment only (FR-011).
/// </summary>
[ApiController]
[Route("api/mock")]
public class MockController(IMockService service) : ControllerBase
{
    /// <summary>Returns all platform users for the dropdown search component.</summary>
    /// <returns>Array of user records; each has id, name, and role.</returns>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsersAsync()
        => Ok(await service.GetUsersAsync());

    /// <summary>Returns the currently active dashboard user shown in the header.</summary>
    /// <returns>Single user object with id, name, 2-character abbreviation, and role.</returns>
    [HttpGet("current-user")]
    public async Task<IActionResult> GetCurrentUserAsync()
        => Ok(await service.GetCurrentUserAsync());

    /// <summary>Returns the 10 most recent client subscription or strategy requests.</summary>
    /// <returns>Array of exactly 10 client request records.</returns>
    [HttpGet("client-requests")]
    public async Task<IActionResult> GetClientRequestsAsync()
        => Ok(await service.GetClientRequestsAsync());

    /// <summary>Returns the 10 most recent signal provider KYC or onboarding requests.</summary>
    /// <returns>Array of exactly 10 signal provider request records.</returns>
    [HttpGet("signal-provider-requests")]
    public async Task<IActionResult> GetSignalProviderRequestsAsync()
        => Ok(await service.GetSignalProviderRequestsAsync());

    /// <summary>Returns the 10 most recent affiliate KYC or onboarding requests.</summary>
    /// <returns>Array of exactly 10 affiliate request records.</returns>
    [HttpGet("affiliate-requests")]
    public async Task<IActionResult> GetAffiliateRequestsAsync()
        => Ok(await service.GetAffiliateRequestsAsync());
}
