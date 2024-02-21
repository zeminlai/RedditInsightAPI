using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RedditInsightAPI.Dtos.RedditInsight;

namespace RedditInsightAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RedditInsightController : ControllerBase
    {
        private readonly IRedditInsightService _redditInsightService;
        public RedditInsightController(IRedditInsightService redditInsightService)
        {
            _redditInsightService = redditInsightService;
        }

        [HttpGet]
        public async Task<ActionResult<GetRedditInsightResultsDto>> GetRedditInsightResults(string searchTerm)
        {
            return Ok(await _redditInsightService.GetRedditInsight(searchTerm));
        }
    }
}
