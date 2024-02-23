namespace RedditInsightAPI.Models
{
    public class RedditComment
    {
        public string? Author { get; set; }
        public int Upvotes { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? Body { get; set; }
    }
}
