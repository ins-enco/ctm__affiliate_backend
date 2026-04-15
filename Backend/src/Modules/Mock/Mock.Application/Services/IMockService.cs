namespace Mock.Application.Services;

public interface IMockService
{
    Task<PagedResponse<UserDto>> GetUsersAsync(string? searchText = null);
    Task<CurrentUserDto> GetCurrentUserAsync();
    Task<PagedResponse<ClientRequestDto>> GetClientRequestsAsync();
    Task<PagedResponse<SignalProviderRequestDto>> GetSignalProviderRequestsAsync();
    Task<PagedResponse<AffiliateRequestDto>> GetAffiliateRequestsAsync();
}
