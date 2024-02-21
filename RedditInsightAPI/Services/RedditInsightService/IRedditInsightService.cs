using RedditInsightAPI.Dtos.RedditInsight;

namespace RedditInsightAPI.Services.RedditInsightService
{
    public interface IRedditInsightService
    {
        Task<GetRedditInsightResultsDto> GetRedditInsight(string searchTerm);
    }
}
