using RouteDemo.DTO;
using RouteDemo.Helpers;

namespace RouteDemo.Services
{
    public class SearchProviderOne : ISearchProviderOne
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly Uri baseUri;
        public SearchProviderOne(string url, IHttpClientFactory httpClientFactory)
        {
            this.baseUri = new Uri(url);
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
        {
            using var httpClient = httpClientFactory.CreateClient();
            var res = await httpClient.GetAsync(new Uri(this.baseUri, "ping"), cancellationToken).ConfigureAwait(false);
            if (res.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

        public async Task<ProviderOneSearchResponse> Search(ProviderOneSearchRequest request, CancellationToken cancellationToken)
        {
            using var httpClient = httpClientFactory.CreateClient();
            var res = await httpClient.PostAsJsonAsync(new Uri(this.baseUri, "search"), request, cancellationToken).ConfigureAwait(false);
            if (res.IsSuccessStatusCode)
            {
                return await HttpResponseHelper.ReadFromHttpResponse<ProviderOneSearchResponse>(res);
            }

            throw new Exception("Internal error in service ProviderOne");
        }
    }
}
