namespace Mock.Application.Services;

public interface IMockService
{
    Task<List<UserDto>> GetUsersAsync();
    Task<CurrentUserDto> GetCurrentUserAsync();
    Task<List<ClientRequestDto>> GetClientRequestsAsync();
    Task<List<SignalProviderRequestDto>> GetSignalProviderRequestsAsync();
    Task<List<AffiliateRequestDto>> GetAffiliateRequestsAsync();
}
