using Affiliate.Application.DTOs;
using Affiliate.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Affiliate.API.Controllers;

[ApiController]
[Route("api/affiliate")]
[Authorize]
public class AffiliateDashboardController(IAffiliateDashboardService dashboardService) : ControllerBase
{
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Dashboard()
    {
        var affiliateIdClaim = User.FindFirst("affiliateId")?.Value
            ?? throw new UnauthorizedAccessException("affiliateId claim missing.");

        var affiliateId = int.Parse(affiliateIdClaim);
        var result = await dashboardService.GetDashboardAsync(affiliateId);
        return Ok(result);
    }
}
