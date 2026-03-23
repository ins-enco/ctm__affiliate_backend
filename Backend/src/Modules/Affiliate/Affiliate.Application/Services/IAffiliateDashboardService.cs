using Affiliate.Application.DTOs;

namespace Affiliate.Application.Services;

public interface IAffiliateDashboardService
{
    Task<DashboardResult> GetDashboardAsync(int affiliateId);
}
