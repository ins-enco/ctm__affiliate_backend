namespace SubscriptionHistory.API.Controllers;

/// <summary>
/// Exposes subscription history records for client subscribe/unsubscribe events.
/// </summary>
[ApiController]
[Route("api/subscription-history")]
public class SubscriptionHistoryController(ISubscriptionHistoryService service) : ControllerBase
{
    /// <summary>
    /// Returns subscription history records. Omit pagination parameters to retrieve all records;
    /// supply <paramref name="page"/> and/or <paramref name="pageSize"/> to receive a paginated slice.
    /// </summary>
    /// <param name="page">1-based page number. Defaults to 1 when <c>pageSize</c> is supplied without a page.</param>
    /// <param name="pageSize">Number of records per page. Defaults to 20 when <c>page</c> is supplied without a page size.</param>
    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null)
    {
        var result = await service.GetAsync(page, pageSize);
        return Ok(result);
    }
}
