using RedditInsightAPI.Dtos.RedditInsight;
using Microsoft.AspNetCore.Mvc;
using PuppeteerSharp;
using Reddit.Controllers;
using Reddit;
using System.Text.RegularExpressions;
using RedditInsightAPI.Models;
using System.Web;
using Newtonsoft.Json;

namespace RedditInsightAPI.Services.RedditInsightService
{
    public class RedditInsightService : IRedditInsightService
    {
        public async Task<GetRedditInsightResultsDto> GetRedditInsight(string searchTerm)
        {

            Console.WriteLine("searchTerm: " + searchTerm);

            List<string> urlList = await GetUrlListFromWebScrape(searchTerm);


            GetRedditInsightResultsDto redditInsightResults = new GetRedditInsightResultsDto
            {
                RedditPosts = new List<RedditPost>(),
            };

            int urlPosition = 0;

            Parallel.ForEach(urlList, new ParallelOptions { MaxDegreeOfParallelism = 5 }, url =>
            {
                urlPosition++;
                RedditPost redditPost = GetSingleRedditPost(url, urlPosition);
                redditInsightResults.RedditPosts.Add(redditPost);
            });

            redditInsightResults.RedditPosts = redditInsightResults.RedditPosts
                .OrderBy(redditPost => redditPost.Id)
                .ToList();

            Console.WriteLine("Done");

            return redditInsightResults;
        }
        private async Task<List<string>> GetUrlListFromWebScrape(string searchTerm)
        {
            Console.WriteLine("Getting list of urls....");
            var urlList = new List<string>();

            // download the browser executable
            await new BrowserFetcher().DownloadAsync();

            // browser execution configs
            var launchOptions = new LaunchOptions
            {
                Headless = false,
            };

            // open a new page in the controlled browser
            using (var browser = await Puppeteer.LaunchAsync(launchOptions))
            using (var page = await browser.NewPageAsync())
            {
                await page.SetRequestInterceptionAsync(true);

                page.Request += (sender, e) =>
                {
                    if (e.Request.ResourceType == ResourceType.Image)
                        e.Request.AbortAsync();
                    else
                        e.Request.ContinueAsync();
                };

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
                Console.WriteLine("Url list scraped");

                foreach(string url in urlList)
                {
                    Console.WriteLine(url);
                }

                return urlList;
            }
        }

        private RedditPost GetSingleRedditPost(string url, int urlPosition)
        {
            Console.WriteLine("Getting single reddit post");
            int maxComments = 10;
            var reddit = new RedditClient(appId: "vS5P8MkiKU1sd7cOQExJ4w", appSecret: "p1TGodFkUvlUSzPmmsacO6aTNgihgw", refreshToken: "2360522660282-Shxy4LKZk3-6OPm7sxd2VJp1urAj4A");

            // Get the ID from the permalink, then preface it with "t3_" to convert it to a Reddit fullname.
            Match match = Regex.Match(url, @"\/comments\/([a-z0-9]+)\/");

            string postFullname = "t3_" + (match != null && match.Groups != null && match.Groups.Count >= 2
                ? match.Groups[1].Value
                : "");
            if (postFullname.Equals("t3_"))
            {
                throw new Exception("Unable to extract ID from permalink.");
            }

            // Retrieve the post and return the result. 
            var post = reddit.Post(postFullname).About();

            RedditPost redditPost = new RedditPost
            {
                Id = urlPosition,
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
            Console.WriteLine("Single reddit post done");
            return redditPost;
        }

        private async Task<List<string>> GetUrlListFromGoogleApi(string searchTerm)
        {
            HttpClient client = new HttpClient();

            var builder = new UriBuilder("https://customsearch.googleapis.com/customsearch/v1");
            builder.Port = -1;
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["cx"] = "b1a638f038c914afc";
            query["q"] = "site: reddit.com " + searchTerm;
            query["key"] = "AIzaSyDMBMHZ-dGJymEqm8lX83QTePAjatjMmv8";
            builder.Query = query.ToString();
            string url = builder.ToString();

            var responseString = await client.GetStringAsync(url);
            var myDeserializedClass = JsonConvert.DeserializeObject<dynamic>(responseString);

            List<string> googleApiUrlList = new List<string>();

            foreach (var items in myDeserializedClass.items)
            {
                googleApiUrlList.Add(items.link.ToString());
                Console.WriteLine(items.link);
            }

            googleApiUrlList = googleApiUrlList
                    .Where(url => url.Contains("https://www.reddit.com") && url.Contains("/comments/"))
                    .Where(url => (url.Count(c => c == '/') > 5))
                    .Take(5).ToList();

            return googleApiUrlList;
        }
    }
}
