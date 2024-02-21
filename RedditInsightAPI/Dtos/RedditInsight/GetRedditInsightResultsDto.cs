using RedditInsightAPI.Models;

namespace RedditInsightAPI.Dtos.RedditInsight
{
    public class GetRedditInsightResultsDto
    {
        public List<RedditPost>? RedditPosts { get; set; }
    }
}
