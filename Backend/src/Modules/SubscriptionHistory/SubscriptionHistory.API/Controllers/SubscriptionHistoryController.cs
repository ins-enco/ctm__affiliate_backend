namespace SubscriptionHistory.API.Controllers;

/// <summary>
/// Exposes subscription history records for client subscribe/unsubscribe events.
/// </summary>
[ApiController]
[Route("api/subscription-history")]
public class SubscriptionHistoryController(ISubscriptionHistoryService service) : ControllerBase
{
    /// <summary>
    /// Returns subscription history records.
    /// Filtering, ordering, and pagination are optional and applied as: filter, then order, then paginate.
    /// </summary>
    /// <param name="query">Case-insensitive partial search over client name, account number, and strategy name.</param>
    /// <param name="statusFilter">Case-insensitive exact match on status. Allowed: Active, Inactive, New, Pending, Approved, Terminated, Connecting, Withdraw.</param>
    /// <param name="fromDate">Include only records with Timestamp on or after this date (inclusive).</param>
    /// <param name="toDate">Include only records with Timestamp on or before this date (inclusive).</param>
    /// <param name="orderBy">Sort field: timestamp, clientName, accountNumber, strategyName, or equityConnect.</param>
    /// <param name="orderDirection">Sort direction: asc or desc.</param>
    /// <param name="page">1-based page number. Defaults to 1 when <c>pageSize</c> is supplied without a page.</param>
    /// <param name="pageSize">Number of records per page. Defaults to 20 when <c>page</c> is supplied without a page size.</param>
    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        [FromQuery] string? query = null,
        [FromQuery] string? statusFilter = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? orderBy = null,
        [FromQuery] string? orderDirection = null)
    {
        var result = await service.GetAsync(page, pageSize, query, statusFilter, fromDate, toDate, orderBy, orderDirection);
        return Ok(result);
    }
}
