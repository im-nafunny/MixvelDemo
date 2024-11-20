using RouteDemo.DTO;

namespace RouteDemo.Services
{
    public interface ISearchProviderOne
    {
        Task<ProviderOneSearchResponse> Search(ProviderOneSearchRequest request, CancellationToken cancellationToken);
        Task<bool> IsAvailableAsync(CancellationToken cancellationToken);
    }
}
