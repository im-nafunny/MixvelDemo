using RouteDemo.DTO;
using RouteDemo.Helpers;

namespace RouteDemo.Services
{
    public class SearchProviderTwo : ISearchProviderTwo
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly Uri baseUri;
        public SearchProviderTwo(string url, IHttpClientFactory httpClientFactory)
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

        public async Task<ProviderTwoSearchResponse> Search(ProviderTwoSearchRequest request, CancellationToken cancellationToken)
        {
            using var httpClient = httpClientFactory.CreateClient();
            var res = await httpClient.PostAsJsonAsync(new Uri(this.baseUri, "search"), request, cancellationToken).ConfigureAwait(false);
            if (res.IsSuccessStatusCode)
            {
                return await HttpResponseHelper.ReadFromHttpResponse<ProviderTwoSearchResponse>(res);
            }

            throw new Exception("Internal error in service ProviderTwo");
        }
    }
}
