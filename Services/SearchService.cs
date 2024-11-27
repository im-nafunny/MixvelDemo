using Microsoft.Extensions.Caching.Memory;
using RouteDemo.DTO;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace RouteDemo.Services
{
    public class SearchService : ISearchService
    {
        private readonly ISearchProviderOne searchProviderOne;
        private readonly ISearchProviderTwo searchProviderTwo;
        private readonly ILogger<SearchService> logger;
        private readonly MemoryCache genericCache;
        private readonly TimeSpan defaultCacheDuration = new TimeSpan(0, 10, 0); // 10 mins default
        private readonly SHA256 crypt;

        public SearchService(ISearchProviderOne searchProviderOne, ISearchProviderTwo searchProviderTwo, ILogger<SearchService> logger)
        {
            this.searchProviderOne = searchProviderOne;
            this.searchProviderTwo = searchProviderTwo;
            this.logger = logger;
            this.genericCache = new MemoryCache(new MemoryCacheOptions() { ExpirationScanFrequency = TimeSpan.FromSeconds(10) });
            this.crypt = SHA256.Create();
        }

        public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
        {
            try
            {
                var t1 = this.searchProviderOne.IsAvailableAsync(cancellationToken);
                var t2 = this.searchProviderTwo.IsAvailableAsync(cancellationToken);
                // Start both task in the same time
                await Task.WhenAll(t1, t2);
                // Check results when complete
                return await t1 && await t2;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Provider is not available");
                return false;
            }
        }

        public async Task<SearchResponse> SearchAsync(SearchRequest requestOrigin, CancellationToken cancellationToken)
        {
            var requestForCache = this.CreateRequestCopyForCache(requestOrigin);
            var key = this.GenerateKey(requestForCache);
            SearchResponse? result;
            if (genericCache.TryGetValue(key, out result) && result != null)
            {
                // found in cache - just return
                return result;
            }

            if (requestOrigin.Filters?.OnlyCached == true)
            {
                // requested from cache only but noting found
                return this.CreateEmptyResponse();
            }

            // nothing found - create new results and store them in cache
            return await this.GetFromCache(key, async () => {
                var t1 = this.RequestProviderOne(requestForCache, cancellationToken);
                var t2 = this.RequestProviderTwo(requestForCache, cancellationToken);
                // Start both task in the same time
                await Task.WhenAll([t1, t2]);
                return CombineResults([await t1, await t2]);
            });
        }

        private async Task<SearchResponse> RequestProviderOne(SearchRequest request, CancellationToken cancellationToken)
        {
            var providerRequest = new ProviderOneSearchRequest()
            {
                DateFrom = request.OriginDateTime,
                DateTo = request.Filters?.DestinationDateTime,
                From = request.Origin,
                To = request.Destination,
                MaxPrice = request.Filters?.MaxPrice,
            };

            try
            {
                var providerResults = await this.searchProviderOne.Search(providerRequest, cancellationToken);
                return new SearchResponse()
                {
                    MinPrice = providerResults.Routes.Min(r => r.Price),
                    MaxPrice = providerResults.Routes.Max(r => r.Price),
                    MinMinutesRoute = providerResults.Routes.Min(r => (r.DateTo - r.DateFrom).Minutes),
                    MaxMinutesRoute = providerResults.Routes.Max(r => (r.DateTo - r.DateFrom).Minutes),
                    Routes = providerResults.Routes.Select(r => new DTO.Route()
                    {
                        Id = Guid.NewGuid(),
                        Destination = r.To,
                        DestinationDateTime = r.DateTo,
                        Origin = r.From,
                        OriginDateTime = r.DateFrom,
                        Price = r.Price,
                        TimeLimit = r.TimeLimit,
                    }).ToArray(),
                };
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Exception on search by Provider One");
                return this.CreateEmptyResponse();
            }            
        }

        private async Task<SearchResponse> RequestProviderTwo(SearchRequest request, CancellationToken cancellationToken)
        {
            var providerRequest = new ProviderTwoSearchRequest()
            {
                DepartureDate = request.OriginDateTime,
                Departure = request.Origin,
                Arrival = request.Destination,
                MinTimeLimit = request.Filters?.MinTimeLimit,
            };

            try
            {
                var providerResults = await this.searchProviderTwo.Search(providerRequest, cancellationToken);
                return new SearchResponse()
                {
                    MinPrice = providerResults.Routes.Min(r => r.Price),
                    MaxPrice = providerResults.Routes.Max(r => r.Price),
                    MinMinutesRoute = providerResults.Routes.Min(r => (r.Arrival.Date - r.Departure.Date).Minutes),
                    MaxMinutesRoute = providerResults.Routes.Max(r => (r.Arrival.Date - r.Departure.Date).Minutes),
                    Routes = providerResults.Routes.Select(r => new DTO.Route()
                    {
                        Id = Guid.NewGuid(),
                        Destination = r.Arrival.Point,
                        DestinationDateTime = r.Arrival.Date,
                        Origin = r.Departure.Point,
                        OriginDateTime = r.Departure.Date,
                        Price = r.Price,
                        TimeLimit = r.TimeLimit,
                    }).ToArray(),
                };
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Exception on search by Provider Two");
                return this.CreateEmptyResponse();
            }
        }

        private SearchResponse CombineResults(IEnumerable<SearchResponse> data)
        {
            var result = new SearchResponse()
            {
                MaxPrice = decimal.MinValue,
                MinPrice = decimal.MaxValue,
                MaxMinutesRoute = int.MinValue,
                MinMinutesRoute = int.MaxValue,
            };

            var routes = new List<DTO.Route>();
            var comparer = new RouteComparer();
            foreach (var item in data)
            {
                result.MaxPrice = Math.Max(result.MaxPrice, item.MaxPrice);
                result.MinPrice = Math.Min(result.MinPrice, item.MinPrice);
                result.MaxMinutesRoute = Math.Max(result.MaxMinutesRoute, item.MaxMinutesRoute);
                result.MinMinutesRoute = Math.Min(result.MinMinutesRoute, item.MinMinutesRoute);
                foreach (var route in item.Routes)
                {
                    // ignore duplicate routes from different proivders
                    if (!routes.Contains(route, comparer))
                    {
                        routes.Add(route);
                    }
                }
            }

            result.Routes = routes.ToArray();
            return result;
        }

        private SearchResponse CreateEmptyResponse()
        {
            return new SearchResponse()
            {
                MaxMinutesRoute = int.MinValue,
                MinMinutesRoute = int.MaxValue,
                MaxPrice = int.MinValue,
                MinPrice = int.MaxValue,
                Routes = new DTO.Route[] { },
            };
        }

        private async Task<T> GetFromCache<T>(string key, Func<Task<T>> executeIfNotFoundInCache)
        {
            var item = await this.genericCache.GetOrCreateAsync(key, async (c) => {
                c.SetAbsoluteExpiration(this.defaultCacheDuration);
                return await executeIfNotFoundInCache.Invoke();
            });

            return item;
        }

        private string GenerateKey<T>(T data)
        {
            return Convert.ToBase64String(this.crypt.ComputeHash(Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(data))));
        }

        private SearchRequest CreateRequestCopyForCache(SearchRequest request)
        {
            var deepCopy = Newtonsoft.Json.JsonConvert.DeserializeObject<SearchRequest>(Newtonsoft.Json.JsonConvert.SerializeObject(request));
            if (deepCopy.Filters?.OnlyCached != null)
            {
                // ignore this flag in caching logic
                deepCopy.Filters.OnlyCached = null;
            }

            return deepCopy;
        }

        private class RouteComparer : EqualityComparer<DTO.Route>
        {
            public override bool Equals(DTO.Route? x, DTO.Route? y)
            {
                if (x == null && y == null)
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                // Note: ignore ID, assuming providers can return the same route with different IDs (we generate unique IDs for them).
                return string.Compare(x.Origin, y.Origin) == 0
                    && string.Compare(x.Destination, y.Destination) == 0
                    && x.OriginDateTime.Equals(y.OriginDateTime)
                    && x.DestinationDateTime.Equals(y.DestinationDateTime)
                    && x.Price.Equals(y.Price)
                    && x.TimeLimit.Equals(y.TimeLimit);
            }

            public override int GetHashCode([DisallowNull] DTO.Route obj)
            {
                return HashCode.Combine(obj.Origin, obj.Destination, obj.OriginDateTime, obj.DestinationDateTime, obj.Price, obj.TimeLimit);
            }
        }
    }
}
