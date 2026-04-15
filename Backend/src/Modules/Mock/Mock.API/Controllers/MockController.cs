namespace Mock.API.Controllers;

/// <summary>
/// Serves static in-memory mock data for the Dashboard screen.
/// Available in the Development environment only (FR-011).
/// </summary>
[ApiController]
[Route("api")]
public class MockController(IMockService service) : ControllerBase
{
    /// <summary>Returns platform users for the dropdown search component. Supports optional name filtering.</summary>
    /// <param name="searchText">Case-insensitive partial match on user name. Returns all users when absent or empty.</param>
    /// <returns>PagedResponse (non-paginated) of user records; each has id, name, and role.</returns>
    [HttpGet("dashboard/listOfUsers")]
    public async Task<IActionResult> GetUsersAsync([FromQuery] string? searchText = null)
        => Ok(await service.GetUsersAsync(searchText));

    /// <summary>Returns the currently active dashboard user shown in the header. Requires <c>API-KEY: SimulatedKeyForDev</c> header in Development.</summary>
    /// <returns>Single user object with id, name, 2-character abbreviation, and role.</returns>
    [HttpGet("currentActiveUser")]
    [ServiceFilter(typeof(DevApiKeyFilter))]
    public async Task<IActionResult> GetCurrentUserAsync()
        => Ok(await service.GetCurrentUserAsync());

    /// <summary>Returns the 10 most recent client subscription or strategy requests.</summary>
    /// <returns>PagedResponse (non-paginated) of exactly 10 client request records.</returns>
    [HttpGet("dashboard/clientRequests")]
    public async Task<IActionResult> GetClientRequestsAsync()
        => Ok(await service.GetClientRequestsAsync());

    /// <summary>Returns the 10 most recent signal provider KYC or onboarding requests.</summary>
    /// <returns>PagedResponse (non-paginated) of exactly 10 signal provider request records.</returns>
    [HttpGet("dashboard/signalProviderRequests")]
    public async Task<IActionResult> GetSignalProviderRequestsAsync()
        => Ok(await service.GetSignalProviderRequestsAsync());

    /// <summary>Returns the 10 most recent affiliate KYC or onboarding requests.</summary>
    /// <returns>PagedResponse (non-paginated) of exactly 10 affiliate request records.</returns>
    [HttpGet("dashboard/affiliateRequests")]
    public async Task<IActionResult> GetAffiliateRequestsAsync()
        => Ok(await service.GetAffiliateRequestsAsync());
}
