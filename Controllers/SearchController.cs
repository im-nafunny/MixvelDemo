using Microsoft.AspNetCore.Mvc;
using RouteDemo.DTO;
using RouteDemo.Services;

namespace RouteDemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ILogger<SearchController> _logger;
        private readonly ISearchService _searchService;

        public SearchController(ILogger<SearchController> logger, ISearchService searchService)
        {
            _logger = logger;
            _searchService = searchService;
        }

        [HttpPost("")]
        public async Task<SearchResponse> Search([FromBody] SearchRequest searchRequest, CancellationToken cancellationToken)
        {
            return await this._searchService.SearchAsync(searchRequest, cancellationToken);
        }

        [HttpGet("ping")]
        public async Task<IActionResult> Ping(CancellationToken cancellationToken)
        {
            if (await this._searchService.IsAvailableAsync(cancellationToken))
            {
                return this.Ok("Pong");
            }

            // return 500 if not available
            return this.Problem("Service is unavailable");
        }
    }
}
