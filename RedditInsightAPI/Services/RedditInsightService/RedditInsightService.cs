using RedditInsightAPI.Dtos.RedditInsight;
using Microsoft.AspNetCore.Mvc;
using PuppeteerSharp;
using Reddit.Controllers;
using Reddit;
using System.Text.RegularExpressions;
using RedditInsightAPI.Models;

namespace RedditInsightAPI.Services.RedditInsightService
{
    public class RedditInsightService : IRedditInsightService
    {
        public async Task<GetRedditInsightResultsDto> GetRedditInsight(string searchTerm)
        {

            List<string> urlList = await getUrlList(searchTerm);

            GetRedditInsightResultsDto redditInsightResults = new GetRedditInsightResultsDto
            {
                RedditPosts = new List<RedditPost>(),
            };

            Parallel.ForEach(urlList, new ParallelOptions { MaxDegreeOfParallelism = 5 }, url =>
            {
                RedditPost redditPost = ExecuteSingleRedditPost(url);
                redditInsightResults.RedditPosts.Add(redditPost);
            });

            return redditInsightResults;
        }
        private async Task<List<string>> getUrlList(string searchTerm)
        {
            var urlList = new List<string>();
            // download the browser executable
            await new BrowserFetcher().DownloadAsync();

            // browser execution configs
            var launchOptions = new LaunchOptions
            {
                Headless = true, // = false for testing
            };

            // open a new page in the controlled browser
            using (var browser = await Puppeteer.LaunchAsync(launchOptions))
            using (var page = await browser.NewPageAsync())
            {
                // scraping logic...
                await page.GoToAsync("https://www.google.com", waitUntil: WaitUntilNavigation.DOMContentLoaded);
                var googleSearchBox = await page.QuerySelectorAsync(".gLFyf");
                await googleSearchBox.TypeAsync("site: reddit.com " + searchTerm);
                await page.Keyboard.PressAsync("Enter");

                await page.WaitForNavigationAsync();

                var jsSelectAllAnchors = @"Array.from(document.querySelectorAll('.yuRUbf a')).map(a => a.href);";
                var urls = await page.EvaluateExpressionAsync<string[]>(jsSelectAllAnchors);
                //urlList = urls.Take(5).ToList();

                urlList = urls
                    .Where(url => url.Contains("https://www.reddit.com") && url.Contains("/comments/"))
                    .Where(url => (url.Count(c => c == '/') > 5))
                    .Take(5).ToList();

                var h3titles = await page.QuerySelectorAllAsync(".DKV0Md");
                return urlList;
            }
        }

        private RedditPost ExecuteSingleRedditPost(string permalink)
        {
            int maxComments = 10;
            var reddit = new RedditClient(appId: "vS5P8MkiKU1sd7cOQExJ4w", appSecret: "p1TGodFkUvlUSzPmmsacO6aTNgihgw", refreshToken: "2360522660282-Shxy4LKZk3-6OPm7sxd2VJp1urAj4A");

            // Get the ID from the permalink, then preface it with "t3_" to convert it to a Reddit fullname.  --Kris
            Match match = Regex.Match(permalink, @"\/comments\/([a-z0-9]+)\/");

            string postFullname = "t3_" + (match != null && match.Groups != null && match.Groups.Count >= 2
                ? match.Groups[1].Value
                : "");
            if (postFullname.Equals("t3_"))
            {
                throw new Exception("Unable to extract ID from permalink.");
            }

            // Retrieve the post and return the result.  --Kris
            var post = reddit.Post(postFullname).About();

            RedditPost redditPost = new RedditPost
            {
                Id = 1,
                Title = post.Title,
                Upvotes = post.UpVotes,
                Subreddit = post.Subreddit,
                Author = post.Author,
                CreatedAt = post.Created,
                Body = post.Listing.IsSelf
                ? ((SelfPost)post).SelfText
                : ((LinkPost)post).URL,
                Comments = new List<RedditComment>(),
            };
            var comments = post.Comments.GetConfidence().Take(maxComments);

            foreach (Comment comment in comments)
            {
                var replies = comment.Replies;
                RedditComment redditComment = new RedditComment
                {
                    Author = comment.Author,
                    CreatedAt = comment.Created,
                    Upvotes = comment.UpVotes,
                    Body = comment.Body,
                };
                redditPost.Comments.Add(redditComment);
            }
            return redditPost;
        }
    }
}
