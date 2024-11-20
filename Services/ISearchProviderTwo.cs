using RouteDemo.DTO;

namespace RouteDemo.Services
{
    public interface ISearchProviderTwo
    {
        Task<ProviderTwoSearchResponse> Search(ProviderTwoSearchRequest request, CancellationToken cancellationToken);
        Task<bool> IsAvailableAsync(CancellationToken cancellationToken);
    }
}
