namespace RedditInsightAPI.Models
{
    public class RedditPost
    {
        public int Id { get; set; }
        public string? Author { get; set; }
        public string? Subreddit { get; set; }
        public int Upvotes { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? Title { get; set; }
        public string? Body { get; set; }
        public List<RedditComment>? Comments { get; set; }
    }
}
