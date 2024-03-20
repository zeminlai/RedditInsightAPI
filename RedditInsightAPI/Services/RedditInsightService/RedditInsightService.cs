using RedditInsightAPI.Dtos.RedditInsight;
using Microsoft.AspNetCore.Mvc;
using PuppeteerSharp;
using Reddit.Controllers;
using Reddit;
using System.Text.RegularExpressions;
using RedditInsightAPI.Models;
using System.Web;
using Newtonsoft.Json;
using RestSharp;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HtmlAgilityPack;
using System.Security.Claims;

namespace RedditInsightAPI.Services.RedditInsightService
{
    public class RedditInsightService : IRedditInsightService
    {
        public async Task<GetRedditInsightResultsDto> GetRedditInsight(string searchTerm)
        {

            Console.WriteLine("searchTerm: " + searchTerm);

            //List<string> urlList = await GetUrlListFromWebScrape(searchTerm);
            List<string> urlList = await GetUrlListFromHiddenGoogleApi(searchTerm);

            GetRedditInsightResultsDto redditInsightResults = new GetRedditInsightResultsDto
            {
                RedditPosts = new List<RedditPost>(),
            };

            int urlPosition = 0;

            Parallel.ForEach(urlList, new ParallelOptions { MaxDegreeOfParallelism = 10 }, url =>
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

        private async Task<List<string>> GetUrlListFromHiddenGoogleApi(string searchTerm)
        {
            int maxUrls = 10;
            var client = new RestClient("https://www.google.com/search");
            var request = new RestRequest(Method.GET);
            request.AddParameter("q", "site: reddit.com " + searchTerm);
            request.AddParameter("oq", "site:");
            request.AddCookie("1P_JAR", "2024-03-05-05");
            request.AddCookie("NID", "512=Kp8rOU5_5GiqgLL0mAtnBZqmo0Qa4kbLP2iNnyx9xG9WQ1gTPc0GPPNUL46kBqLlsLBaTg8t-9G2X3glyuhUH4TqQ9TjeLRXiBlNUWrTx33MFeqLLSCSNNExEs7e1GXPPmsp0bVGJ6TyU6cqA4PG_mbEqUGxX5NqrcuwDd7mdDrpA6UwmcwMusV6MpK3YQ9C6x_s3VZTAc7PurzqrJ4mAXNupheoG33eZCQ4Re3GXlEEzhuYlNDFK4qKxsUfJhU609KRLCcFt2v0f7wMazg66l0WNmVqCI16fzGKKR-s06FSGK2q-Shc13NHCG4gwhBNuHRJL6GptMEVMKRt28VtQwzpOTIK6hanokrHObxMEjjFHYu3UoWc_6VS3-_sL3gbNPDBln7DzSkrGA2iJRhOJJJY6NVt4CYGfx7BLZn-KV2q82HaxvkYpxdhcYoJAOUOGDabznHgTZUI-VndnlhHoLAV3xBG3UD52zTBh62qi90P2wQEAv1okoMRjjX8Z5hV88EGK9390TCQnc6dLFT3rhdCNLQQXOFs4gDisZftAFtEcQjjfbEJTsozpVdxdoM5VKNzT4I9iGV80ZTbbG3yU7D0s0RcM3VTYLEtQ_YFjQVc_8LVIcoHvhfQbKzMxldTHiDiMaC0u0P798ZLx51z8IsiatHCALSCH4dF");
            request.AddCookie("AEC", "Ae3NU9OcQlqOTg_YYKxjX3VRRT0qVuI1awfSiGb8tam-QWweZOqUI1dtaw");
            IRestResponse response = client.Execute(request);

            var doc = new HtmlDocument();
            doc.LoadHtml(response.Content);
            var htmlBody = doc.DocumentNode.SelectNodes("//body/div[@id = 'main\']/div/div[@class='Gx5Zad fP1Qef xpd EtOod pkphOe\']");
            List<string> googleApiUrlList = new List<string>();

            foreach (var htmlItem in htmlBody)
            {
                string url = htmlItem.SelectSingleNode("div[@class='egMi0 kCrYT\']/a").Attributes["href"].Value;
                url = url.Substring(url.IndexOf("=") + 1);
                url = url.Substring(0, url.LastIndexOf('/'));
                Console.WriteLine(url);
                googleApiUrlList.Add(url);
            }

            googleApiUrlList = googleApiUrlList
                    .Where(url => url.Contains("https://www.reddit.com") && url.Contains("/comments/"))
                    .Where(url => (url.Count(c => c == '/') > 5))
                    .Take(maxUrls).ToList();

            return googleApiUrlList;
        }

        private RedditPost GetSingleRedditPost(string url, int urlPosition)
        {
            Console.WriteLine("Getting single reddit post:" + urlPosition);
            int maxComments = 5;
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

            Console.WriteLine("Fetching single reddit post: " + urlPosition);
            // Retrieve the post and return the result. 
            var post = reddit.Post(postFullname).About();
            Console.WriteLine("Fetching done: " + urlPosition);

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
                Link = url,
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

        private async Task<List<string>> GetUrlListFromWebScrape(string searchTerm)
        {
            Console.WriteLine("Getting list of urls....");
            var urlList = new List<string>();

            // download the browser executable
            await new BrowserFetcher().DownloadAsync();

            // browser execution configs
            var launchOptions = new LaunchOptions
            {
                Headless = true,
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

                foreach (string url in urlList)
                {
                    Console.WriteLine(url);
                }

                return urlList;
            }
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
